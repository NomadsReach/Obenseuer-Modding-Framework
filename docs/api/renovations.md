# Renovations API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.renovations`.
Files: `src/Api/Renovations.cs`, `src/Api/RenovationsPatches.cs`.

Everything not listed here is internal. Renovation cost and build-time tweaks stay in `TweaksRenovations`. Housing's tenement rules are in `docs/api/housing.md`.

## Wiring

```csharp
internal static void Renovations.Init();
```

Core calls this from `Core.Awake`.

`Init` installs one hook, `TenementUpgradeProgress.FinishUpgrade`. If it is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.renovations.log`. The reads and `Start` keep working; only `Finished` stops.

## Threading and safety

- Main thread only.
- `Renovations.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, `All` returns an empty list and the other reads return `0`.
- `Start` throws `ArgumentException` for a null or empty id. Every other failure returns `false`.
- `Finished` is raised synchronously after the game's own `FinishUpgrade` has run. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## Ids

A renovation id is `"<apartmentNumber>/<upgrade id>"`. `0` means a building-wide upgrade (the game's general upgrades, `TenementGeneral`). Apartment upgrades use the apartment number (`TenementApartment.apartmentNumber`). Read ids from `All`; the upgrade id is `TenementUpgrade.Id`.

## API

```csharp
public static class Renovations
{
    public static List<RenovationInfo> All();
    public static RenovationInfo Get(string id);
    public static float Progress(string id);
    public static int Cost(string id);
    public static bool Start(string id);

    public static bool IsAvailable { get; }
    public static event Action<RenovationInfo> Finished;
}

public enum RenovationState
{
    Locked, Available, InProgress, AwaitingFinalPayment, Installed
}

public sealed class RenovationInfo
{
    public string Id { get; }              // "<apartmentNumber>/<upgrade id>"
    public string Name { get; }            // TenementUpgrade.GetName, the game's label
    public int ApartmentId { get; }        // apartment number, 0 for building-wide
    public int Cost { get; }               // OC first payment (TenementUpgrade.GetPriceStartOC)
    public int FinalCost { get; }          // RM final payment (TenementUpgrade.GetPriceFinalRM)
    public RenovationState State { get; }
}
```

- `All` lists every renovation the game defines for the building and for each apartment: the general upgrade groups and each apartment type's groups (`TenementUpgradeGroup.updateGroup`). Duplicates in one scope are dropped. A renovation is listed whether or not it is available. An upgrade that throws while it is described is skipped and logged once; the rest of the list is still returned.
- `Get(id)` returns one entry of `All`, or `null` when the id is not found.
- `State`:
  - `Installed`: `TenementUpgrade.IsUpgraded` is true.
  - `InProgress`: an order is being built.
  - `AwaitingFinalPayment`: the building work is done (`upgradeReady`). The player pays the RM final payment in the game's upgrade panel. This API does not pay it.
  - `Available`: the upgrade's required upgrades are installed (`TenementUpgrade.CanUpgrade` without items) and no other upgrade in the same line (the same `TenementUpgradeGroup`, for example electricity of one apartment) is `InProgress` or `AwaitingFinalPayment`. The game's upgrade panel locks a line the same way.
  - `Locked`: anything else. This includes an upgrade whose line has another upgrade ordered or awaiting its final payment.
- `Progress` is `0` when not ordered, the game's build progress (`TenementUpgradeProgress.GetCurrentProgress`) while `InProgress`, and `1` when `Installed` or `AwaitingFinalPayment`.
- `Cost` is the OC first payment after any `TweaksRenovations.CostMultiplier`, because the game's own `GetPriceStartOC` is what the tweak patches.
- `Start` orders a renovation through the game's own order path, `TenementApartment.StartUpdate` or `TenementGeneral.StartUpdate`. The game charges the OC first payment and the required items inside that call (`TenementUpgrade.TakeRequiredItemsAndMoney`). It returns `false` without calling the game unless all of these hold:
  - the contractor menu is open (`TenementController.contractorOpen`) and a contractor is set;
  - the renovation is `Available`;
  - the contractor offers the upgrade for that apartment size (`TenementContractor.CanUpgrade`);
  - unless `TenementController.ignoreUpgradePricesAndItems` is set: the player can pay the OC first payment (`Money.CheckMoney`) and holds the required items (`TenementUpgrade.CanUpgrade` with items).
- After a successful order `Start` does what the upgrade panel does around the order: it removes the started upgrade and the lower upgrades of its line from the marked list (`TenementController.RemoveSupersededMarkedUpgrades`), closes the upgrade panel if it is open, and refreshes the floor buttons (`BlueprintManager.UpdateAll`), so a panel opened before the call cannot order the same upgrade a second time. `Start` does not do the developer "complete instantly" path.
- `Start` returns `true` only when the game returned the new order. The OC first payment and the items are taken by the game inside that call; a failed `Start` that returned `false` before the game call has charged nothing.
- `Finished` fires after `TenementUpgradeProgress.FinishUpgrade`, which applies the upgrade to the building. It runs for the player's final payment. It does not run for silent orders (`TenementUpgradeProgress.FinishUpgrade(true)`, used by scene set-up through `TenementUpgradeStart`), which have no payment, task or achievement. The payload is the renovation as `Installed`.

## Not wrapped

- The final RM payment and the act of finishing an upgrade by hand. That is the player's action in the upgrade panel (`UpgradeUI.OnPressed`, `RE/World/UpgradeUI.cs:457`). No public call is exposed.
- Requested build-time overrides and silent orders (`TenementUpgradeStart.Upgrade`, `RE/World/TenementUpgradeStart.cs:34`). Only the game's default order is made.
- Marking an upgrade with the star (`TenementController.MarkUpgrade`, `RE/World/TenementController.cs:1070`).
- Apartment condition and cleaning: see `docs/api/cleaning.md`.
