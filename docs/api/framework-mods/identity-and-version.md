---
title: Identity and version
parent: Framework mods
grand_parent: Framework basics
nav_order: 5
---

# Identity and version

The id and version are how the framework tells mods apart and checks that a mod is new enough.

## Things to know

- The id is 1 to 64 characters: letters, digits, `.`, `_` and `-`. It names your mod's log, config file and saved data, and it is the name your mod's game patches are filed under.
- A version has 2 to 4 numbers separated by dots, such as `0.1` or `1.2.3`. Missing numbers count as 0, so `0.11`, `0.11.0` and `0.11.0.0` are the same version. The oldest version in a dependency follows the same rule. <span class="tech">(For a BepInEx plugin the version is cut at its first `-` or `+` before it is compared, so `1.2.0-beta.1` and `1.2.0+abc` count as `1.2.0`, and a prerelease counts as equal to its release, not lower. Framework mod versions and dependency minimums do not get this cut, so `1.2.0-beta` is refused for them.)</span>
- Capital and small letters do not matter when ids are compared. This holds against other framework mods and against BepInEx plugin ids.

## Limits

- Text, a sign or a space is refused in a version, so `-beta`, `+1.0` and a space in front of `1.0` do not work.
- The name must not be empty.
- Some ids belong to the framework, and you can't use them. A framework mod whose id is `ObenseuerCore`, `nomad.obenseuer.core`, `ObenseuerCore.ModKeybinds` or `obenseuer.moddingframework`, or starts with one of them, in any letter case, is skipped with a warning. An id that is a Windows device name, such as `con`, `nul` or `aux`, is skipped too, because it could not be used as a file name.
- If two framework mods have the same id, only one loads. The first file when sorted by path loads, and the other is skipped with a warning that names both files.
- A framework mod whose id is already used by an installed BepInEx plugin is skipped with a warning.
