---
title: Obenseuer Modding Framework medical API (Medical)
parent: API reference
nav_order: 46
---

# Obenseuer Modding Framework medical API (Medical)

Status: experimental.
Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.medical`. Logs go to `BepInEx\logs\<owner>.log`.

Everything not listed here is internal.

## Rules

- **Main thread only.** Every member is called from Unity's main thread.
- **Safe at any time.** Every member can be called on the main menu, while loading, and with no save loaded. Reads return a neutral default (`false`, or an empty list). Writes return `false`. `Treat` also returns `false` and charges nothing unless the doctor's menu is open. It also returns `false` while a save loads or saves (`SaveData.Busy`).
- **Only `ArgumentException`.** An unknown `MedicalTreatment` value throws `ArgumentException`. Nothing else is thrown to the caller.
- **Game purchase path.** `Treat` calls the game's own `BuyTreatment`, which charges the price through `Money.Buy`. The mod never subtracts money itself.

## Medical

```csharp
public enum MedicalTreatment
{
    PrescriptionShroomSubstitute,
    Fungicide, PrescriptionFungicide, GrowthsRemovalBasic, DangerousExperimentalTreatment,
    Antidepressants, PrescriptionAntidepressants, RubberDuck,
    PhysicalPainkillers, PhysicalPrescriptionPainkillers, AdhesiveBandages, Bandages, HealWounds,
    RadiationPainkillers, RadiationPrescriptionPainkillers, IodinePills
}

public sealed class MedicalTreatmentInfo
{
    public MedicalTreatment Kind { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
    public bool Available { get; }   // the doctor offers it right now
}

public sealed class MedicalTreatedArgs
{
    public MedicalTreatment Kind { get; }
    public int Price { get; }
    public MoneyCurrency Currency { get; }
}

public static class Medical
{
    public static bool IsAvailable { get; }                       // installed and the owner is not disabled
    public static List<MedicalTreatmentInfo> Treatments();        // every treatment of a loaded doctor
    public static bool CanTreat(MedicalTreatment kind);           // doctor menu open, offered now and the player can pay
    public static bool Treat(MedicalTreatment kind);              // doctor menu open: buys it through the game's own path
    public static event Action<MedicalTreatedArgs> Treated;
}
```

- The enum values are fixed. New treatments are appended.
- `Treatments` lists each treatment that a loaded `Doctor` object sells, whether or not it is offered right now. `Available` is the result of the game's `GetAvailableTreatments()` for its selection, which is the list the doctor's menu shows. The list is empty when no `Doctor` is loaded.
- `CanTreat` is `true` when the doctor's menu is open, the treatment belongs to the doctor whose menu is open, it is offered, and the player has at least `Price` in `Currency`. It is `false` on the main menu, in any other menu, and with no menu open.
- `Treat` checks `CanTreat` first, so nothing is charged and no "not enough money" popup is shown when the menu is closed, the treatment is not offered or the player cannot pay. It then calls the game's `DoctorPanelSelection.BuyTreatment(Treatment)`. The game charges the price, plays the sound, and gives the item or applies the effect, the same as the doctor's buy button does.
- `Treated` fires after the game's `BuyTreatment` returns `true`. It fires for a purchase made in the doctor's menu too, because the event comes from the game's `BuyTreatment` override, not from `Treat`.
- `Treated` is raised, so a throwing handler is logged and the others still run.

### Why `Treat` needs the doctor menu

`BuyTreatment` reads no menu state, but its `HealWounds` branch calls `BlackCanvas.StartFadeInAndOut` with `closeMenu: true` and can fire the examination bed's relays. That is written for the doctor's menu. Called with no menu open, it would run the fade and the relays during play, and on the main menu `BlackCanvas.instance` is null, which throws after the money is taken. So `Treat` and `CanTreat` require the doctor's menu to be open (`GameUIController.IsMenuVisible`, the Doctor panel shown, and a current `Doctor`), and use only that doctor's treatments. `Treatments` is a read and works without the menu.

### Side effects a mod should know about

- `HealWounds` asks `BlackCanvas.StartFadeInAndOut` to close the game menu (`closeMenu: true`). When the examination bed is loaded and the player is hurt, the fade runs through the bed's `teleportRelay` and `endRelay`. What those relays do was not traced. The same happens when the game's own button is used.
- `GetAvailableTreatments` on the physical health selection sets the `HealWounds` price. `Treatments` and `CanTreat` call it, as the game's menu does.
- The charge comes first. If the sound step or the item step throws after `Money.Buy`, the money is already taken, `Treat` returns `false`, and `Treated` does not fire.
- For the item treatments the game calls `Inventory.AddItemDialogue`, which gives nothing when the inventory is full. The money is already taken by then and `BuyTreatment` still returns `true`. The doctor's own buy button does the same. `Treat` does not check for free inventory space.

## Not wrapped, and where it lives

- Opening and closing the doctor's menu: `DoctorController.ShowMenu(Doctor)` opens the game menu through `GameUIController`. Calling it from a mod is not wrapped, because the menu is UI.
- Selection-level purchase (the info panel's price): `DoctorPanel.OnBuyButtonPressed` charges `currentSelection.price` and records the purchase in the menu state. It depends on the menu's current selection, so it is not wrapped.
- Condition and stat reads: `Conditions.All`, and `Survival.Get` for mood, radiation and fungal progression.
- Addictions, mood and radiation values: `Survival`.

## If a hook cannot be installed

The framework logs `medical events installed` or `medical events not installed` at startup.

The framework installs the `BuyTreatment` postfixes on the five treatment classes (`TreatmentAddictions`, `TreatmentFungalDisease`, `TreatmentMentalHealth`, `TreatmentPhysicalHealth`, `TreatmentRadiation`). A failed patch disables the owner's events, logs one error, and leaves `Treatments`, `CanTreat` and `Treat` working.

## Example

Run with the BepInEx console open, a save loaded, and the player near the doctor.

```csharp
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.medical", "Medical Smoke", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
public class MedicalSmoke : BaseUnityPlugin
{
    private void Start()
    {
        Medical.Treated += args => Logger.LogInfo("treated " + args.Kind + " for " + args.Price + " " + args.Currency);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;
        Logger.LogInfo("available " + Medical.IsAvailable);
        foreach (MedicalTreatmentInfo t in Medical.Treatments()) Logger.LogInfo(t.Kind + " price=" + t.Price + " " + t.Currency + " available=" + t.Available + " canTreat=" + Medical.CanTreat(t.Kind));
    }
}
```

On BepInEx 5, write `"0.11.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](../guides/getting-started/packaging.md#supporting-bepinex-5-too).

Expected: F9 prints one line for each treatment of the loaded doctor. To test a purchase, change the `Update` method to call `Medical.Treat(MedicalTreatment.Bandages)` on a key press, with the player holding enough cash. The `treated` line appears, and the price is taken from the wallet. Check the examination bed behaviour separately with `HealWounds`.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
