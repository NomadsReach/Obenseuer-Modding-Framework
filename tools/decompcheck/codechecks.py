import bisect
import difflib
import os
import re

from csindex import Index, Parser, mask_strings, split_top, strip_comments

ALIAS = re.compile(r'^\s*using\s+(\w+)\s*=\s*([\w.]+)\s*;', re.M)
TYPEOF_ONE = re.compile(r'^typeof\(\s*([^()]+?)\s*\)(?:\s*\.\s*MakeByRefType\(\s*\))?$')
TYPEOF_NESTED = re.compile(r'^typeof\(\s*([^()]+?)\s*\)\s*\.\s*GetNestedType\(\s*"([^"]+)"')
HANDLER_EXPR = re.compile(r'^(?:CorePatcher\s*\.\s*)?Handler\((.*)\)$', re.S)
NEW_TYPES = re.compile(r'^new\s*(?:Type\s*)?\[\s*\d*\s*\]\s*\{(.*)\}$', re.S)
IDENT = re.compile(r'^[A-Za-z_]\w*$')
DECL = re.compile(r'\b(?:public|private|protected|internal|static|virtual|override|sealed|async)\b[^;{}()=]*$')
ACCESS = re.compile(r'\bAccessTools\s*\.\s*(\w+)\s*(<[^()]*>)?\s*\(')
ACCESS_SPEC = {
    'Field': ('field', None), 'DeclaredField': ('field', None),
    'Property': ('prop', None), 'DeclaredProperty': ('prop', None),
    'PropertyGetter': ('prop', None), 'PropertySetter': ('prop', None),
    'Method': ('method', 2), 'DeclaredMethod': ('method', 2),
    'Constructor': ('.ctor', 1), 'DeclaredConstructor': ('.ctor', 1),
    'Inner': ('type', None), 'FieldRefAccess': ('field', None),
}
TYPEOF_RECV = re.compile(r'\btypeof\(\s*([^()]+?)\s*\)\s*\.\s*(GetField|GetMethod|GetProperty|GetNestedType|GetConstructor|GetEvent)\s*\(')
GETMEMBER = re.compile(r'\.\s*(GetField|GetMethod|GetProperty|GetNestedType|GetConstructor|GetEvent)\s*\(')
TYPEOF_TAIL = re.compile(r'typeof\(\s*[^()]+?\s*\)\s*$')
GETTYPE = re.compile(r'(?<![\w.])GetType\(\s*"([^"]+)"\s*\)')
RECV_KIND = {'GetField': 'field', 'GetProperty': 'prop', 'GetNestedType': 'type', 'GetEvent': 'event',
             'GetMethod': 'method', 'GetConstructor': '.ctor'}
TRAVERSE = re.compile(r'\bTraverse\s*\.\s*Create\s*\(')
TRAVERSE_CALL = re.compile(r'\.\s*(Field|Property|Method)\(\s*"([^"]+)"')
HARMONY_ATTR = re.compile(r'\[HarmonyPatch\((.*?)\)\]', re.S)


mask = mask_strings


def rel_path(path, root):
    return os.path.relpath(path, root).replace('\\', '/')


def call_args(masked, p):
    depth = 0
    segs = []
    start = p + 1
    for i in range(p, len(masked)):
        c = masked[i]
        if c in '([{':
            depth += 1
        elif c in ')]}':
            depth -= 1
            if depth == 0:
                segs.append((start, i))
                return segs, i
        elif c == ',' and depth == 1:
            segs.append((start, i))
            start = i + 1
    return segs, len(masked) - 1


def literal(expr):
    if expr and expr.startswith('"') and expr.endswith('"') and len(expr) >= 2:
        return expr[1:-1]
    return None


def parse_types(expr):
    e = expr.strip()
    if e == 'Type.EmptyTypes':
        return []
    m = NEW_TYPES.match(e)
    if not m:
        return None
    items = [x for x in split_top(m.group(1)) if x]
    out = []
    for it in items:
        t = TYPEOF_ONE.match(it)
        if not t:
            return None
        out.append(t.group(1))
    return out


