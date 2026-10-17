---
title: Localization
parent: Settings
grand_parent: Framework basics
nav_order: 4
---

# Localization

`Localization` reads the language the game is set to, and finds the game's own text in that language.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Localization</code> <span>reads the language and text</span></p>

This is the class to start from. `Language()` gives the language the game is set to, and `Localize` finds the game's text for a key in that language.

<div class="api-pair" markdown="1">

```csharp
public static class Localization
{
    public static string Language();
    public static string Localize(string key);
    public static string Localize(string table, string key);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Language()` | `string` | The name of the language the game is set to, such as English. |
| `Localize(key)` | `string` | The game's text for a key, in that language. |
| `Localize(table, key)` | `string` | The same, from any other table of text. |

</div>
</div>

## Things to know

- `Language()` gives the name of the language the player picked, for example `English`. <span class="tech">(it reads `LocalizationSettings.SelectedLocale.LocaleName`)</span>
- `Localize(key)` gives the game's text for a key, in the player's language. A key is the name the game uses for a piece of text. It looks in the `Menu` table, which is the only table the game itself reads.
- `Localize(table, key)` looks in any other table of text.
- The framework finds the Unity localization code only when it runs. It does not need a reference to it to build. <span class="tech">(the class reaches `UnityEngine.Localization` through reflection)</span>

## Limits

- `Language()` gives `""` until the game's language system has started.
- When there is no translation for the key, you get the key back. The game's own menu text does the same. <span class="tech">(this matches how the game's `MenuLocalization` falls back)</span>
