import re

from codechecks import HARMONY_ATTR, TYPEOF_ONE, call_args, literal, mask, strip_comments
from csindex import ALL_KINDS, Index, Parser, split_top
from docchecks import BACKTICK, FILE_REF, HEAD, match_rels
from resolve import Universe, type_match

SAMPLE = '''
namespace Demo.Inner {
  public class Outer<T> : Base, IThing where T : class {
    public int Count;
    private static readonly string label = "x { ; }";
    public event System.Action Changed;
    public string Name { get; private set; }
    public Outer() { }
    public void Run(int a, ref string b = null) { if (a > 0) { } }
    public T Get<T>(int k) where T : class { return null; }
    public class Nested { public enum Mode { A, B = 2, C } }
    public enum Kind : byte { Rat, Cat }
    public delegate void Tick(int n);
  }
  public struct Pt { public float X, Y; }
}
'''

BASE_SRC = '''
public class Base {
    public void Hello() { }
    protected int hp;
}
public class Child : Base { }
public class Hooked : MonoBehaviour { public void Own() { } }
'''


def parse(asm, rel, text):
    idx = Index(asm)
    Parser(idx, rel, text).run()
    return idx


def check_parser():
    idx = parse('game', 'sample.cs', SAMPLE)
    outer = idx.by_full['Demo.Inner.Outer']
    assert outer.arity == 1, outer.arity
    assert outer.bases == ['Base', 'IThing'], outer.bases
    assert [p for p in outer.methods['Run'][0].params] == [('int', 'a'), ('ref string', 'b')], outer.methods['Run'][0].params
    assert '.ctor' in outer.methods
    assert 'Get' in outer.methods
    assert {'Count', 'label'} <= outer.fields, outer.fields
    assert 'Changed' in outer.events and 'Name' in outer.props
    nested = idx.lookup('Outer+Nested')
    assert nested and nested[0].full == 'Demo.Inner.Outer.Nested', [t.full for t in nested]
    assert idx.lookup('Outer`1')[0] is outer
    mode = idx.by_full['Demo.Inner.Outer.Nested.Mode']
    assert mode.kind == 'enum' and {'A', 'B', 'C'} <= mode.fields, mode.fields
    assert idx.by_full['Demo.Inner.Outer.Kind'].fields == {'Rat', 'Cat'}
    assert idx.by_full['Demo.Inner.Outer.Tick'].kind == 'delegate'
    assert idx.by_full['Demo.Inner.Pt'].fields == {'X', 'Y'}


def check_inheritance():
    fw = Index('framework')
    Parser(fw, 'base.cs', '').run()
    game = parse('game', 'base.cs', BASE_SRC)
    univ = Universe(game, fw)
    child = univ.find('Child')
    assert univ.member(child, 'Hello')[0] == 'ok'
    assert univ.member(child, 'Nope')[0] == 'fail'
    hooked = univ.find('Hooked')
    assert univ.member(hooked, 'Missing')[0] == 'warn'
    assert univ.member(hooked, 'gameObject')[0] == 'ok'
    assert univ.member(univ.find('Base'), 'hp', {'field'})[0] == 'ok'


def check_harmony_attr():
    text = '[HarmonyPatch(typeof(Foo), "Bar")]\nvoid X() { }'
    clean = strip_comments(text)
    m = HARMONY_ATTR.search(mask(clean))
    assert m is not None
    args = split_top(clean[m.start(1):m.end(1)])
    assert TYPEOF_ONE.match(args[0]).group(1) == 'Foo'
    assert literal(args[1]) == 'Bar'


def check_backticks():
    spans = [s.group(1) for s in BACKTICK.finditer('Use `Hud.ShowHint("x")` and `Items/Osrat.cs:12`.')]
    assert spans == ['Hud.ShowHint("x")', 'Items/Osrat.cs:12'], spans
    h = HEAD.match('Hud.ShowHint("x")')
    assert h.group(1) == 'Hud.ShowHint' and h.group(0) + '("x")' == 'Hud.ShowHint("x")'
    assert HEAD.match('Items/Osrat.cs').group(1) == 'Items'
    assert HEAD.match('Items/Osrat.cs').group(0) + '/Osrat.cs' == 'Items/Osrat.cs'
    assert HEAD.match('1abc') is None


def check_file_refs():
    refs = [(m.group(1), m.group(2)) for m in FILE_REF.finditer('see `NPC\\CatEars.cs:123` and Items/Osrat.cs, not Foo.csproj')]
    assert refs == [('NPC\\CatEars.cs', '123'), ('Items/Osrat.cs', None)], refs
    rels = ['Assembly-CSharp/NPC/CatEars.cs', 'Assembly-CSharp/Items/Osrat.cs', 'Assembly-CSharp/NPC/NPC/CatEars.cs']
    assert match_rels('NPC/CatEars.cs', rels) == ['Assembly-CSharp/NPC/CatEars.cs', 'Assembly-CSharp/NPC/NPC/CatEars.cs']
    assert match_rels('Items/Osrat.cs', rels) == ['Assembly-CSharp/Items/Osrat.cs']
    assert match_rels('Osrat.cs', rels) == ['Assembly-CSharp/Items/Osrat.cs']
    assert match_rels('CatEars.cs', ['Assembly-CSharp/NPC/CatEarsX.cs']) == []


def check_args_and_types():
    text = mask('F("a,b", typeof(X<A,B>), c)')
    segs, end = call_args(text, text.index('('))
    assert len(segs) == 3, segs
    assert type_match('ref ItemLiquidData', 'ItemLiquidData')
    assert type_match('T', 'Anything')
    assert not type_match('int', 'string')


def run():
    checks = [check_parser, check_inheritance, check_harmony_attr, check_backticks, check_file_refs,
              check_args_and_types]
    for fn in checks:
        fn()
    return len(checks)
