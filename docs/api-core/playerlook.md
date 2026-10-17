---
title: PlayerLook
parent: Framework core classes
grand_parent: Framework basics
nav_order: 16
---

# PlayerLook

`PlayerLook` reads and changes how the player looks: the body shape, the face and the clothes. A look is plain data (a `Look` object), so you can store it, change it and apply it again. The game rebuilds the body and clothes itself.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerLook</code> <span>read, change and randomise the look</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class PlayerLook
{
    public static event Action<Look> Changed;
    public static bool IsAvailable { get; }
    public static Look Capture();
    public static Look Capture(Appearance appearance);
    public static bool Apply(Look look);
    public static bool Apply(Look look, Appearance appearance);
    public static bool Live(Look look);
    public static bool Live(Look look, Appearance appearance);
    public static Look Randomise();
    public static Look Randomise(Appearance appearance);
    public static Look Vanilla();
    public static IList<string> Options(string slot, bool male);
    public static IList<string> Options(string slot, bool male, Appearance appearance);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Capture()` | `Look` | Reads the player's look. Returns `null` when `IsAvailable` is `false`. |
| `Capture(Appearance appearance)` | `Look` | Reads the look of any `Appearance`, such as a character's look in the game. |
| `Apply(Look look)` | `bool` | Changes the player to this look. Returns `false` when it is not available, or when the game throws. |
| `Apply(Look look, Appearance appearance)` | `bool` | Changes the look of any `Appearance` to this look, without firing `Changed`. Returns `false` when the appearance is not ready, or when the game throws. |
| `Live(Look look)` | `bool` | Changes only the body and face dials, not the clothes. |
| `Live(Look look, Appearance appearance)` | `bool` | Changes only the body and face dials of any `Appearance`, not the clothes, without firing `Changed`. |
| `Randomise()` | `Look` | Rolls a new random look, applies it, and returns it. |
| `Randomise(Appearance appearance)` | `Look` | Rolls a new random look for any `Appearance`, applies it, and returns it, without firing `Changed`. |
| `Vanilla()` | `Look` | A random look made with the game's own random-character rules. It is not applied. |
| `Options(string slot, bool male)` | `IList<string>` | The clothes names you can pick for a slot, in the game's order. |
| `Options(string slot, bool male, Appearance appearance)` | `IList<string>` | Does the same for any `Appearance`. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `true` when the player exists and a look can be read or changed. |

| Event | Gives you | When it fires |
|---|---|---|
| `Changed` | `Look`, the look that was captured | After `Apply(look)`, `Live(look)` or `Randomise()` changes the player's look. The look is `null` when the capture fails. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerLook.Look</code> <span>one complete look: flags and height, public fields</span></p>
<div class="api-pair" markdown="1">

```csharp
[Serializable]
public sealed class Look
{
    public bool set;
    public bool male;
    public bool advanced;
    public float height = 0.8f;
}
```

| Property | Type | What it means |
|---|---|---|
| `set` | `bool` | `true` for a look taken from `Capture`. |
| `male` | `bool` | `true` for the male body. |
| `advanced` | `bool` | `true` when the advanced body dials are used. |
| `height` | `float` | Body height, from 0 to 1. Defaults to 0.8. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerLook.Look</code> <span>body and face dials, all floats</span></p>
<div class="api-pair" markdown="1">

```csharp
[Serializable]
public sealed class Look
{
    public float shape;
    public float gender;
    public float headSize = 1f;
    public float faceSize = 1f;
    public float neckLength;
    public float shoulderWidth;
    public float breastSize;
    public float hipWidth;
    public float handSize = 1f;
    public float footSize = 1f;
    public float lipThickness;
    public float jawPosition;
    public float chinPosition;
    public float eyeSize = 1f;
    public float eyeDistance;
    public float eyePosition;
    public float eyeDepth;
    public float earSize = 1f;
    public float earRotation;
    public float noseRootRotation;
    public float noseTipRotation;
    public float noseWidth;
}
```

| Property | Type | What it means |
|---|---|---|
| `shape` | `float` | A dial from -1 to 1. Defaults to 0. |
| `gender` | `float` | A dial from 0 to 1. Defaults to 0. |
| `headSize` | `float` | A dial from 0 to 1. Defaults to 1. |
| `faceSize` | `float` | A dial from 0 to 1. Defaults to 1. |
| `neckLength` | `float` | A dial from 0 to 1. Defaults to 0. |
| `shoulderWidth` | `float` | A dial from 0 to 1. Defaults to 0. |
| `breastSize` | `float` | A dial from 0 to 1. Defaults to 0. |
| `hipWidth` | `float` | A dial from 0 to 1. Defaults to 0. |
| `handSize` | `float` | A dial from 0 to 1. Defaults to 1. |
| `footSize` | `float` | A dial from 0 to 1. Defaults to 1. |
| `lipThickness` | `float` | A dial from 0 to 1. Defaults to 0. |
| `jawPosition` | `float` | A dial from 0 to 1. Defaults to 0. |
| `chinPosition` | `float` | A dial from 0 to 1. Defaults to 0. |
| `eyeSize` | `float` | A dial from 0 to 1. Defaults to 1. |
| `eyeDistance` | `float` | A dial from 0 to 1. Defaults to 0. |
| `eyePosition` | `float` | A dial from 0 to 1. Defaults to 0. |
| `eyeDepth` | `float` | A dial from 0 to 1. Defaults to 0. |
| `earSize` | `float` | A dial from 0 to 1. Defaults to 1. |
| `earRotation` | `float` | A dial from 0 to 1. Defaults to 0. |
| `noseRootRotation` | `float` | A dial from 0 to 1. Defaults to 0. |
| `noseTipRotation` | `float` | A dial from 0 to 1. Defaults to 0. |
| `noseWidth` | `float` | A dial from 0 to 1. Defaults to 0. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerLook.Look</code> <span>clothes, each a wearable name</span></p>
<div class="api-pair" markdown="1">

```csharp
[Serializable]
public sealed class Look
{
    public string head = "";
    public string hat = "";
    public string hair = "";
    public string glasses = "";
    public string beard = "";
    public string shirtT = "";
    public string shirtB = "";
    public string pantsT = "";
    public string pantsB = "";
    public string socks = "";
    public string shoes = "";
}
```

| Property | Type | What it means |
|---|---|---|
| `head` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `hat` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `hair` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `glasses` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `beard` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `shirtT` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `shirtB` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `pantsT` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `pantsB` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `socks` | `string` | The name of a wearable asset in the game files. `""` means none. |
| `shoes` | `string` | The name of a wearable asset in the game files. `""` means none. |

</div>
</div>

### Putting it together

Logs every look change, then takes the player's hat off.

<div class="api-example" markdown="1">

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

public class LookTweak : BaseUnityPlugin
{
    private void Awake()
    {
        PlayerLook.Changed += look => Logger.LogInfo("look changed");
    }

    private void Start()
    {
        PlayerLook.Look look = PlayerLook.Capture();
        if (look == null) return;
        look.hat = "";
        PlayerLook.Apply(look);
    }
}
```

| Step | What it does |
|---|---|
| `PlayerLook.Changed += ...` | Logs each look change. |
| `PlayerLook.Capture()` | Reads the player's current look. |
| `look.hat = ""` | Clears the hat in the copy of the look. |
| `PlayerLook.Apply(look)` | Applies the look, which fires `Changed`. |

</div>

### Reading and changing a look

- `Capture()` returns the player's look, or `null` when `IsAvailable` is `false`. A look from `Capture` has `set` true. `Capture(appearance)` reads any `Appearance`.
- `Apply(look)` changes the player to the look you give. Each dial is kept inside its range: height 0 to 1, shape -1 to 1, and every other dial 0 to 1. The game then rebuilds the body and clothes, and `Changed` fires. <span class="tech">(A new costume is built at runtime, so the game's shared Random NPC asset is never written. The rebuild is the game's `LoadCharacter`.)</span>
- `Live(look)` changes only the dials, not the clothes. It sets the body and face dial values on the player and calls `UpdateShapes`. The same limits apply, and `Changed` fires.
- The `Appearance` versions of `Live`, `Randomise` and `Options` work on any `Appearance`, and they do not fire `Changed`. `Live` returns `false` and `Randomise` returns `null` when the `Appearance` is not ready, and `Options` returns an empty list then.
- `Randomise()` rolls a new random look. It captures the current look, rolls every dial except height, rolls each piece of clothing, applies the result, fires `Changed`, and returns the new look. It keeps the captured `male` and `advanced`. Leggings and a female beard always take the first entry, as the game's Random NPC rule does.
- `Vanilla()` gives you a random look made with the game's own random-character rules, and does not apply it. The body is male or female and basic or advanced by a coin toss, breast size is 0 to 0.3 for male and 0.3 to 1 for female, gender is rounded to 0 or 1, and height is included. Only `Vanilla` uses the game's gender ranges. <span class="tech">(It uses `Appearance.LoadCharacterValues`.)</span>

### Clothes and options

- `Options(slot, male)` lists the names you can use for a slot, in the game's order. The slots are `Head`, `Hat`, `Hair`, `Glasses`, `Beard`, `Jacket`, `Shirt`, `Pants`, `Leggings`, `Socks` and `Shoes`. An unknown slot throws `ArgumentException`. When not available, it returns an empty list. A female beard gives the same full list as a male one, because the game's editor offers every beard to either gender.
- Clothes are matched by name within the list for the look's gender. A name that is not found clears the slot, except `head`, which falls back to the first head of that gender. Leggings are matched by name too.
- The head list follows `male`. A female beard is not forced to one entry in `Options` or `Apply`, and only `Randomise` and `Vanilla` roll it as its first entry. Breast size is not limited by gender in `Apply`, `Live` or `Randomise`: `Randomise` rolls it over 0 to 1 for either gender.

### Events and saving

- A handler that throws is logged, and the other handlers still run. The `Appearance` versions do not fire `Changed`.
- Nothing is saved. Store a `Look` with `SaveData.Register` if you need it.

### Errors and checks

- A null `look` or `appearance` throws `ArgumentNullException`. The exception is `Options`, which returns an empty list for a null `appearance`.
- A game error is logged and the call returns `false` or `null`. The log is `BepInEx\logs\obenseuer.moddingframework.playerlook.log`.
- The debug check is `[Debug] PlayerLookSelfCheck = false` in `BepInEx\config\obenseuer.moddingframework.cfg`. When it is true, the framework runs a check when it starts and logs `playerlook self-check ok`, or `playerlook self-check failed: <check>`, to `BepInEx\logs\ObenseuerCore.log`. The check covers the dial limits and the clothes rules, on temporary `Wearable` assets. It does not touch an `Appearance`.

### Still to be checked in the game

- `Apply`, `Live` and `Randomise` on a running player: the body and clothes after `LoadCharacter`.
- `IsAvailable` on the main menu and during scene changes.

The UI kit (`ModUi`, `UiTheme`, `UiSprites`) is on the [UI kit](../api/ui.md) page.
