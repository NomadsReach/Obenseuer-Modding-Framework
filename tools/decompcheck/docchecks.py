import bisect
import difflib
import os
import re

from csindex import ALL_KINDS
from resolve import EXTERNAL_PREFIXES, EXTERNAL_TYPES

BACKTICK = re.compile(r'`([^`\n]+)`')
XML_C = re.compile(r'<c>([^<]+)</c>')
CREF = re.compile(r'cref="([^"]+)"')
HEAD = re.compile(r'^([A-Za-z_]\w*(?:(?:::|\.)[A-Za-z_]\w*)*)')
CSHARP_CODE = re.compile(r'(?<![\w.])([A-Z]\w*)\.([A-Za-z_]\w*)\b')
FILE_REF = re.compile(r'(?<![\w/\\.])((?:[A-Za-z0-9_.-]+[\\/])*[A-Za-z0-9_-]+\.cs)(?::(\d+))?(?![\w.])')
FENCE_OPEN = re.compile(r'^\s*```+\s*([\w+#-]*)\s*$')
FENCE_CLOSE = re.compile(r'^\s*```+\s*$')
KEYWORDS = {'string', 'int', 'bool', 'float', 'double', 'long', 'byte', 'char', 'void', 'object', 'var', 'null',
            'true', 'false', 'new', 'return', 'this', 'base', 'static', 'public', 'private', 'class', 'enum',
            'if', 'else', 'for', 'foreach', 'while', 'using', 'namespace', 'typeof', 'nameof', 'default'}
FILE_EXT = {'cs', 'json', 'md', 'dll', 'txt', 'yml', 'yaml', 'png', 'jpg', 'wav', 'ogg', 'csproj', 'props', 'sln',
            'ps1', 'py', 'sh', 'zip', 'xml', 'html', 'js', 'css', 'ts', 'lua', 'exe', 'log', 'cfg', 'ini', 'toml',
            'bat', 'cmd', 'gif', 'svg', 'pdf', 'mp3', 'bin', 'dat', 'nomad', 'csv', 'asset', 'prefab', 'unity',
            'anim', 'mat', 'shader', 'cginc', 'mp4', 'webm', 'woff', 'ttf', 'otf', 'bepinex', 'cache'}
DOC_EXTERNAL = {'Harmony', 'BepInEx', 'Debug', 'Time', 'Input', 'Application', 'PlayerPrefs', 'File', 'Path',
                'Directory', 'Json', 'JsonUtility', 'Console', 'Math', 'Random', 'Convert', 'Environment',
                'Regex', 'Task', 'Thread', 'Encoding', 'Guid', 'DateTime', 'TimeSpan', 'Action', 'Func',
                'LocalizationSettings', 'Log'}
NEGATED = re.compile(r'(?:\bno|\bnot|\bnor|\bwithout|\bnever|\bremoved|\bno longer)\s*$', re.I)
ILLUSTRATIVE_FILES = {'ExampleCounter.cs', 'InfoPanel.cs'}
STRING_LIT = re.compile(r'"((?:[^"\\\n]|\\.)*)"')


def match_rels(path, rels):
    return [r for r in rels if r == path or r.endswith('/' + path)]


def string_literals(export_root, paths):
    out = {}
    for p in paths:
        rel = os.path.relpath(p, export_root).replace('\\', '/')
        with open(p, 'r', encoding='utf-8-sig', errors='replace') as fh:
            for m in STRING_LIT.finditer(fh.read()):
                out.setdefault(m.group(1), set()).add(rel)
    return out


