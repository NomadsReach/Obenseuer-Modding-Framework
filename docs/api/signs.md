---
title: Signs
parent: Objects and devices
grand_parent: API reference
nav_order: 15
---

# Signs

## What it is

Some signs in the game can be written on: the player walks up, types a message, and the sign keeps it. This API lets your mod list those signs in the scene you are in, read what each one says, change what it says, and get told when the text changes. The text is part of the save, so it is still there after a load.

## When to use it

- Write a message on a sign from your mod, for example to show a price or a notice.
- React when the player finishes writing on a sign.
- Read what the player has written on their signs.

## Quick example

This mod writes a log line each time a sign's text changes, either because the player wrote on it or because a mod did. Once a save is loaded, it also logs how many signs are in the scene.

```csharp
using ObenseuerCore;

[ModInfo("example.signwatch", "Sign Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class SignWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Signs.TextChanged += sign => Log.Info("Sign " + sign.Id + " now reads: " + sign.Text);
        Events.WhenReady(() => Log.Info(Signs.All().Count + " signs in this scene."));
    }
}
```

`TextChanged` is an event: the framework calls your code each time it happens. `WhenReady` waits until a save is loaded before it runs your code. See [Using events](../guides/getting-started/using-events.md) for more on both.

## Common tasks

List every sign with its text and how much text it can hold.

```csharp
foreach (SignInfo sign in Signs.All())
    Log.Info(sign.Id + ": \"" + sign.Text + "\" (" + sign.MaxLines + " lines, " + sign.MaxLength + " characters at most)");
```

Write on the first sign. `SetText` returns `false` when the sign does not accept the text, for example because it is too long.

```csharp
SignInfo first = Signs.All().Count > 0 ? Signs.All()[0] : null;
if (first != null && !Signs.SetText(first.Id, "Closed for repairs"))
    Log.Info("The sign did not accept that text.");
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Signs</code> <span>finds, reads and writes signs</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Signs
{
    public static List<SignInfo> All();
    public static SignInfo Get(string id);
    public static bool SetText(string id, string text);
    public static bool IsAvailable { get; }
    public static event Action<SignTextChangedArgs> TextChanged;
}
```

| Method | Returns | What it does |
|---|---|---|
| `All()` | `List<SignInfo>` | Every sign in the scene you are in. |
| `Get(string id)` | `SignInfo` | The sign with this id. `null` when there is none. |
| `SetText(string id, string text)` | `bool` | Writes the text on the sign. `true` when it did. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when edits by the player could not be tracked. |

| Event | Gives you | When it fires |
|---|---|---|
| `TextChanged` | `SignTextChangedArgs` | A sign's text has changed. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SignInfo</code> <span>a snapshot of one sign</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SignInfo
{
    public string Id { get; }
    public string Text { get; }
    public int MaxLines { get; }
    public int MaxLength { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | A name for this sign that you can use with `Get` and `SetText`. |
| `Text` | `string` | What the sign says now. |
| `MaxLines` | `int` | The most lines it holds. 1 for a one-line sign. |
| `MaxLength` | `int` | The most characters it holds. 0 when there is no limit. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SignTextChangedArgs</code> <span>what TextChanged tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SignTextChangedArgs
{
    public string Id { get; }
    public string Text { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The sign's id, the same as `SignInfo.Id`. |
| `Text` | `string` | The new text. |

</div>
</div>

### Things to know

- `All()` lists the signs that are switched on in the scene you are in. A sign the game has switched off is not listed. With no save loaded, or on the main menu, it gives an empty list. <span class="tech">(It lists every `EditableSign` on an active object in the active scene.)</span>
- `Get(id)` returns `null` when no sign has that id. A null or empty id finds nothing.
- `Id` is a name that identifies one sign. It normally stays the same each time the game runs. A sign the game gave no unique id gets one made from its place in the scene, and that id changes when the scene changes. <span class="tech">(It is the save GUID, else the scene path.)</span>
- `SetText` writes the text on the sign and updates what the player sees. It does the same as the game does after the player finishes typing. It returns `true` when the text was written. <span class="tech">(It sets `EditableSign.text` and then calls the game's `UpdateText`.)</span>
- `SetText` returns `false` when the sign does not accept the text. That is when no sign has that id, when a save is loading or being written, when the player is typing on that sign right now (the game would overwrite the text every frame), when the text has more characters than `MaxLength` allows (if that is above 0), and when the text has more lines than `MaxLines`. A new line in your text starts a new line on the sign. <span class="tech">(`MaxLength` is the `characterLimit` of the sign's input field. Lines are counted by splitting the text on `\n`.)</span>
- `SetText` never cuts your text short. A mod that wants a shorter text must shorten it before the call.
- `SetText` throws `ArgumentException` when `text` is `null`. Pass an empty string to clear a sign.
- `TextChanged` fires in two cases. It fires after a `SetText` that really changed the text, and it fires when the player finishes typing and the text is different from what it was when they started. It does not fire when the text is the same, and it does not fire when a save loads, because a load puts the text back without going through either path. <span class="tech">(The player's edit is seen through the game's `EditableSign.StartEditing` and `StopEditing`. If those changes could not be made, `TextChanged` still fires for `SetText`, because `SetText` raises it directly.)</span>

### What you can't do (yet)

- You can't start a player's edit from your mod. The player types on a sign through the game's own keyboard code. The only way to see an edit is `TextChanged`. <span class="tech">(`Interact`, `StartEditing` and the keyboard handling are not wrapped. The private `editing` static, which tracks the one sign being edited, is not needed for the reads.)</span>
- You can't change a sign's font, colour or position. <span class="tech">(`textComponent` is not wrapped.)</span>

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.signs.log`.

Everything not listed in this reference is internal and may change without notice.

### When you can call it

- Call everything from your mod's normal code (`OnLoad`, event handlers, `Update`), not from a background thread you started. The game only works safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time. On the main menu or with no save loaded, `All()` gives an empty list, `Get` gives `null` and `SetText` gives `false`.
- Nothing throws an error to you, except `SetText` with a `null` text.
- Your `TextChanged` handlers run inside the game's own call, right after the text has changed, one at a time. If one of your handlers throws an error, the framework logs it and still runs the other handlers.

### If something fails to set up

The framework changes two of the game's methods when it starts, so it can see when the player edits a sign. If a game update breaks one of them, only the tracking of the player's edits turns off, and the framework writes error lines and a warning to the log. Its feature group is `obenseuer.moddingframework.signs`, the name you will see in the log. A feature group is the named set of the framework's changes for one feature.

- `Signs.IsAvailable` is `false` when the tracking is switched off. `All`, `Get` and `SetText` keep working. `TextChanged` stops for the player's edits, but it still fires for `SetText`. <span class="tech">(The methods are `EditableSign.StartEditing` and `StopEditing`.)</span>
