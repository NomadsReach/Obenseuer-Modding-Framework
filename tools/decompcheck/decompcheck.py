#!/usr/bin/env python
import argparse
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from csindex import build_index, normalize_ref
from resolve import Universe, discover_helpers
from codechecks import CodeScan, Sink
from docchecks import DocScan, string_literals

DEFAULT_EXPORT = r'C:\Users\Ricky\source\ObenseuerPublicExport'
DEFAULT_DECOMPILE = r'C:\Users\Ricky\source\ObenseuerReverseEng\Game\generated\25969064'


def default_out():
    base = os.environ.get('LOCALAPPDATA') or os.path.join(os.path.expanduser('~'), 'AppData', 'Local')
    return os.path.join(base, 'ObenseuerDecompCheck')


def walk(root, exts, skip=('.git', 'bin', 'obj')):
    out = []
    for dp, dn, fn in os.walk(root):
        dn[:] = [d for d in dn if d not in skip]
        for f in fn:
            if f.endswith(exts):
                out.append(os.path.join(dp, f))
    return sorted(out)


def rel(path, root):
    return os.path.relpath(path, root).replace('\\', '/')


def check_types_lists(decompile, game_index):
    result = []
    for name in ('types-Assembly-CSharp.txt', 'types-Assembly-CSharp-firstpass.txt'):
        path = os.path.join(decompile, name)
        listed = parsed = generated = 0
        missing = []
        if not os.path.exists(path):
            result.append({'file': name, 'listed': 0, 'parsed': 0, 'generated': 0, 'missing': ['file not found']})
            continue
        with open(path, 'r', encoding='utf-8-sig', errors='replace') as fh:
            for line in fh:
                parts = line.strip().split(' ', 1)
                if len(parts) != 2:
                    continue
                listed += 1
                nm = parts[1].strip()
                if '<' in nm:
                    generated += 1
                    continue
                if normalize_ref(nm) in game_index.by_full:
                    parsed += 1
                else:
                    missing.append(nm)
        result.append({'file': name, 'listed': listed, 'parsed': parsed, 'generated': generated,
                       'missing': missing})
    return result


def build(export, decompile):
    t0 = time.time()
    game = build_index([os.path.join(decompile, 'Assembly-CSharp'),
                        os.path.join(decompile, 'Assembly-CSharp-firstpass')], 'game', decompile)
    fw = build_index([os.path.join(export, 'src'), os.path.join(export, 'webui'),
                      os.path.join(export, 'preloader')], 'framework', export)
    return game, fw, time.time() - t0


def run_checks(export, decompile, only):
    game, fw, index_secs = build(export, decompile)
    univ = Universe(game, fw)
    helpers = discover_helpers(univ)
    code_sink = Sink('code')
    doc_sink = Sink('docs')
    result = {'code_files': 0, 'doc_stats': {}, 'helpers': sorted(helpers), 'game_refs': []}
    if only in (None, 'code'):
        scan = CodeScan(univ, helpers, code_sink)
        result['code_files'] = scan.run(export)
    if only in (None, 'docs'):
        doc_paths = []
        for p in walk(os.path.join(export, 'docs'), '.md'):
            doc_paths.append((p, rel(p, export)))
        for name in ('README.md', os.path.join('Starter', 'README.md'), 'CHANGELOG.md'):
            p = os.path.join(export, name)
            if os.path.exists(p):
                doc_paths.append((p, rel(p, export)))
        xml_paths = [(p, rel(p, export)) for p in walk(os.path.join(export, 'src'), '.cs')]
        export_rels = [rel(p, export) for p in walk(export, '.cs')]
        decompile_rels = [rel(p, decompile) for p in walk(decompile, '.cs')]
        literals = string_literals(export, walk(export, '.cs'))
        dscan = DocScan(univ, doc_sink, export_rels, decompile_rels, decompile, literals)
        result['doc_stats'] = dscan.run(export, doc_paths, xml_paths)
        result['game_refs'] = dscan.game_refs
        result['doc_files'] = len(doc_paths) + len(xml_paths)
    result.update({'game': game, 'fw': fw, 'index_secs': index_secs,
                   'findings': code_sink.findings + doc_sink.findings,
                   'counts': dict(code_sink.counts)})
    for k, v in doc_sink.counts.items():
        result['counts'].setdefault(k, v)
    result['types_lists'] = check_types_lists(decompile, game)
    return result


def fmt_list(items):
    return ', '.join(items) if items else ''


def table(rows):
    out = ['| Status | File:line | Reference | Why | Closest real names |', '|---|---|---|---|---|']
    for f in rows:
        loc = '%s:%s' % (f['file'], f['line']) if f['line'] else f['file']
        out.append('| %s | %s | `%s` | %s | %s |' % (f['status'], loc, f['ref'].replace('|', '\\|'),
                                                  f['message'].replace('|', '\\|'),
                                                  fmt_list(f['closest']).replace('|', '\\|')))
    return out


