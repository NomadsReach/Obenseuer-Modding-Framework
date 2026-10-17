import bisect
import re

TYPE_HEAD = re.compile(
    r'^(?:(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|unsafe|new|file)\s+)*'
    r'(class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+'
    r'(<[^\s<>]*>\w*|[A-Za-z_]\w*)\s*(<[^>]*>)?\s*(\([^)]*\))?\s*(:\s*(.*?))?\s*(?:\bwhere\b.*)?$',
    re.S)
DELEGATE_HEAD = re.compile(r'^(?:(?:public|private|protected|internal|static|new|unsafe)\s+)*delegate\s+(.*)$', re.S)
NS_HEAD = re.compile(r'^namespace\s+([\w.]+)\s*$')
NAME_PAT = r'(?:<[^\s<>()]*>\w*|[A-Za-z_@]\w*)'
LAST_NAME = re.compile(NAME_PAT + r'\s*$')
MODIFIERS = re.compile(r'^(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|readonly|const|extern|unsafe|async|new|partial|volatile|fixed|required|file)\s+)+')
SPLIT_TOKENS = re.compile(r'([{}\[\]();])')
ATTR = re.compile(r'\[\s*[A-Za-z_@][^\[\]]*\]')
STRIP_PAIRS = re.compile(
    r'//[^\n]*|/\*[\s\S]*?\*/|(?:\$@|@\$|\$|@)?"(?:""|\\.|[^"\\\n])*"|\'(?:\\.|[^\'\\\n])+\'')
LITERAL = re.compile(r'"(?:""|\\.|[^"\\\n])*"|\'(?:\\.|[^\'\\\n])+\'')
TYPEOF_IN_BODY = re.compile(r'\btypeof\(\s*([A-Za-z_][\w.+<>, ]*?)\s*\)')
OWNER_HINT = re.compile(r'CorePatcher|AccessTools|\.Get\w*\(')
IMPLICIT_HINT = re.compile(r'CorePatcher|AccessTools')


def strip_comments(text):
    def repl(m):
        s = m.group(0)
        if s.startswith('//') or s.startswith('/*'):
            return ''.join('\n' if c == '\n' else ' ' for c in s)
        return s
    kept = '\n'.join('' if ln.lstrip().startswith('#') else ln for ln in text.split('\n'))
    return STRIP_PAIRS.sub(repl, kept)


def mask_strings(text):
    def repl(m):
        s = m.group(0)
        if s[0] == "'":
            return "' '" if len(s) == 3 else "'" + 'x' * (len(s) - 2) + "'"
        return '"' + 'x' * (len(s) - 2) + '"'
    return LITERAL.sub(repl, text)


def last_name(pre):
    pre = pre.rstrip()
    while pre.endswith('>'):
        depth = 0
        i = len(pre) - 1
        while i >= 0:
            if pre[i] == '>':
                depth += 1
            elif pre[i] == '<':
                depth -= 1
                if depth == 0:
                    break
            i -= 1
        if i <= 0:
            break
        pre = pre[:i].rstrip()
    m = LAST_NAME.search(pre)
    return m.group(0) if m else None


def split_top(s, sep=','):
    parts, depth, cur = [], 0, []
    for ch in s:
        if ch in '([{<':
            depth += 1
        elif ch in ')]}>':
            depth -= 1
        if ch == sep and depth == 0:
            parts.append(''.join(cur))
            cur = []
        else:
            cur.append(ch)
    if ''.join(cur).strip():
        parts.append(''.join(cur))
    return [p.strip() for p in parts if p.strip()]


def normalize_ref(ref):
    s = ref.strip()
    s = re.sub(r'^global::', '', s)
    s = s.replace('::', '.').replace('+', '.')
    prev = None
    while prev != s:
        prev = s
        s = re.sub(r'(?<=\w)<[^<>]*>', '', s)
    s = re.sub(r'`\d+', '', s)
    s = re.sub(r'\s+', '', s)
    return s


class Overload:
    def __init__(self, name, params, file, line):
        self.name = name
        self.params = params
        self.file = file
        self.line = line
        self.body_typeofs = []
        self.owner_text = ''
        self.required = len(params)


