---
title: Localization
parent: Settings
grand_parent: Framework basics
nav_order: 4
---

# Localization

`Localization` reads the language the game is set to, and finds the game's own text in that language.

```csharp
public static class Localization
{
    public static string Language();                         // the name of the language the game is set to, such as English
    public static string Localize(string key);               // the game's text for a key, in that language
    public static string Localize(string table, string key); // the same, from any other table of text
}
```

- **`Language()` gives the name of the language the player picked, for example `English`.** It gives `""` until the game's language system has started.
  - Technical: it reads `LocalizationSettings.SelectedLocale.LocaleName`.
- **`Localize(key)` gives the game's text for a key, in the player's language.** A key is the name the game uses for a piece of text. It looks in the `Menu` table, which is the only table the game itself reads. `Localize(table, key)` looks in any other table of text.
- **When there is no translation for the key, you get the key back.** The game's own menu text does the same.
  - Technical: this matches how the game's `MenuLocalization` falls back.
- **The framework finds the Unity localization code only when it runs.** It does not need a reference to it to build.
  - Technical: the class reaches `UnityEngine.Localization` through reflection.
