---
title: Treatments and prices
parent: Medical
grand_parent: Player life and skills
nav_order: 2
---

# Treatments and prices

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

```csharp
public enum MedicalTreatment    // the treatments the doctor sells
{
    PrescriptionShroomSubstitute,      // gives the item "Mushroom Substitute Prescription" (id 15810)
    Fungicide,                         // gives the item "Fungicide" (id 15020)
    PrescriptionFungicide,             // gives the item "Fungicide Prescription" (id 15040)
    GrowthsRemovalBasic,               // hurts the player and lowers the SMV progression and its rate
    DangerousExperimentalTreatment,    // hurts the player more and lowers the SMV progression and its rate by more
    Antidepressants,                   // gives the item "Antidepressants" (id 140020)
    PrescriptionAntidepressants,       // gives the item "Antidepressants Prescription" (id 12852)
    RubberDuck,                        // gives the item "Rubber Duck" (id 11)
    PhysicalPainkillers,               // gives the item "Painkillers" (id 140010)
    PhysicalPrescriptionPainkillers,   // gives the item "Painkillers Prescription" (id 12851)
    AdhesiveBandages,                  // gives the item "Adhesive Bandages" (id 12670)
    Bandages,                          // gives the item "Bandage" (id 10940)
    HealWounds,                        // sets the player's health to 100 and bleeding to 0
    RadiationPainkillers,              // gives the item "Painkillers" (id 140010)
    RadiationPrescriptionPainkillers,  // gives the item "Painkillers Prescription" (id 12851)
    IodinePills                        // gives the item "Iodine Pills" (id 15360)
}

public sealed class MedicalTreatmentInfo    // one treatment and its price
{
    public MedicalTreatment Kind { get; }       // which treatment it is
    public int Price { get; }                   // what it costs, in Currency
    public MoneyCurrency Currency { get; }      // OC (Open Sewer Coins) or RM (Real Money)
    public bool Available { get; }              // true when the doctor offers it right now
}

public sealed class MedicalTreatedArgs    // what Treated tells you
{
    public MedicalTreatment Kind { get; }       // which treatment was bought
    public int Price { get; }                   // what it cost
    public MoneyCurrency Currency { get; }      // the currency it was paid in
}

public static class Medical
{
    public static bool IsAvailable { get; }                       // false when the Treated event could not be set up
    public static List<MedicalTreatmentInfo> Treatments();        // the treatments the doctors in the scene sell
    public static bool CanTreat(MedicalTreatment kind);           // true when the doctor's screen is open, it is offered, and the player can pay
    public static bool Treat(MedicalTreatment kind);              // buys the treatment for the player; true when it was bought
    public static event Action<MedicalTreatedArgs> Treated;       // a treatment was bought
}
```

OC is Open Sewer Coins and RM is Real Money (see the [glossary](../../glossary.md#oc)).

- **The list of treatments is fixed, and new ones are only ever added at the end.** The numbers behind the names will not change between versions.
- **`Treatments()` lists every treatment that a doctor in the scene sells, whether or not the doctor offers it right now.** `Available` tells you which ones the doctor's screen shows at the moment. The list is empty when no doctor is loaded. If the scene has more than one doctor, you get each treatment once, from the first doctor that sells it.
  - Technical: `Available` is the result of the game's `GetAvailableTreatments()` for that treatment's selection (the object holding one group of treatments), which is the list the doctor's menu shows. The framework finds the doctors with `SceneIds.Active<Doctor>()`.
- **`CanTreat` is `true` only when the doctor's screen is open and the player can buy that treatment.** It needs three things: the treatment belongs to the doctor whose screen is open, the doctor offers it, and the player has at least `Price` in `Currency`. It is `false` on the main menu, in any other menu, and with no menu open.
  - Technical: it requires `GameUIController.IsMenuVisible`, the Doctor panel as the current panel, a `DoctorController.currentDoctor`, an active `DoctorPanel`, and the `BlackCanvas`, `Inventory` and `PlayerStats` instances.
- **`Treat` buys the treatment the same way the doctor's buy button does.** The game charges the price, plays the sound, and gives the item or applies the effect. Your mod never takes the money itself. It returns `true` when the treatment was bought.
  - Technical: it calls `DoctorPanelSelection.BuyTreatment(Treatment)`, which charges through `Money.Buy`.
- **`Treat` checks `CanTreat` first.** If the doctor's screen is closed, the treatment is not offered, or the player can't pay, it returns `false`, takes no money and shows no "not enough money" popup. It also returns `false` while a save is loading or being written.
- **`Treat` needs the doctor's screen to be open because the game was written for it.** Healing wounds fades the screen and closes the menu, and it can set off the examination bed. With no menu open that would run in the middle of play, and on the main menu it would fail after the money was taken. Opening and closing the doctor's screen is not part of this API, because it is user interface.
  - Technical: the `HealWounds` branch of `BuyTreatment` calls `BlackCanvas.StartFadeInAndOut` with `closeMenu: true` and can fire the examination bed's relays. `BlackCanvas.instance` is null on the main menu. `DoctorController.ShowMenu(Doctor)` opens the menu through `GameUIController`.
- **`Treated` fires every time a treatment is bought, including when the player buys one with the doctor's own button.** It fires after the purchase went through. It does not fire if the purchase failed.
  - Technical: it is raised after the game's `BuyTreatment` returns `true`, from a postfix on the `BuyTreatment` of the five treatment classes, not from `Treat`.
- **Heal wounds closes the menu and may set off the examination bed.** When the bed is loaded and the player is hurt, the screen fades while the bed does its own actions. This is the same as when the player uses the game's button. What those actions do has not been checked.
  - Technical: the relays are the bed's `teleportRelay` and `endRelay`.
- **Reading the list can update the price of healing wounds.** The game works out that price when it builds its list of offered treatments, and `Treatments()` and `CanTreat` build that list too, as the doctor's screen does.
  - Technical: `GetAvailableTreatments` on the physical health selection sets the `HealWounds` price.
- **The money is taken first, so a failure afterwards can cost the player money.** If the sound or the item step fails after the payment, the money is gone, `Treat` returns `false` and `Treated` does not fire.
- **A treatment that gives an item is still paid for when the inventory is full.** The game then gives nothing, but still counts the purchase as done, and so does the doctor's own button. `Treat` does not check for free space in the inventory.
  - Technical: the game calls `Inventory.AddItemDialogue`, which gives nothing when the inventory is full, and `BuyTreatment` still returns `true`.
- **`IsAvailable` tells you whether `Treated` works.** It is `false` when the framework could not set up the event. `Treatments`, `CanTreat` and `Treat` still work.
- **Conditions and survival values are on other pages.** The conditions the player has are on the [Conditions](../conditions.md) page. Addictions, mood and radiation values are read with `Survival`.
  - Technical: `Conditions.All`, and `Survival.Get` for mood, radiation and fungal progression.
- **Buying by the doctor's info panel is not part of this API.** That button charges the price of the current selection and records the purchase in the menu state, so it depends on what the menu has selected.
  - Technical: `DoctorPanel.OnBuyButtonPressed` charges `currentSelection.price`.