def write_report(res, out_dir, export, decompile):
    os.makedirs(out_dir, exist_ok=True)
    findings = res['findings']
    fails_code = [f for f in findings if f['section'] == 'code' and f['status'] == 'FAIL']
    fails_docs = [f for f in findings if f['section'] == 'docs' and f['status'] == 'FAIL']
    warns = [f for f in findings if f['status'] == 'WARN']
    by_pattern = {}
    for f in warns:
        by_pattern[f['pattern']] = by_pattern.get(f['pattern'], 0) + 1
    lines = ['# Decompile reference check', '',
             'Export: `%s`' % export, 'Decompile: `%s`' % decompile, '',
             'Totals: FAIL (code) %d, FAIL (docs) %d, WARN %d' % (len(fails_code), len(fails_docs), len(warns)), '']
    lines += ['## FAIL (code)', ''] + (table(fails_code) if fails_code else ['None.']) + ['']
    lines += ['## FAIL (docs)', ''] + (table(fails_docs) if fails_docs else ['None.']) + ['']
    lines += ['## WARN', '']
    if by_pattern:
        lines += ['Top patterns:', '']
        for k, v in sorted(by_pattern.items(), key=lambda kv: -kv[1]):
            lines.append('- %s: %d' % (k, v))
        lines += ['']
        lines += table(warns)
    else:
        lines.append('None.')
    lines += ['', '## Game file references found in public docs', '']
    if res['game_refs']:
        lines += ['| Doc | Line | Reference | Matches in decompile |', '|---|---|---|---|']
        for g in res['game_refs']:
            lines.append('| %s | %s | `%s` | %s |' % (g['doc'], g['line'], g['ref'], fmt_list(g['matches'][:3])))
    else:
        lines.append('None.')
    lines += ['', '## Coverage', '']
    gi, fi = res['game'], res['fw']
    lines += ['- Code files scanned (export .cs, all folders): %d' % res['code_files']]
    lines += ['- Doc files scanned (docs md, README, Starter README, CHANGELOG, src XML comments): %s' % res.get('doc_files', 0)]
    lines += ['- Game files indexed: %d (types %d)' % (gi.files, gi.count())]
    lines += ['- Framework files indexed: %d (types %d)' % (fi.files, fi.count())]
    lines += ['- Framework helpers with (Type, string) signatures: %d names (%s)' % (len(res['helpers']), fmt_list(res['helpers']))]
    lines += ['', '| Check kind | OK | FAIL | WARN |', '|---|---|---|---|']
    for k in sorted(res['counts']):
        c = res['counts'][k]
        lines.append('| %s | %d | %d | %d |' % (k, c['ok'], c['fail'], c['warn']))
    lines += ['', '| Doc stat | Count |', '|---|---|']
    for k, v in sorted(res['doc_stats'].items()):
        lines.append('| %s | %d |' % (k, v))
    lines += ['', '| types-*.txt | Listed | Parsed | Compiler-generated (not emitted by ilspycmd) | Listed but not parsed |',
              '|---|---|---|---|---|']
    for t in res['types_lists']:
        lines.append('| %s | %d | %d | %d | %d |' % (t['file'], t['listed'], t['parsed'], t['generated'], len(t['missing'])))
        for m in t['missing'][:20]:
            lines.append('| - %s | | | | |' % m)
    lines.append('')
    md = os.path.join(out_dir, 'decompcheck-report.md')
    with open(md, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write('\n'.join(lines))
    js = os.path.join(out_dir, 'decompcheck-report.json')
    payload = {'export': export, 'decompile': decompile, 'findings': findings, 'counts': res['counts'],
               'doc_stats': res['doc_stats'], 'game_refs': res['game_refs'], 'types_lists': res['types_lists'],
               'game_files': gi.files, 'game_types': gi.count(), 'framework_files': fi.files,
               'framework_types': fi.count(), 'helpers': res['helpers']}
    with open(js, 'w', encoding='utf-8', newline='\n') as fh:
        json.dump(payload, fh, indent=1)
    return md, js, len(fails_code), len(fails_docs), len(warns)


def main(argv=None):
    if argv is None:
        argv = sys.argv[1:]
    if argv and argv[0] == 'selftest':
        import selftest
        n = selftest.run()
        print('selftest OK: %d groups of asserts passed' % n)
        return 0
    ap = argparse.ArgumentParser(description='Check export references against the game decompile.')
    ap.add_argument('--export', default=DEFAULT_EXPORT)
    ap.add_argument('--decompile', default=DEFAULT_DECOMPILE)
    ap.add_argument('--out', default=None)
    ap.add_argument('--only', choices=['code', 'docs'], default=None)
    args = ap.parse_args(argv)
    out_dir = args.out or default_out()
    res = run_checks(args.export, args.decompile, args.only)
    md, js, nc, nd, nw = write_report(res, out_dir, args.export, args.decompile)
    print('FAIL (code): %d' % nc)
    print('FAIL (docs): %d' % nd)
    print('WARN: %d' % nw)
    print('report: %s' % md)
    print('json: %s' % js)
    return 1 if (nc or nd) else 0


if __name__ == '__main__':
    sys.exit(main())