class DocScan:
    def __init__(self, univ, sink, export_rels, decompile_rels, decompile_root, literals):
        self.univ = univ
        self.sink = sink
        self.export_rels = export_rels
        self.decompile_rels = decompile_rels
        self.decompile_root = decompile_root
        self.literals = literals
        self.line_cache = {}
        self.stats = {'files': 0, 'spans': 0, 'checked': 0, 'unknown_head': 0, 'external': 0,
                      'not_identifier': 0, 'file_like': 0, 'negated': 0, 'string_literal': 0,
                      'code_block_refs': 0, 'file_refs': 0, 'cs_refs_export': 0, 'cs_refs_game': 0,
                      'cs_refs_illustrative': 0, 'cs_refs_missing': 0}
        self.game_refs = []

    def external_head(self, parts, cands):
        return all(c.asm == 'game' for c in cands) and parts[0] in (EXTERNAL_TYPES | DOC_EXTERNAL)

    def check_span(self, text, file, line, before=''):
        self.stats['spans'] += 1
        t = text.strip()
        if not t or '/' in t or '\\' in t:
            self.stats['not_identifier'] += 1
            return
        if t in self.literals:
            self.stats['string_literal'] += 1
            return
        if NEGATED.search(before):
            self.stats['negated'] += 1
            return
        m = HEAD.match(t)
        if not m:
            self.stats['not_identifier'] += 1
            return
        rest = t[m.end():]
        if rest and not rest.startswith('('):
            self.stats['not_identifier'] += 1
            return
        parts = m.group(1).replace('::', '.').split('.')
        if len(parts) > 1 and parts[-1].lower() in FILE_EXT:
            self.stats['file_like'] += 1
            return
        if parts[0] in KEYWORDS:
            self.stats['not_identifier'] += 1
            return
        k, cands = self.longest_type(parts)
        if not cands:
            if self.univ.is_external(parts[0]) or parts[0] in DOC_EXTERNAL or '.'.join(parts).startswith(EXTERNAL_PREFIXES):
                self.stats['external'] += 1
            else:
                self.stats['unknown_head'] += 1
            return
        if self.external_head(parts, cands):
            self.stats['external'] += 1
            return
        if k == len(parts):
            self.stats['checked'] += 1
            return
        self.stats['checked'] += 1
        member = parts[k]
        st, note, close = self.univ.member(cands, member, ALL_KINDS)
        ref = t
        if st == 'ok':
            return
        if st == 'fail' and any(c.file in self.literals.get(member, ()) for c in cands):
            st, note = 'warn', 'not a member, but the name is a string literal in the type\'s own file (config key?)'
        if st == 'warn':
            self.sink.add('WARN', 'docs member', file, line, ref, '"%s": %s' % (member, note), close, 'docs member')
        elif len(set(c.full for c in cands)) > 1:
            self.sink.add('WARN', 'docs member', file, line, ref,
                          '"%s" not on any of %d types named %s (short name is ambiguous)' % (
                              member, len(set(c.full for c in cands)), '.'.join(parts[:k])), close, 'ambiguous name')
        else:
            self.sink.add('FAIL', 'docs member', file, line, ref,
                          '"%s" does not exist on %s (framework or game index)' % (member, '.'.join(parts[:k])),
                          close, 'docs member')

    def longest_type(self, parts):
        for k in range(len(parts), 0, -1):
            cands = self.univ.find('.'.join(parts[:k]))
            if cands:
                return k, cands
        return 0, []

    def code_block(self, lines, file, start):
        for off, ln in enumerate(lines):
            for m in CSHARP_CODE.finditer(ln):
                head, member = m.group(1), m.group(2)
                if head in KEYWORDS or head in DOC_EXTERNAL:
                    continue
                cands = self.univ.find(head)
                if not cands or self.external_head([head], cands):
                    continue
                self.stats['code_block_refs'] += 1
                st, note, close = self.univ.member(cands, member, ALL_KINDS)
                if st == 'fail':
                    self.sink.add('WARN', 'docs code block', file, start + off, m.group(0),
                                  '"%s" not found on %s (code block)' % (member, head), close, 'code block')

    def file_refs(self, text, file, line):
        for m in FILE_REF.finditer(text):
            path = m.group(1).replace('\\', '/')
            num = int(m.group(2)) if m.group(2) else None
            self.stats['file_refs'] += 1
            if path.split('/')[-1] in ILLUSTRATIVE_FILES:
                self.stats['cs_refs_illustrative'] += 1
                continue
            exp = match_rels(path, self.export_rels)
            if exp:
                self.stats['cs_refs_export'] += 1
                continue
            game = match_rels(path, self.decompile_rels)
            if not game:
                self.stats['cs_refs_missing'] += 1
                self.sink.add('FAIL', 'docs file', file, line, m.group(0),
                              'file "%s" is in neither the export nor the decompile' % path, None, 'file ref')
                continue
            self.stats['cs_refs_game'] += 1
            self.game_refs.append({'doc': file, 'line': line, 'ref': m.group(0), 'matches': game})
            if num is not None:
                n = self.line_count(game[0])
                if n is not None and num > n:
                    self.sink.add('WARN', 'docs file line', file, line, m.group(0),
                                  'line %d is past the end of %s (%d lines)' % (num, game[0], n), None, 'file line')

    def line_count(self, rel):
        if rel not in self.line_cache:
            try:
                with open(os.path.join(self.decompile_root, rel), 'r', encoding='utf-8-sig', errors='replace') as fh:
                    self.line_cache[rel] = sum(1 for _ in fh)
            except OSError:
                self.line_cache[rel] = None
        return self.line_cache[rel]

    def markdown(self, path, rel):
        with open(path, 'r', encoding='utf-8-sig', errors='replace') as fh:
            lines = fh.read().split('\n')
        self.stats['files'] += 1
        in_code = False
        lang = ''
        block = []
        block_start = 0
        for i, ln in enumerate(lines, 1):
            if not in_code:
                fo = FENCE_OPEN.match(ln)
                if fo:
                    in_code = True
                    lang = fo.group(1).lower()
                    block = []
                    block_start = i + 1
                    continue
            else:
                if FENCE_CLOSE.match(ln):
                    in_code = False
                    if lang in ('csharp', 'cs', 'c#'):
                        self.code_block(block, rel, block_start)
                    continue
                block.append(ln)
                self.file_refs(ln, rel, i)
                continue
            self.file_refs(ln, rel, i)
            for sp in BACKTICK.finditer(ln):
                self.check_span(sp.group(1), rel, i, ln[:sp.start()])

    def xml_comments(self, path, rel):
        with open(path, 'r', encoding='utf-8-sig', errors='replace') as fh:
            lines = fh.read().split('\n')
        self.stats['files'] += 1
        for i, ln in enumerate(lines, 1):
            s = ln.strip()
            if not s.startswith('///'):
                continue
            body = s[3:]
            for cm in CREF.finditer(body):
                ref = re.sub(r'^[TMPFE]:', '', cm.group(1)).split('(')[0]
                if ref and not ref.startswith('!'):
                    self.check_span(ref, rel, i)
            for c in XML_C.finditer(body):
                self.check_span(c.group(1), rel, i)
            for sp in BACKTICK.finditer(body):
                self.check_span(sp.group(1), rel, i)
            self.file_refs(body, rel, i)

    def run(self, export_root, doc_paths, xml_paths):
        for path, rel in doc_paths:
            if rel.endswith('.md'):
                self.markdown(path, rel)
        for path, rel in xml_paths:
            self.xml_comments(path, rel)
        return self.stats
