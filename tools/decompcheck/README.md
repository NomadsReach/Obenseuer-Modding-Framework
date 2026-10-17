# decompcheck

Checks that the public export only names game and framework code that exists in the game decompile.
Python 3 standard library only. Run with `python` (not `python3`).

```
python decompcheck.py selftest
python decompcheck.py [--export DIR] [--decompile DIR] [--out DIR] [--only code|docs]
```

Defaults: export `C:\Users\Ricky\source\ObenseuerPublicExport`, decompile
`C:\Users\Ricky\source\ObenseuerReverseEng\Game\generated\25969064`, output `%LOCALAPPDATA%\ObenseuerDecompCheck\`
(`decompcheck-report.md` and `decompcheck-report.json`). Exit code 1 when any FAIL is found.

## Files

- `decompcheck.py`: CLI, runs both checks, writes the report, `selftest` entry point.
- `csparse.py`: regex tokenizer and declaration parser for ILSpy C# (namespaces, types, nesting, generics, members, params).
- `csindex.py`: per-assembly index, suffix lookup (`Outer.Inner`, `Outer+Inner`, `Name`N), inheritance walk.
- `resolve.py`: game plus framework universe, member and overload lookup, Harmony parameter rules, helper discovery.
- `codechecks.py`: scans every `.cs` in the export (including `tools`, `Examples`, `Starter`).
- `docchecks.py`: `docs/**/*.md`, `README.md`, `Starter/README.md`, `CHANGELOG.md`, and `///` comments in `src`.
- `selftest.py`: offline asserts for parsing, inheritance, Harmony attributes, backticks, file refs, argument splitting.

## Code checks

- `AccessTools.Field/Method/Property/PropertyGetter/PropertySetter/Declared*/Constructor/Inner/TypeByName/FieldRefAccess`.
- `typeof(T).GetField/GetMethod/GetProperty/GetNestedType/GetConstructor/GetEvent("name", ...)`.
- `GetType("Namespace.Type")` strings, `[HarmonyPatch(typeof(T), "name")]` attributes.
- Framework helpers taking a `Type` and a `string` (for example `CorePatcher.PatchCore/Patch/Prefix/Postfix/Handler`,
  and per-file `Hook` helpers). They are discovered from the framework source. A helper call is checked only when its
  arity fits and its name argument is a literal, a `nameof`, or a variable (variables become WARN).
- Overloads: when the call passes a `new[] { typeof(...) }` or `Type.EmptyTypes` list, the parameter count and simple type names must match.
- Handlers: `Handler(typeof(H), "P")` and string handler names must be static methods of `H`.
- Harmony injected parameters: `___field` must be a field of the patched type or a base. Other names must be
  parameters of the patched method. Allowed specials: `__instance`, `__result`, `__state`, `__args`,
  `__originalMethod`, `__runOriginal`, `__exception`, `__n`, `__<digits>`.
- Accessor names `get_X`/`set_X` count as property `X`. `nameof(...)` is skipped.

## Docs checks

- Backticked spans and `<c>`/`cref` in `///` comments: `Type`, `Type.Member`, `Type::Member`, `Type.Member(...)`.
  The longest prefix that resolves to a type is the head, and the next segment must be a member.
- Unresolved heads are counted, not reported (plain words, config keys, file names, external APIs).
- Csharp code blocks: `Type.Member` with a resolving head, WARN only.
- `.cs` references: must exist in the export or the decompile (trailing-path match, category folder optional).
  A `:N` line past the end of the file is a WARN. Decompile references are listed in the report.

## Heuristics that turn a would-be FAIL into WARN or OK

- Member missing on a type with an external base (MonoBehaviour and similar): WARN, because inherited members
  cannot be verified. Names in `UNITY_INHERITED` pass.
- Short name shared by several types (for example `Data`): WARN, not FAIL.
- Name that is a string literal in the type's own source file (config keys such as `CostMultiplier`): WARN.
- Span preceded by `no`, `not`, `without`, `never`, `removed`: skipped as negated (`There is no Invoices.Pay`).
- `using X = Y;` aliases are expanded per file. Types declared in the same folder are visible to that folder's files.
- External allowlists: `EXTERNAL_TYPES` and `EXTERNAL_PREFIXES` (resolve.py) apply to code references that do not
  resolve. `DOC_EXTERNAL` (docchecks.py) applies to docs heads that resolve only to game types, not framework types.
- `ILLUSTRATIVE_FILES` (docchecks.py): sample mod file names used in guides (`ExampleCounter.cs`, `InfoPanel.cs`).
- Helper target type: a `Type` parameter is the owner only if the body uses it in a `CorePatcher`, `AccessTools`
  or `.Get...` statement. Otherwise the owner is the `typeof` in the body's `CorePatcher`/`AccessTools` statements.

## Known limits

- ILSpy does not emit compiler-generated types (`<Start>d__1`, `<>c`). They are counted as "compiler-generated" in the
  types-*.txt cross-check, not as parse failures.
- Dynamic receivers and names (variables, string concatenation) are WARN, not checked.
- `[HarmonyPatch]` attributes are checked only when both the class `typeof` and the name are on the same attribute.
- `Traverse` calls are WARN only (receiver type is not static). The export has none.
- Overload checks compare simple type names; generic parameters are wildcards.
- `string` literals are compared exactly; `Type.Member` spans are not checked when the full span is a literal in the export.
