import os

from csparse import Parser, mask_strings, normalize_ref, split_top, strip_comments

OBJECT_BASES = {'object', 'System.Object', 'Object', 'ValueType', 'System.ValueType', 'Enum', 'System.Enum'}
UNITY_INHERITED = {
    'gameObject', 'transform', 'name', 'tag', 'enabled', 'isActiveAndEnabled', 'hideFlags', 'GetComponent',
    'GetComponents', 'GetComponentInChildren', 'GetComponentInParent', 'TryGetComponent', 'StartCoroutine',
    'StopCoroutine', 'StopAllCoroutines', 'Invoke', 'InvokeRepeating', 'CancelInvoke', 'IsInvoking',
    'SendMessage', 'BroadcastMessage', 'SendMessageUpwards', 'GetInstanceID', 'CompareTag', 'ToString', 'Equals',
    'GetHashCode', 'GetType', 'MemberwiseClone', 'Finalize', 'ReferenceEquals', 'Awake', 'Start', 'Update',
    'FixedUpdate', 'LateUpdate', 'OnEnable', 'OnDisable', 'OnDestroy', 'OnValidate', 'Reset', 'OnGUI',
    'OnCollisionEnter', 'OnCollisionExit', 'OnTriggerEnter', 'OnTriggerExit', 'OnTriggerStay', 'OnBecameVisible',
    'OnBecameInvisible', 'OnApplicationQuit', 'OnApplicationFocus', 'OnApplicationPause', 'UpdateCoroutine',
    'destroyCancellationToken', 'Destroy', 'DestroyImmediate', 'DontDestroyOnLoad',
    'Instantiate', 'FindObjectOfType', 'FindObjectsOfType', 'FindFirstObjectByType', 'FindAnyObjectByType',
}
ALL_KINDS = ('method', 'field', 'prop', 'event', 'type')


class Index:
    def __init__(self, asm):
        self.asm = asm
        self.by_full = {}
        self.by_suffix = {}
        self.namespaces = set()
        self.files = 0
        self.parse_errors = []

    def add(self, t):
        existing = self.by_full.get(t.full)
        if existing is not None:
            existing.merge(t)
            return existing
        self.by_full[t.full] = t
        parts = t.full.split('.')
        for i in range(len(parts)):
            self.by_suffix.setdefault('.'.join(parts[i:]), []).append(t)
        if t.ns:
            self.namespaces.add(t.ns.split('.')[0])
        return t

    def lookup(self, ref):
        key = normalize_ref(ref)
        if key in self.by_full:
            return [self.by_full[key]]
        seen = []
        for t in self.by_suffix.get(key, []):
            if t not in seen:
                seen.append(t)
        return seen

    def count(self):
        return len(self.by_full)


def iter_cs(roots):
    for root in roots:
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in ('bin', 'obj', '.git')]
            for fn in filenames:
                if fn.endswith('.cs'):
                    yield os.path.join(dirpath, fn)


def build_index(roots, asm, base_dir):
    index = Index(asm)
    for path in sorted(iter_cs(roots)):
        index.files += 1
        rel = os.path.relpath(path, base_dir).replace('\\', '/')
        try:
            with open(path, 'r', encoding='utf-8-sig', errors='replace') as f:
                text = f.read()
        except OSError as e:
            index.parse_errors.append((rel, str(e)))
            continue
        Parser(index, rel, text).run()
    return index


def chain_members(index_pair, t):
    seen = set()
    external = False
    found_in = []
    stack = [t]
    while stack:
        cur = stack.pop()
        if id(cur) in seen:
            continue
        seen.add(id(cur))
        found_in.append(cur)
        for b in cur.bases:
            bn = normalize_ref(b)
            if bn in OBJECT_BASES or not bn:
                continue
            cands = []
            for idx in index_pair:
                cands = [c for c in idx.lookup(bn) if c.kind != 'delegate']
                if cands:
                    break
            if cands:
                stack.append(cands[0])
            else:
                external = True
    return found_in, external


def member_names(t, kinds):
    names = set()
    if 'method' in kinds:
        names |= set(t.methods)
    if 'field' in kinds:
        names |= t.fields
    if 'prop' in kinds:
        names |= t.props
    if 'event' in kinds:
        names |= t.events
    if 'type' in kinds:
        names |= set(t.nested)
    return names
