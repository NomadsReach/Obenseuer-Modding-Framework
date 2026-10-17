---
title: PlayerLook
parent: Framework core classes
grand_parent: Framework basics
nav_order: 16
---

# PlayerLook

`PlayerLook` reads and changes how the player looks: the body shape, the face and the clothes. A look is plain data (a `Look` object), so you can store it, change it and apply it again. The game rebuilds the body and clothes itself.

```csharp
public static class PlayerLook
{
    public static event Action<Look> Changed;       // the player's look was changed through PlayerLook
    public static bool IsAvailable { get; }         // true when the player exists and a look can be read or changed
    public static Look Capture();                   // read the player's look; null when not available
    public static Look Capture(Appearance appearance);   // read the look of any Appearance (a character's look in the game)
    public static bool Apply(Look look);            // change the player to this look; false on failure
    public static bool Apply(Look look, Appearance appearance);   // the same for any Appearance, without Changed
    public static bool Live(Look look);             // change only the body and face dials, not the clothes
    public static bool Live(Look look, Appearance appearance);    // the same for any Appearance, without Changed
    public static Look Randomise();                 // roll a new random look, apply it, and return it
    public static Look Randomise(Appearance appearance);          // the same for any Appearance, without Changed
    public static Look Vanilla();                   // a random look made with the game's own rules; not applied
    public static IList<string> Options(string slot, bool male);   // the clothes names you can pick for a slot
    public static IList<string> Options(string slot, bool male, Appearance appearance);   // the same for any Appearance

    [Serializable]
    public sealed class Look    // one complete look
    {
        public bool set;             // true for a look taken from Capture
        public bool male;            // true for the male body
        public bool advanced;        // true when the advanced body dials are used
        public float height = 0.8f;  // body height, 0 to 1
        // Public fields, see the list below. Dials are floats, and the wearables are strings.
    }
}
```

The `Look` fields are all public.

- **The body and face dials are floats.** They are `height` (default 0.8), `shape`, `gender`, `headSize` (1), `faceSize` (1), `neckLength`, `shoulderWidth`, `breastSize`, `hipWidth`, `handSize` (1), `footSize` (1), `lipThickness`, `jawPosition`, `chinPosition`, `eyeSize` (1), `eyeDistance`, `eyePosition`, `eyeDepth`, `earSize` (1), `earRotation`, `noseRootRotation`, `noseTipRotation` and `noseWidth`. A dial defaults to 0 unless a different number is shown in brackets.
- **The clothes are strings.** They are `head`, `hat`, `hair`, `glasses`, `beard`, `shirtT`, `shirtB`, `pantsT`, `pantsB`, `socks` and `shoes`. Each holds the name of a wearable asset in the game files, and `""` means none.

**Reading a look.**

- **`Capture()` returns the player's look, or `null` when `IsAvailable` is false.** A look from `Capture` has `set` true. `Capture(appearance)` reads any `Appearance`.

**Changing a look.**

- **`Apply(look)` changes the player to the look you give.** Each dial is kept inside its range: height 0 to 1, shape -1 to 1, and every other dial 0 to 1. The game then rebuilds the body and clothes, and `Changed` fires. It returns `false` when not available, or when the game throws (the error is logged).
  - Technical: a new costume is built at runtime, so the game's shared Random NPC asset is never written. The rebuild is the game's `LoadCharacter`.
- **`Apply(look, appearance)` does the same for any `Appearance`, without firing `Changed`.**
- **`Live(look)` changes only the dials, not the clothes.** It sets the body and face dial values on the player and calls `UpdateShapes`. The same limits apply, and `Changed` fires.
- **The `Appearance` versions of `Live`, `Randomise` and `Options` work on any `Appearance`, and they do not fire `Changed`.** `Live` returns `false` and `Randomise` returns `null` when the `Appearance` is not ready, and `Options` returns an empty list then.
- **`Randomise()` rolls a new random look.** It captures the current look, rolls every dial except height, rolls each piece of clothing, applies the result, fires `Changed`, and returns the new look. It keeps the captured `male` and `advanced`. Leggings and a female beard always take the first entry, as the game's Random NPC rule does.
- **`Vanilla()` gives you a random look made with the game's own random-character rules, and does not apply it.** The body is male or female and basic or advanced by a coin toss, breast size is 0 to 0.3 for male and 0.3 to 1 for female, gender is rounded to 0 or 1, and height is included. Only `Vanilla` uses the game's gender ranges.
  - Technical: it uses `Appearance.LoadCharacterValues`.

**Clothes and options.**

- **`Options(slot, male)` lists the names you can use for a slot, in the game's order.** The slots are `Head`, `Hat`, `Hair`, `Glasses`, `Beard`, `Jacket`, `Shirt`, `Pants`, `Leggings`, `Socks` and `Shoes`. An unknown slot throws `ArgumentException`. When not available, it returns an empty list. A female beard gives the same full list as a male one, because the game's editor offers every beard to either gender.
- **Clothes are matched by name within the list for the look's gender.** A name that is not found clears the slot, except `head`, which falls back to the first head of that gender. Leggings are matched by name too.
- **Gender rules.** The head list follows `male`. A female beard is not forced to one entry in `Options` or `Apply`, and only `Randomise` and `Vanilla` roll it as its first entry. Breast size is not limited by gender in `Apply`, `Live` or `Randomise`: `Randomise` rolls it over 0 to 1 for either gender.

**Events and saving.**

- **`Changed` fires after `Apply(look)`, `Live(look)` and `Randomise()`, and it receives the look that was captured.** That look is `null` when the capture fails. A handler that throws is logged, and the other handlers still run. The `Appearance` versions do not fire it.
- **Nothing is saved.** Store a `Look` with `SaveData.Register` if you need it.

**Errors.**

- **A null `look` or `appearance` throws `ArgumentNullException`.** The exception is `Options`, which returns an empty list for a null `appearance`.
- **A game error is logged and the call returns `false` or `null`.** The log is `BepInEx\logs\obenseuer.moddingframework.playerlook.log`.

**Debug check.** `[Debug] PlayerLookSelfCheck = false` in `BepInEx\config\obenseuer.moddingframework.cfg`. When it is true, the framework runs a check when it starts and logs `playerlook self-check ok`, or `playerlook self-check failed: <check>`, to `BepInEx\logs\ObenseuerCore.log`. The check covers the dial limits and the clothes rules, on temporary `Wearable` assets. It does not touch an `Appearance`.

**Still to be checked in the game.**

- `Apply`, `Live` and `Randomise` on a running player: the body and clothes after `LoadCharacter`.
- `IsAvailable` on the main menu and during scene changes.

This plugin logs every look change, then takes the player's hat off.

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

The UI kit (`ModUi`, `UiTheme`, `UiSprites`) is on the [UI kit](../api/ui.md) page.
