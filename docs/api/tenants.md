---
title: Obenseuer Modding Framework tenant API (Tenants)
parent: API reference
nav_order: 74.1
---

# Obenseuer Modding Framework tenant API (Tenants)

Experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.tenants`. Logs go to `BepInEx\logs\obenseuer.moddingframework.tenants.log`.
Files: `src/Api/Tenants.cs`.

Everything not listed here is internal. Main thread only.

Scope: moving an NPC into an apartment, starting a tenant event, opening the rent screen, the tenement income, and the upgrade and clean levels of the tenement. Eviction and the tenant event reports are in `Housing`.

## Rules

- **Rent.** `Rent` calls the game's own `TenementController.RentApartment`. It refuses the player's own apartment (number 1), an apartment that has a resident, including one with furniture only, an NPC who is already a tenant, a tenement that is not loaded, and an open rent screen. It also refuses when the game's own requirements check (`TenementResident.CheckRequirements`) fails: size, electricity, water, heating and the rest.
- **Silent.** With `silent` true, the game does not take or pay the collateral, shows no notification, and does not fade. The NPC already in the scene stays visible, as with the game's own silent rent, because only the fade path hides a rented resident. With `silent` false, the game's own path pays the deposit through `Money.AddMoney` and plays the `RentFade` fade. The framework itself does not charge or credit anything.
- **Move in.** `moveInHours` is the time until the tenant moves in, from 0 to 8760 hours.
- **Tenant event.** `TriggerEvent` runs the game's own `TenementEvent.OnTrigger` for the first event of that kind on the tenant's record whose own condition (`CanTrigger`) holds. The game's random roll is skipped, so the event always fires. The event sends its mail, sets its dialogue variables and runs the scene event for that tenant (a relay or a spawner), if that scene is loaded. It refuses an NPC who is not a tenant, a tenant who has not moved in yet, a furniture-only apartment, a tenant record with no event of that kind, and a tenement event controller that is not loaded. `Housing.TenementEventStarted` fires for it like for a random event.
- **Rent screen.** `OpenRentScreen` calls the game's own `TenementResident.StartRent`, which marks the NPC as the open resident and opens the game menu on the tenement page. It refuses an NPC who is already a tenant or a relative of one, a tenement that is not loaded, a rent or contractor screen that is already open, an open menu, a pause, blocked input, a disabled inventory (the keys the game itself needs to open the tenement page), a game menu the game has switched off (lockpicking, for example), the player not holding the task item the game requires for renting, building mode is open, and an active dialogue. The player then chooses the apartment. The framework does not rent anything itself.
- **Owner.** `Rent`, `TriggerEvent` and `OpenRentScreen` take the mod id. The other calls only read.
- **Failure.** Reads return `-1` or `null` when the tenement or the apartment is not loaded. `Rent`, `TriggerEvent` and `OpenRentScreen` return `false` during a save or a load.

## API

```csharp
public static class Tenants
{
    public static bool CanRent(int npcId);                                              // not a tenant yet, and has a tenant record
    public static bool Rent(string modId, int npcId, int apartmentId, float moveInHours, bool silent);
    public static bool TriggerEvent(string modId, int npcId, TenementEventKind kind);   // true when an event ran
    public static bool OpenRentScreen(string modId, int npcId);                         // true when the menu opened on the rent screen
    public static RentPayment TotalIncome();                                            // null when not loaded
    public static RentPayment RawIncome();                                              // null when not loaded
    public static int GeneralUpgrade(TenementController.GeneralUpgradeCategory category);
    public static int ApartmentUpgrade(int apartmentId, TenementController.ApartmentUpgradeCategory category);
    public static int FurnitureClearPercent(int apartmentId);
}
```

- `TotalIncome` and `RawIncome` return a `RentPayment` with `Oc` and `Rm`, the same type `Housing` uses for rent. `RawIncome` is the full amount before the game divides it into payments.
- `ApartmentUpgrade` and `FurnitureClearPercent` take the apartment number, as `Housing.Get` does.

## Example

```csharp
if (Tenants.CanRent(npcId) && Tenants.Rent("MyMod", npcId, 3, 0f, true))
{
    RentPayment income = Tenants.TotalIncome();
}

Tenants.TriggerEvent("MyMod", npcId, TenementEventKind.MakeNoise);
Tenants.OpenRentScreen("MyMod", otherNpcId);
```

## Not wrapped

- **Closing the rent screen.** The player closes it with the menu key. The framework has no call for it.
- **Relatives.** `CanRent` and `Rent` treat an NPC who is listed as a relative of a tenant as already rented, as the game does.
