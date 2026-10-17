import difflib
import re

from csindex import ALL_KINDS, UNITY_INHERITED, chain_members, member_names, normalize_ref

PARAM_MODS = {'ref', 'out', 'in', 'params', 'this', 'scoped', 'readonly'}
ALIASES = {'String': 'string', 'Int32': 'int', 'Single': 'float', 'Boolean': 'bool', 'Object': 'object',
           'Int64': 'long', 'Double': 'double', 'Byte': 'byte'}
WILDCARD = re.compile(r'^[A-Z]\d?$')
HANDLER_PARAM = re.compile(r'prefix|postfix|finalizer', re.I)
HARMONY_SPECIAL = re.compile(r'^(__instance|__result|__state|__args|__originalMethod|__runOriginal|__exception|__n|__\d+)$')
EXTERNAL_TYPES = {
    'object', 'string', 'int', 'float', 'bool', 'byte', 'long', 'double', 'char', 'void', 'uint', 'short',
    'decimal', 'Type', 'Mathf', 'Object', 'Math', 'Enum', 'Array', 'Exception', 'Action', 'Func', 'Delegate',
    'MonoBehaviour', 'GameObject', 'Transform', 'Component', 'Animator', 'Renderer', 'Collider', 'Rigidbody',
    'Vector3', 'Vector2', 'Quaternion', 'Color', 'Time', 'Input', 'Debug', 'Application', 'Random', 'Resources',
    'Harmony', 'HarmonyMethod', 'MethodInfo', 'FieldInfo', 'PropertyInfo', 'MethodBase', 'BindingFlags',
    'Dictionary', 'List', 'HashSet', 'Queue', 'Stack', 'IEnumerable', 'IEnumerator', 'StringBuilder', 'Regex',
    'Assembly', 'Thread', 'Monitor', 'Stopwatch', 'DateTime', 'TimeSpan', 'Guid', 'Encoding', 'File', 'Path',
    'Directory', 'JsonUtility', 'PlayerPrefs', 'Scene', 'SceneManager', 'Canvas', 'Image', 'Text', 'Button',
    'Sprite', 'Texture2D', 'Material', 'Shader', 'Camera', 'Light', 'AudioSource', 'AudioClip', 'Collision',
    'RaycastHit', 'Ray', 'Bounds', 'Rect', 'Matrix4x4', 'Plane', 'Physics', 'Coroutine', 'WaitForSeconds',
    'TMP_Text', 'TextMeshProUGUI', 'Selectable', 'EventSystem', 'Slider', 'Toggle', 'Dropdown', 'InputField',
    'Keyboard', 'Mouse', 'Gamepad', 'Cursor', 'Screen', 'QualitySettings', 'Shader', 'LayerMask', 'Physics2D',
    'ScriptableObject', 'SerializeField', 'Attribute', 'Nullable', 'Tuple', 'ValueTuple', 'Span', 'Memory',
    'CultureInfo', 'NumberStyles', 'Convert', 'BitConverter', 'Buffer', 'GC', 'WeakReference', 'Lazy',
    'Predicate', 'Comparison', 'EventArgs', 'EventHandler', 'Exception', 'ArgumentException', 'Logger',
}
EXTERNAL_PREFIXES = ('UnityEngine.', 'System.', 'HarmonyLib.', 'BepInEx.', 'TMPro.', 'Mono.', 'Steamworks.',
                     'Cinemachine.', 'Rewired.', 'DG.Tweening.', 'LitJson.', 'Unity.', 'Microsoft.',
                     'PixelCrushers.', 'Newtonsoft.', 'ConfigurationManager.')


def simple(t):
    s = ' '.join(x for x in t.split() if x not in PARAM_MODS)
    arr = s.count('[]')
    s = normalize_ref(s.replace('[]', ''))
    s = s.split('.')[-1]
    s = ALIASES.get(s, s)
    return s + '[]' * arr


def type_match(ptype, tt):
    ps = simple(ptype)
    ts = simple(tt)
    if WILDCARD.match(ps.replace('[]', '')):
        return True
    return ps == ts