class Sink:
    def __init__(self, section):
        self.section = section
        self.findings = []
        self.counts = {}

    def add(self, status, kind, file, line, ref, message, closest=None, pattern=None):
        self.counts.setdefault(kind, {'ok': 0, 'fail': 0, 'warn': 0})
        self.counts[kind]['ok' if status == 'OK' else status.lower()] += 1
        if status != 'OK':
            self.findings.append({'section': self.section, 'status': status, 'kind': kind, 'file': file,
                                  'line': line, 'ref': ref, 'message': message,
                                  'closest': closest or [], 'pattern': pattern or kind})

    def ok(self, kind):
        self.add('OK', kind, '', 0, '', '')


class CodeScan:
    def __init__(self, univ, helpers, sink):
        self.univ = univ
        self.helpers = helpers
        self.sink = sink
        names = sorted(helpers, key=len, reverse=True)
        self.helper_re = re.compile(r'\b(' + '|'.join(re.escape(n) for n in names) + r')\s*\(') if names else None
        self.all_names = None
        self.aliases = {}
        self.folders = {}

    def resolve_ref(self, ref):
        head = ref.split('.')[0]
        if head in self.aliases:
            ref = self.aliases[head] + ref[len(head):]
        return self.univ.find(ref)

    def closest_type(self, ref):
        if self.all_names is None:
            self.all_names = sorted(set(list(self.univ.idx['game'].by_full) + list(self.univ.idx['framework'].by_full)))
        return difflib.get_close_matches(ref, self.all_names, 3, 0.6)

    def target(self, expr, file, line, ref):
        m = TYPEOF_ONE.match(expr)
        if m:
            x = m.group(1)
            cands = self.resolve_ref(x)
            if cands:
                return 'ok', cands, x
            if self.univ.is_external(x):
                return 'external', [], x
            self.sink.add('FAIL', 'type', file, line, ref, 'type "%s" is not in game or framework index' % x,
                          self.closest_type(x), 'type')
            return 'missing', [], x
        n = TYPEOF_NESTED.match(expr)
        if n:
            outer = self.resolve_ref(n.group(1))
            nested = [c.nested[n.group(2)] for c in outer if n.group(2) in c.nested]
            if nested:
                return 'ok', nested, n.group(2)
            self.sink.add('FAIL', 'type', file, line, ref,
                          'nested type "%s" not found in "%s"' % (n.group(2), n.group(1)), None, 'nested')
            return 'missing', [], n.group(2)
        self.sink.add('WARN', 'dynamic', file, line, ref, 'target type is not a typeof expression: ' + expr[:60],
                      None, 'dynamic type expression')
        return 'dynamic', [], expr

    def name_arg(self, expr, file, line, ref):
        lit = literal(expr)
        if lit is not None:
            return lit
        if expr.startswith('nameof('):
            return None
        self.sink.add('WARN', 'dynamic', file, line, ref, 'member name is not a string literal: ' + expr[:60],
                      None, 'dynamic name')
        return None

    def member(self, kind, cands, name, file, line, ref, pattern_kind):
        if name is None:
            return None
        kinds = set(kind) if isinstance(kind, set) else {'method' if kind == '.ctor' else kind}
        st, note, close = self.univ.member(cands, name, kinds)
        if st == 'ok':
            self.sink.ok(pattern_kind)
        elif st == 'warn':
            self.sink.add('WARN', pattern_kind, file, line, ref, '"%s": %s' % (name, note), close, 'external base')
        else:
            self.sink.add('FAIL', pattern_kind, file, line, ref, '"%s": %s' % (name, note), close, 'member')
        return st

    def overload(self, cands, name, types, file, line, ref, pattern_kind):
        if types is None or not cands:
            return None
        st, ovs = self.univ.overload(cands, name, types)
        if st == 'fail':
            sigs = ['(%s)' % ', '.join(p[0] for p in o.params) for o in ovs][:4]
            self.sink.add('FAIL', pattern_kind, file, line, ref,
                          '"%s" has no overload taking (%s)' % (name, ', '.join(types)), sigs, 'overload')
        elif st == 'ok':
            self.sink.ok(pattern_kind + ' overload')
        return st

    def handler_arg(self, expr, decl, file, line, ref):
        if expr is None or expr == 'null':
            return None
        lit = literal(expr)
        if lit is not None:
            return decl and ([decl], lit)
        m = HANDLER_EXPR.match(expr)
        if m:
            parts = split_top(m.group(1))
            if len(parts) == 2:
                t = TYPEOF_ONE.match(parts[0])
                nm = literal(parts[1])
                if t and nm is not None:
                    return self.resolve_ref(t.group(1)), nm
        self.sink.add('WARN', 'dynamic', file, line, ref, 'handler is not a literal Handler(typeof(H), "name"): ' + expr[:50],
                      None, 'dynamic handler')
        return None

    def harmony(self, tcands, tname, types, handlers, file, line, ref):
        for hc, hname in handlers:
            if not hc or hname is None:
                continue
            hovs = self.univ.overloads(hc, hname)
            if not hovs:
                continue
            for status, issue in self.univ.harmony_issues(tcands, tname, types, hovs):
                self.sink.add(status, 'harmony', file, line, ref,
                              'handler %s: %s' % (hname, issue), None, 'harmony param')
            self.sink.ok('harmony')

    def handler_only(self, h, raw, file, line, ref):
        kind, tcands, _ = self.target(raw[h.i], file, line, ref)
        if kind != 'ok':
            return
        for k, pname, ptype in h.handlers:
            pair = self.handler_arg(raw[k], h.decl, file, line, ref) if k < len(raw) else None
            if pair:
                st, note, close = self.univ.member(pair[0], pair[1], {'method'})
                if st == 'fail':
                    self.sink.add('FAIL', 'helper handler', file, line, ref,
                                  'handler "%s": %s' % (pair[1], note), close, 'helper handler')
                elif st == 'ok':
                    self.sink.ok('helper handler')

    def helper_site(self, h, raw, file, line, ref, name):
        if h.j is None:
            self.handler_only(h, raw, file, line, ref)
            return
        if h.i is not None:
            tcands_kind, tcands, tname_hint = self.target(raw[h.i], file, line, ref)
        else:
            tcands_kind, tcands, tname_hint = 'ok', self.resolve_ref(h.implicit), h.implicit
            if not tcands:
                self.sink.add('FAIL', 'type', file, line, ref, 'implicit type "%s" not in index' % h.implicit,
                              self.closest_type(h.implicit), 'type')
                return
        tname = self.name_arg(raw[h.j], file, line, ref)
        if tcands_kind not in ('ok',):
            return
        if tname is None:
            return
        types = None
        if h.args_idx is not None and h.args_idx < len(raw):
            types = parse_types(raw[h.args_idx])
            if types is None and raw[h.args_idx].strip() != 'null':
                self.sink.add('WARN', 'dynamic', file, line, ref, 'overload types are not a literal list',
                              None, 'dynamic types')
        st = self.member(h.kind, tcands, tname, file, line, ref, 'helper ' + name)
        if st != 'ok':
            return
        if h.name in ('Prefix', 'Postfix', 'Finalizer') and h.decl.name == 'CorePatcher' and len(raw) in (5, 6):
            pair = self.handler_pair((raw[len(raw) - 3], raw[len(raw) - 2]), file, line, ref)
            handlers = [pair] if pair else []
        else:
            handlers = []
            for k, pname, ptype in h.handlers:
                if k < len(raw):
                    pair = self.handler_arg(raw[k], h.decl, file, line, ref)
                    if pair:
                        handlers.append(pair)
        self.overload(tcands, tname, types, file, line, ref, 'helper ' + name)
        if h.name in ('Prefix', 'Postfix', 'Finalizer') and h.decl.name == 'CorePatcher' and len(raw) == 5:
            if len(self.univ.overloads(tcands, tname)) > 1:
                self.sink.add('FAIL', 'overload', file, line, ref,
                              '"%s" is overloaded, name-only patch cannot pick one' % tname, None, 'overloaded name')
        if 'method' in h.kind and handlers:
            self.harmony(tcands, tname, types, handlers, file, line, ref)

    def handler_pair(self, hexpr, file, line, ref):
        t = TYPEOF_ONE.match(hexpr[0].strip())
        nm = literal(hexpr[1].strip())
        if t and nm is not None:
            return self.resolve_ref(t.group(1)), nm
        self.sink.add('WARN', 'dynamic', file, line, ref, 'handler pair is not literal', None, 'dynamic handler')
        return None

    def helper_calls(self, rel, masked, clean):
        nl = [m.start() for m in re.finditer('\n', masked)]
        for m in self.helper_re.finditer(masked):
            name = m.group(1)
            ls = masked.rfind('\n', 0, m.start()) + 1
            if DECL.search(masked[ls:m.start()]):
                continue
            segs, end = call_args(masked, m.end() - 1)
            raw = [clean[s:e].strip() for s, e in segs]
            raw = [r for r in raw if r != '']
            cands = [h for h in self.helpers[name] if h.min_arity <= len(raw) <= h.arity]
            if not cands:
                continue
            same = [h for h in cands if h.file == rel] or cands
            plausible = [h for h in same if (h.i is None or raw[h.i].startswith('typeof') or IDENT.match(raw[h.i]))
                         and (h.j is None or literal(raw[h.j]) is not None or raw[h.j].startswith('nameof')
                              or IDENT.match(raw[h.j]))]
            if not plausible:
                continue
            sigs = {h.signature(): h for h in plausible}
            if len(sigs) > 1:
                self.sink.add('WARN', 'ambiguous', rel, bisect.bisect_right(nl, m.start()) + 1,
                              re.sub(r'\s+', ' ', clean[m.start():end + 1])[:110],
                              'several helper signatures match this call', None, 'ambiguous helper')
                continue
            h = plausible[0]
            line = bisect.bisect_right(nl, m.start()) + 1
            ref = re.sub(r'\s+', ' ', clean[m.start():end + 1])[:110]
            self.helper_site(h, raw, rel, line, ref, name)

    def access_tools(self, rel, masked, clean, nl):
        for m in ACCESS.finditer(masked):
            fn = m.group(1)
            ls = masked.rfind('\n', 0, m.start()) + 1
            if fn == 'TypeByName':
                segs, _ = call_args(masked, m.end() - 1)
                arg = clean[segs[0][0]:segs[0][1]].strip() if segs else ''
                lit = literal(arg)
                line = bisect.bisect_right(nl, m.start()) + 1
                ref = re.sub(r'\s+', ' ', clean[m.start():m.end() + 40])[:110]
                if lit is None:
                    self.sink.add('WARN', 'dynamic', rel, line, ref, 'TypeByName argument is dynamic', None, 'dynamic type expression')
                    continue
                name = lit.split(',')[0].strip()
                if self.resolve_ref(name):
                    self.sink.ok('TypeByName')
                elif self.univ.is_external(name):
                    self.sink.add('WARN', 'external', rel, line, ref, 'TypeByName "%s" is outside the index' % name, None, 'external type')
                else:
                    self.sink.add('FAIL', 'type', rel, line, ref, 'TypeByName "%s" not in game or framework index' % name,
                                  self.closest_type(name), 'type')
                continue
            spec = ACCESS_SPEC.get(fn)
            if spec is None:
                continue
            kind, types_idx = spec
            segs, _ = call_args(masked, m.end() - 1)
            raw = [clean[s:e].strip() for s, e in segs]
            line = bisect.bisect_right(nl, m.start()) + 1
            ref = re.sub(r'\s+', ' ', clean[m.start():segs[-1][1] + 1 if segs else m.end()])[:110]
            if fn == 'FieldRefAccess' and m.group(2) and len(raw) == 1:
                raw = ['typeof(%s)' % split_top(m.group(2)[1:-1])[0]] + raw
            if len(raw) < 2:
                continue
            st, tcands, tname = self.target(raw[0], rel, line, ref)
            if st != 'ok':
                continue
            name = self.name_arg(raw[1], rel, line, ref)
            if name is None:
                continue
            types = None
            if types_idx is not None and len(raw) > types_idx:
                types = parse_types(raw[types_idx])
            st2 = self.member(kind, tcands, name, rel, line, ref, 'AccessTools.' + fn)
            if st2 == 'ok' and types is not None:
                self.overload(tcands, name, types, rel, line, ref, 'AccessTools.' + fn)

    def typeof_members(self, rel, masked, clean, nl):
        for m in TYPEOF_RECV.finditer(masked):
            segs, _ = call_args(masked, m.end() - 1)
            raw = [clean[s:e].strip() for s, e in segs]
            line = bisect.bisect_right(nl, m.start()) + 1
            ref = re.sub(r'\s+', ' ', clean[m.start():(segs[-1][1] + 1) if segs else m.end()])[:110]
            kind = RECV_KIND[m.group(2)]
            texpr = 'typeof(%s)' % m.group(1)
            st, tcands, _ = self.target(texpr, rel, line, ref)
            if st != 'ok' or not raw:
                continue
            name = self.name_arg(raw[0], rel, line, ref)
            if name is None:
                continue
            st2 = self.member(kind, tcands, name, rel, line, ref, 'typeof.' + m.group(2))
            if st2 == 'ok' and m.group(2) in ('GetMethod',) and len(raw) in (2, 5, 6):
                idx = {2: 1, 5: 3, 6: 4}[len(raw)]
                types = parse_types(raw[idx])
                if types is not None:
                    self.overload(tcands, name, types, rel, line, ref, 'typeof.GetMethod')

    def dynamic_receivers(self, rel, masked, clean, nl):
        for m in GETMEMBER.finditer(masked):
            before = masked[max(0, m.start() - 200):m.start()]
            if TYPEOF_TAIL.search(before.rstrip()):
                continue
            line = bisect.bisect_right(nl, m.start()) + 1
            ref = re.sub(r'\s+', ' ', clean[m.start():m.end() + 30])[:110]
            self.sink.add('WARN', 'dynamic', rel, line, ref.strip(), 'receiver is not typeof(X): type not known statically',
                          None, 'dynamic receiver')

    def gettype_strings(self, rel, masked, clean, nl):
        for m in GETTYPE.finditer(masked):
            name = clean[m.start(1) + 1:m.end(1) - 1].split(',')[0].strip()
            line = bisect.bisect_right(nl, m.start()) + 1
            ref = clean[m.start():m.end()]
            if self.resolve_ref(name):
                self.sink.ok('GetType')
            else:
                self.sink.add('WARN', 'external', rel, line, ref, 'GetType "%s" is not in game or framework index (external assembly?)' % name,
                              None, 'external type')

    def traverse(self, rel, masked, clean, nl):
        if not TRAVERSE.search(masked):
            return
        for m in TRAVERSE_CALL.finditer(masked):
            line = bisect.bisect_right(nl, m.start()) + 1
            self.sink.add('WARN', 'dynamic', rel, line, clean[m.start():m.end()].strip(),
                          'Traverse receiver type is not static', None, 'dynamic receiver')

    def harmony_attrs(self, rel, masked, clean, nl):
        for m in HARMONY_ATTR.finditer(masked):
            args = split_top(clean[m.start(1):m.end(1)])
            line = bisect.bisect_right(nl, m.start()) + 1
            ref = clean[m.start():m.end()]
            t = next((TYPEOF_ONE.match(a) for a in args if TYPEOF_ONE.match(a)), None)
            lit = next((literal(a) for a in args if literal(a) is not None), None)
            if t is None or lit is None:
                self.sink.add('WARN', 'dynamic', rel, line, ref, 'HarmonyPatch attribute without typeof and name', None, 'dynamic attribute')
                continue
            kind = 'prop' if any('MethodType.Getter' in a or 'MethodType.Setter' in a for a in args) else 'method'
            st, tcands, _ = self.target('typeof(%s)' % t.group(1), rel, line, ref)
            if st == 'ok':
                self.member(kind, tcands, lit, rel, line, ref, 'HarmonyPatch')

    def folder_index(self, folder, export_root):
        if folder not in self.folders:
            idx = Index('local')
            for fn in sorted(os.listdir(folder)):
                if fn.endswith('.cs'):
                    p = os.path.join(folder, fn)
                    with open(p, 'r', encoding='utf-8-sig', errors='replace') as fh:
                        Parser(idx, rel_path(p, export_root), fh.read()).run()
            self.folders[folder] = idx
        return self.folders[folder]

    def run(self, export_root):
        files = []
        for dp, dn, fn in os.walk(export_root):
            dn[:] = [d for d in dn if d not in ('.git', 'bin', 'obj')]
            for f in fn:
                if f.endswith('.cs'):
                    files.append(os.path.join(dp, f))
        for path in sorted(files):
            rel = os.path.relpath(path, export_root).replace('\\', '/')
            with open(path, 'r', encoding='utf-8-sig', errors='replace') as fh:
                text = fh.read()
            clean = strip_comments(text)
            masked = mask(clean)
            nl = [m.start() for m in re.finditer('\n', clean)]
            self.aliases = {m.group(1): m.group(2) for m in ALIAS.finditer(clean)}
            self.univ.set_local(self.folder_index(os.path.dirname(path), export_root))
            if self.helper_re is not None:
                self.helper_calls(rel, masked, clean)
            self.access_tools(rel, masked, clean, nl)
            self.typeof_members(rel, masked, clean, nl)
            self.dynamic_receivers(rel, masked, clean, nl)
            self.gettype_strings(rel, masked, clean, nl)
            self.traverse(rel, masked, clean, nl)
            self.harmony_attrs(rel, masked, clean, nl)
        return len(files)