class TypeRec:
    def __init__(self, name, arity, full, ns, outer, kind, bases, file, line, asm):
        self.name = name
        self.arity = arity
        self.full = full
        self.ns = ns
        self.outer = outer
        self.kind = kind
        self.bases = bases
        self.file = file
        self.line = line
        self.asm = asm
        self.methods = {}
        self.fields = set()
        self.props = set()
        self.events = set()
        self.nested = {}

    def merge(self, other):
        for k, v in other.methods.items():
            self.methods.setdefault(k, []).extend(v)
        self.fields |= other.fields
        self.props |= other.props
        self.events |= other.events
        for k, v in other.nested.items():
            self.nested.setdefault(k, v)
        for b in other.bases:
            if b not in self.bases:
                self.bases.append(b)


class Parser:
    def __init__(self, index, rel, text):
        self.index = index
        self.rel = rel
        self.clean = mask_strings(strip_comments(text))
        self.nl = [m.start() for m in re.finditer('\n', self.clean)]

    def line_at(self, off):
        return bisect.bisect_left(self.nl, off) + 1

    def run(self):
        stack = [{'k': 'root', 'ns': ''}]
        buf = []
        bracket = 0
        off = 0
        for piece in SPLIT_TOKENS.split(self.clean):
            start = off
            off += len(piece)
            if not piece:
                continue
            top = stack[-1]
            if top['k'] == 'body':
                if piece == '{':
                    stack.append({'k': 'body', 'ov': None, 'start': 0})
                elif piece == '}':
                    done = stack.pop()
                    if done.get('ov') is not None:
                        self_call = re.compile(r'\b%s\s*\(' % re.escape(done['ov'].name))
                        owner = [s for s in self.clean[done['start']:start].split(';')
                                 if OWNER_HINT.search(s) or self_call.search(s)]
                        done['ov'].owner_text = ' '.join(owner)
                        done['ov'].body_typeofs = [x for s in owner if IMPLICIT_HINT.search(s)
                                                   for x in TYPEOF_IN_BODY.findall(s)]
                continue
            if top['k'] == 'enum':
                if piece == '}':
                    self.enum_commit(top)
                    stack.pop()
                elif piece in '{};[]()':
                    top['buf'] += piece
                else:
                    for i, chunk in enumerate(piece.split(',')):
                        if i:
                            self.enum_commit(top)
                        top['buf'] += chunk
                continue
            if piece in ('(', ')'):
                buf.append(piece)
                continue
            if piece == '[':
                bracket += 1
                buf.append(piece)
                continue
            if piece == ']':
                bracket = max(0, bracket - 1)
                buf.append(piece)
                continue
            if len(piece) != 1 or piece not in '{};':
                buf.append(piece)
                continue
            if bracket > 0:
                buf.append(piece)
                continue
            header = ' '.join(''.join(buf).split())
            buf = []
            if piece == '{':
                self.open_block(stack, header, start)
            elif piece == '}':
                if len(stack) > 1 and stack[-1]['k'] in ('type', 'ns'):
                    stack.pop()
            else:
                self.semi(stack, header)
        return self.index

    def owner_type(self, stack):
        for s in reversed(stack):
            if s['k'] == 'type':
                return s['t']
        return None

    def current_ns(self, stack):
        for s in reversed(stack):
            if s['k'] in ('root', 'ns'):
                return s['ns']
        return ''

    def make_type(self, stack, kind, name, arity, bases, off):
        outer = self.owner_type(stack)
        ns = self.current_ns(stack)
        full = (outer.full + '.' + name) if outer else ((ns + '.' + name) if ns else name)
        t = TypeRec(name, arity, full, ns, outer, kind, bases, self.rel, self.line_at(off), self.index.asm)
        t = self.index.add(t)
        if outer is not None:
            outer.nested.setdefault(name, t)
        return t

    def type_head(self, h):
        tm = TYPE_HEAD.match(h)
        if not tm:
            return None
        kind = tm.group(1).split()[0]
        name = tm.group(2)
        arity = len(split_top(tm.group(3)[1:-1])) if tm.group(3) and tm.group(3)[1:-1].strip() else 0
        bases_txt = re.split(r'\swhere\s', ' ' + (tm.group(6) or '') + ' ')[0].strip()
        bases = split_top(bases_txt) if bases_txt else []
        return kind, name, arity, bases

    def open_block(self, stack, header, off):
        h = header
        for _ in range(3):
            h = ATTR.sub('', h)
        h = ' '.join(h.split())
        top = stack[-1]
        if top['k'] in ('root', 'ns'):
            m = NS_HEAD.match(h)
            if m:
                parent = self.current_ns(stack)
                full = (parent + '.' + m.group(1)) if parent else m.group(1)
                stack.append({'k': 'ns', 'ns': full})
                return
            th = self.type_head(h)
            if th:
                kind, name, arity, bases = th
                if kind == 'enum':
                    t = self.make_type(stack, 'enum', name, 0, [], off)
                    stack.append({'k': 'enum', 't': t, 'buf': ''})
                else:
                    t = self.make_type(stack, kind, name, arity, bases, off)
                    stack.append({'k': 'type', 't': t})
                return
            stack.append({'k': 'body', 'ov': None, 'start': 0})
            return
        if top['k'] == 'type':
            th = self.type_head(h)
            if th:
                kind, name, arity, bases = th
                if kind == 'enum':
                    t = self.make_type(stack, 'enum', name, 0, [], off)
                    stack.append({'k': 'enum', 't': t, 'buf': ''})
                else:
                    t = self.make_type(stack, kind, name, arity, bases, off)
                    stack.append({'k': 'type', 't': t})
                return
            ov = self.member(top['t'], h, True, off)
            stack.append({'k': 'body', 'ov': ov, 'start': off + 1})
            return
        stack.append({'k': 'body', 'ov': None, 'start': 0})

    def semi(self, stack, header):
        top = stack[-1]
        if top['k'] not in ('root', 'ns', 'type'):
            return
        h = ' '.join(ATTR.sub('', header).split())
        if not h:
            return
        if top['k'] in ('root', 'ns'):
            nm = NS_HEAD.match(h)
            if nm:
                top['ns'] = nm.group(1)
                return
            dm = DELEGATE_HEAD.match(h)
            if dm:
                name = last_name(dm.group(1).split('(')[0])
                if name:
                    self.make_type(stack, 'delegate', name, 0, [], 0)
            return
        dm = DELEGATE_HEAD.match(h)
        if dm:
            name = last_name(dm.group(1).split('(')[0])
            if name:
                self.make_type(stack, 'delegate', name, 0, [], 0)
            return
        self.member(top['t'], h, False, 0)

    def member(self, t, h, has_body, off):
        h = MODIFIERS.sub('', h).strip()
        h = re.sub(r'^(?:(?:public|private|protected|internal|static)\s+)+', '', h).strip()
        if not h or 'delegate' in h.split('(')[0].split():
            return None
        line = self.line_at(off) if off else 0
        if h.startswith('event '):
            body = h[len('event '):]
            name = last_name(body.split('=')[0].split('{')[0].split('(')[0])
            if name and '(' not in body.split('{')[0]:
                t.events.add(name)
            return None
        eq = re.search(r'(?<![=!<>])=(?![=>])', h)
        paren = next((i for i in range(1, len(h)) if h[i] == '(' and (h[i - 1].isalnum() or h[i - 1] in '_>')), -1)
        arrow = h.find('=>')
        if paren >= 0 and (eq is None or paren < eq.start()) and not (0 <= arrow < paren):
            pre = h[:paren]
            name = last_name(pre)
            if name is None:
                return None
            if 'operator' in pre.split():
                name = 'operator'
            elif name == t.name and len(pre.split()) <= 1:
                name = '.ctor'
            params, required = self.parse_params(h, paren)
            ov = Overload(name, params, self.rel, line)
            ov.required = required
            t.methods.setdefault(name, []).append(ov)
            return ov
        if eq is not None and (paren < 0 or eq.start() < paren):
            for part in split_top(h[:eq.start()]):
                n = last_name(part)
                if n:
                    t.fields.add(n)
            return None
        if arrow >= 0:
            name = last_name(h[:arrow])
            if name:
                t.props.add(name)
            return None
        if 'this[' in h.replace(' ', ''):
            t.props.add('this')
            return None
        if has_body:
            name = last_name(h)
            if name:
                t.props.add(name)
            return None
        for part in split_top(h):
            n = last_name(part)
            if n:
                t.fields.add(n)
        return None

    def parse_params(self, h, paren):
        depth = 0
        end = None
        for i in range(paren, len(h)):
            if h[i] == '(':
                depth += 1
            elif h[i] == ')':
                depth -= 1
                if depth == 0:
                    end = i
                    break
        if end is None:
            return [], 0
        out = []
        required = 0
        for part in split_top(h[paren + 1:end]):
            if '=' not in part:
                required += 1
            part = part.split('=')[0].strip()
            toks = part.split()
            if len(toks) < 1:
                continue
            out.append((' '.join(toks[:-1]), toks[-1]))
        return out, required

    def enum_commit(self, st):
        raw = st['buf'].strip()
        st['buf'] = ''
        m = re.match(r'[A-Za-z_]\w*', raw)
        if m:
            st['t'].fields.add(m.group(0))