class Universe:
    def __init__(self, game, framework):
        self.idx = {'game': game, 'framework': framework}

    def set_local(self, index):
        if index is None:
            self.idx.pop('local', None)
        else:
            self.idx['local'] = index

    def search(self, asm):
        order = [asm] + [a for a in ('local', 'framework', 'game') if a != asm]
        return [self.idx[a] for a in order if a in self.idx]

    def find(self, ref):
        ref = ref.strip()
        order = ['local'] if 'local' in self.idx else []
        if not ref.startswith('global::'):
            order.append('framework')
        order.append('game')
        out = []
        seen = set()
        for a in order:
            for t in self.idx[a].lookup(ref):
                if t.full not in seen:
                    seen.add(t.full)
                    out.append(t)
        return out

    def is_external(self, ref):
        head = normalize_ref(ref).split('.')[0]
        return normalize_ref(ref) in EXTERNAL_TYPES or head in EXTERNAL_TYPES or ref.strip().startswith(EXTERNAL_PREFIXES)

    def chain(self, cands):
        out = []
        ext = False
        for t in cands:
            found, e = chain_members(self.search(t.asm), t)
            ext = ext or e
            for c in found:
                if c not in out:
                    out.append(c)
        return out, ext

    def member(self, cands, name, kinds=ALL_KINDS):
        if not cands:
            return 'missing', 'type not in game or framework index', []
        if name == '.ctor' and any(t.kind in ('class', 'struct', 'record') for t in cands):
            return 'ok', '', []
        chain, ext = self.chain(cands)
        names = set()
        accessor = name[4:] if name.startswith(('get_', 'set_')) else None
        for c in chain:
            cn = member_names(c, kinds)
            if name in cn or (accessor and accessor in member_names(c, {'prop'})):
                return 'ok', '', []
            names |= cn
        close = difflib.get_close_matches(name, sorted(names), 3, 0.6)
        if name in UNITY_INHERITED and ext:
            return 'ok', 'inherited from external base', []
        if ext:
            return 'warn', 'not in index, type has an external base', close
        return 'fail', 'member not in index', close

    def overloads(self, cands, name):
        chain, _ = self.chain(cands)
        out = []
        for c in chain:
            out.extend(c.methods.get(name, []))
        return out

    def overload(self, cands, name, types):
        ovs = self.overloads(cands, name)
        if types is None:
            return 'unknown', ovs
        for o in ovs:
            if len(o.params) == len(types) and all(type_match(p[0], tt) for p, tt in zip(o.params, types)):
                return 'ok', ovs
        if not ovs:
            return ('fail' if name == '.ctor' and types else 'skip'), ovs
        return 'fail', ovs

    def harmony_issues(self, tcands, mname, types, handler_ovs):
        ovs = self.overloads(tcands, mname)
        if not ovs:
            return []
        matched = [o for o in ovs if types is None or len(o.params) == len(types)] or ovs
        names = set(p[1] for o in matched for p in o.params)
        issues = []
        for ho in handler_ovs:
            for ptype, pname in ho.params:
                if HARMONY_SPECIAL.match(pname):
                    continue
                if pname.startswith('___'):
                    fld = pname[3:]
                    st = self.member(tcands, fld, {'field'})[0]
                    if st in ('fail', 'warn'):
                        issues.append((st.upper(), 'field "%s" (from %s) not found on patched type' % (fld, pname)))
                    continue
                if pname not in names:
                    issues.append(('FAIL', 'parameter "%s" not in patched method (%s)' % (pname, ', '.join(sorted(names)))))
        return issues


class Helper:
    def __init__(self, name, arity, min_arity, i, j, implicit, args_idx, handlers, decl):
        self.name = name
        self.arity = arity
        self.min_arity = min_arity
        self.i = i
        self.j = j
        self.implicit = implicit
        self.args_idx = args_idx
        self.handlers = handlers
        self.decl = decl
        self.file = decl.file
        self.kind = helper_kind(name)

    def signature(self):
        return (self.i, self.j, self.implicit, self.args_idx, tuple(h[0] for h in self.handlers))


PATCH_HELPERS = {'Handler', 'PatchCore', 'Patch', 'Prefix', 'Postfix', 'Finalizer', 'UniqueNamed', 'Hook'}


def helper_kind(name):
    if name in PATCH_HELPERS:
        return {'method'}
    if 'Field' in name:
        return {'field'}
    if 'Property' in name:
        return {'prop'}
    return set(ALL_KINDS)


def implicit_type(ov, decl):
    found = sorted(set(x.strip() for x in ov.body_typeofs if simple(x) != decl.name))
    found = [x for x in found if simple(x) not in ('string', 'object', 'Type')]
    return found[0] if len(found) == 1 else None


def helper_sig(ov, decl, name):
    ps = [simple(p[0]) for p in ov.params]
    pnames = [p[1] for p in ov.params]
    i = next((k for k, p in enumerate(ps) if p == 'Type'), None)
    if i is not None and not re.search(r'\b%s\b' % re.escape(pnames[i]), ov.owner_text):
        i = None
    start = i + 1 if i is not None else 0
    j = next((k for k in range(start, len(ps)) if ps[k] == 'string' and not HANDLER_PARAM.search(pnames[k])), None)
    if j is None:
        j = next((k for k in range(len(ps)) if ps[k] == 'string' and pnames[k] in ('name', 'method', 'methodName')), None)
    handlers = [(k, pnames[k], ps[k]) for k in range(len(ps)) if k != j and HANDLER_PARAM.search(pnames[k])]
    if j is None and (i is None or not handlers):
        return None
    implicit = None
    if i is None:
        implicit = implicit_type(ov, decl)
        if implicit is None:
            return None
    args_idx = next((k for k, p in enumerate(ps) if p == 'Type[]'), None)
    return Helper(name, len(ov.params), ov.required, i, j, implicit, args_idx, handlers, decl)


def discover_helpers(univ):
    helpers = {}
    for t in univ.idx['framework'].by_full.values():
        for name, ovs in t.methods.items():
            for ov in ovs:
                h = helper_sig(ov, t, name)
                if h is not None:
                    helpers.setdefault(name, []).append(h)
    return helpers
