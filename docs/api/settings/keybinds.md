---
title: Keybinds
parent: Settings
grand_parent: Framework basics
nav_order: 3
---

# Keybinds

This page uses `KeyCode` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

`Keybinds` reads the keys the player bound to the game's actions, and registers keys for your mod. A key you register is saved in a config file and can be changed in the Mods menu.

Game actions use the game's own names, written exactly as the game spells them:

`Forward`, `Backward`, `Left`, `Right`, `Walk`, `Crouch`, `Jump`, `Interact`, `Menu`, `Inventory`, `Map`, `Tasks`, `Tenement`, `Character`, `Notes`, `Pause Menu`, `Hide Hud`, `Hints`, `Primary Use`, `Auxiliary Use`, `Head Use`, `Primary Reload`, `Auxiliary Reload`, `Building Menu`, `Move Left`, `Move Right`, `Place Take`, `Rotate Object`, `Rotation Mode`, `Snap Object`, `Buy Sell`, `Resource Slots`, `Move Item`, `Drag And Drop`, `Drop`, `Switch Tab Left`, `Switch Tab Right`, `Use Item`, `Exit Cancel`, `screenshot`, `screenshotNoUI`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Keybinds</code> <span>reads and registers keys</span></p>

This is the class to start from. `Key`, `Button`, `Label`, `Down` and `Held` read the keys the player bound to a game action, and `Register` adds a key for your mod.

<div class="api-pair" markdown="1">

```csharp
public static class Keybinds
{
    public static KeyCode Key(string action);
    public static KeyCode Button(string action);
    public static string Label(string action);
    public static bool Down(string action);
    public static bool Held(string action);
    public static ModKeybind Register(string modId, string id, string displayName, KeyCode defaultKey);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Key(action)` | `KeyCode` | The keyboard key the player bound to a game action. |
| `Button(action)` | `KeyCode` | The controller button for a game action. |
| `Label(action)` | `string` | The text to show the player for the action's key or button. |
| `Down(action)` | `bool` | `true` on the frame the player presses the action. |
| `Held(action)` | `bool` | `true` while the player holds the action. |
| `Register(modId, id, displayName, defaultKey)` | `ModKeybind` | Adds a key for your mod. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModKeybind</code> <span>a key your mod registered</span></p>

`Keybinds.Register` gives you one of these. Use its `Down`, `Up` and `Held` methods to check your key, and read `Key` for the main key it is bound to.

<div class="api-pair" markdown="1">

```csharp
public sealed class ModKeybind
{
    public KeyCode Key { get; }
    public bool Down();
    public bool Up();
    public bool Held();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Down()` | `bool` | `true` on the frame the key is pressed. |
| `Up()` | `bool` | `true` on the frame the key is let go. |
| `Held()` | `bool` | `true` while the key is held. |

| Property | Type | What it means |
|---|---|---|
| `Key` | `KeyCode` | The main key it is bound to. |

</div>
</div>

## Things to know

- `Key(action)` gives the keyboard key the player has bound, including any change the player made. It gives `KeyCode.None` when the action has no key or the name is not known.
- `Button(action)` gives the controller button for the action. The game does not let the player rebind controller buttons, so this is the fixed default.
- `Label(action)` gives the text the game shows for the device in use. A keyboard player gets `E`, and a controller player gets `A`. Use it for prompts like `"Press [" + Keybinds.Label("Interact") + "]"`. It gives `""` when the action has no key.
- `Down(action)` and `Held(action)` are `true` when the player presses or holds the bound key or button. They follow the player's rebinds.
- They do not check whether a menu or a conversation is open, so check that yourself before you act on them (for example with `UiState`).
- Reading a key or button can switch the game's input device indicator. A read of a key or button that is held can make the game think the player moved to the keyboard or to the controller, as the game's own reads do. <span class="tech">(it can change `InputManager.inputType`)</span>
- `Register` adds a key for your mod, and gives back the same key if you register it again. It takes your mod id, a short id for the key, the name shown in the Mods menu, and the key it starts on.
- The player can change it in the Mods menu, on the page **Mod Keybinds**. The framework adds that page the first time any mod registers a key. <span class="tech">(the key is saved in `config\ObenseuerCore.ModKeybinds.cfg`, in the section `modId` under the key `id`; calling `Register` again with the same `modId` and `id` returns the same handle)</span>
- `ModKeybind.Key` is the main key only. It does not give the modifier keys. A modifier key is one like Shift or Ctrl that must be held with the main key.
- `Down()`, `Up()` and `Held()` check the main key, and every modifier that is bound must be held. Other keys held at the same time, such as the movement keys, do not stop them from working. <span class="tech">(the main key is read with `Input.GetKeyDown`, `Input.GetKeyUp` or `Input.GetKey`)</span>
- Mod keys are shown in the Mods menu, and not on the game's Key Bindings screen.
- A framework option can run a self-check of this key rule at start. It is the setting `KeyChordSelfCheck` in the `[Debug]` section of the framework's config. When it is on, the framework logs `keychord self-check ok` if the check passes.

## Limits

- `Register` gives `null` if the key could not be created, for example when the config file cannot be opened. The error is logged. Check for `null`.
- If two mods use the same key, `Register` writes one warning to the log, and both keys still work. The warning names both mods, their ids and their keys.
- The check runs only when `Register` runs. A change the player makes later in the Mods menu is not checked, and a clash with a game action is not found. The player fixes a clash by changing a key in the Mods menu. <span class="tech">(two keys clash when the main key is the same and one set of modifier keys contains the other, so that one press fires both)</span>
- A key value that someone typed into the config file by hand and that cannot be read leaves the key unbound until the player picks a new one. `ModKeybind.Key` is `KeyCode.None` then.
