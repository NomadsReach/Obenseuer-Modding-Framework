---
title: Tenants
parent: People
grand_parent: API reference
nav_order: 14
---

# Tenants

Status: experimental.

## What it is

In the game, the player can own a tenement (an apartment block) and rent its flats out to NPCs. A tenant pays rent, can cause trouble, and the building can be upgraded. This API lets a mod move an NPC into a flat, set off a tenant event (such as a noisy night), open the game's rent screen for an NPC, read the rent the tenement earns, and read the upgrade and clean levels of the building.

Evicting a tenant and the reports about tenant events are on the [Housing](housing.md) page.

## When to use it

- Move an NPC into a flat when a quest step is done.
- Make a tenant do something, such as make noise, to test or to tell a story.
- Open the game's rent screen so the player can pick a flat for an NPC.
- Show how much rent the tenement earns, or how far a flat has been cleaned or upgraded.

## Quick example

This mod, once a save is loaded, logs the tenement's rent income and how many NPCs could be rented a flat right now. Nothing is changed.

```csharp
using ObenseuerCore;

[ModInfo("example.tenantwatch", "Tenant Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class TenantWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            RentPayment income = Tenants.TotalIncome();
            Log.Info(income == null ? "The tenement is not loaded" : "Rent income: " + income.Oc + " OC + " + income.Rm + " RM");
            int candidates = 0;
            foreach (NpcInfo npc in Npcs.All())
                if (Tenants.CanRent(npc.Id)) candidates++;
            Log.Info(candidates + " NPCs could rent a flat");
        });
    }
}
```

`TotalIncome` gives `null` when the tenement is not loaded. The income has two amounts, `Oc` (Open Sewer Coins) and `Rm` (Real Money), see [OC](../glossary.md#oc) and [RM](../glossary.md#rm). `WhenReady` runs your code once a save is loaded (see [Using events](../guides/getting-started/using-events.md)).

## Common tasks

Move an NPC into flat number 3 straight away, without the deposit, the notification or the fade. `Rent` returns `true` when the NPC is now the flat's tenant.

```csharp
int npcId = 0;
if (Tenants.CanRent(npcId) && Tenants.Rent(Mod.Id, npcId, 3, 0f, true))
    Log.Info("Moved in");
```

Make a tenant make noise. The event always fires when the call is accepted.

```csharp
bool ran = Tenants.TriggerEvent(Mod.Id, npcId, TenementEventKind.MakeNoise);
```

Open the game's rent screen for an NPC, so the player picks the flat.

```csharp
bool opened = Tenants.OpenRentScreen(Mod.Id, npcId);
```

Read how clean and how upgraded a flat is. Flat numbers are the same as on the Housing page.

```csharp
int clear = Tenants.FurnitureClearPercent(3);
int water = Tenants.ApartmentUpgrade(3, TenementController.ApartmentUpgradeCategory.Water);
int roof = Tenants.GeneralUpgrade(TenementController.GeneralUpgradeCategory.Roof);
```

## Reference

This is the full list of what the API gives you. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

The calls that change the game take your mod's id as `modId` (see [Mod id](../glossary.md#mod-id)). The other calls only read.

### Tenants

An NPC is given by its id from the [NPC API](npcs.md) (`NpcInfo.Id`). A flat is given by its number, the same number `Housing.Get` uses. Money amounts are a `RentPayment`, which has `Oc` and `Rm` and is described on the [Housing](housing.md) page.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Tenants</code> <span>move tenants, trigger their events and read the tenement</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Tenants
{
    public static bool CanRent(int npcId);
    public static bool Rent(string modId, int npcId, int apartmentId, float moveInHours, bool silent);
    public static bool TriggerEvent(string modId, int npcId, TenementEventKind kind);
    public static bool OpenRentScreen(string modId, int npcId);
    public static RentPayment TotalIncome();
    public static RentPayment RawIncome();
    public static int GeneralUpgrade(TenementController.GeneralUpgradeCategory category);
    public static int ApartmentUpgrade(int apartmentId, TenementController.ApartmentUpgradeCategory category);
    public static int FurnitureClearPercent(int apartmentId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `CanRent(int npcId)` | `bool` | `true` when the NPC is not a tenant yet and could be one. |
| `Rent(string modId, int npcId, int apartmentId, float moveInHours, bool silent)` | `bool` | Moves an NPC into a flat. `true` when it is now the tenant. |
| `TriggerEvent(string modId, int npcId, TenementEventKind kind)` | `bool` | Makes a tenant event happen. `true` when an event ran. |
| `OpenRentScreen(string modId, int npcId)` | `bool` | Opens the rent screen for an NPC. `true` when it opened. |
| `TotalIncome()` | `RentPayment` | The rent the tenement earns. `null` when the tenement is not loaded. |
| `RawIncome()` | `RentPayment` | The same rent, before the game divides it into payments. `null` when the tenement is not loaded. |
| `GeneralUpgrade(TenementController.GeneralUpgradeCategory category)` | `int` | The upgrade level of the building. `-1` when the tenement is not loaded. |
| `ApartmentUpgrade(int apartmentId, TenementController.ApartmentUpgradeCategory category)` | `int` | The upgrade level of one flat. `-1` when it is unknown. |
| `FurnitureClearPercent(int apartmentId)` | `int` | How much of a flat has been cleared of furniture. `-1` when it is unknown. |

</div>
</div>

#### Things to know

- `CanRent(npcId)` is `true` when the NPC is not a tenant yet and has a tenant record. An NPC who is listed as a relative of a tenant counts as already rented, as it does in the game.
- `Rent` moves the NPC into the flat the game's own way. It follows the game's rules, so it refuses (returns `false`) when:
  - the flat is the player's own flat (number 1);
  - the flat already has a resident, even one that has furniture only;
  - the NPC is already a tenant;
  - the tenement is not loaded, or its rent screen is open;
  - the flat does not meet what the NPC needs, such as size, electricity, water and heating. <span class="tech">(It calls `TenementController.RentApartment`. The requirements check is `TenementResident.CheckRequirements`.)</span>
- `moveInHours` is how many hours pass before the tenant moves in, from 0 to 8760 (a year). A value outside that range makes `Rent` return `false` and writes a line to the log once.
- `silent` decides whether the game takes a deposit and shows the move. With `silent` set to `true`, the game takes no deposit and pays none, shows no notification, and does not fade the screen. The NPC that is already in the scene stays visible. With `silent` set to `false`, the game takes the deposit and plays its normal fade. The framework never charges or pays anything itself. <span class="tech">(With `silent` false, the deposit goes through `Money.AddMoney` and the fade is `RentFade`. A rented resident is only hidden by the fade path, so a silent rent leaves the NPC visible, the same as the game's own silent rent.)</span>
- `TriggerEvent` makes the first matching tenant event happen, and the game's random roll is skipped. It picks the first event of that kind on the tenant's record that the game says can happen now. The event sends its mail, sets its dialogue values and runs the scene reaction for that tenant (a relay or a spawner), if that scene is loaded. `Housing.TenementEventStarted` fires for it just like for a random event. <span class="tech">(It calls `TenementEvent.OnTrigger` for the first event of that type whose `CanTrigger()` holds.)</span>
- `TriggerEvent` returns `false` when no event ran. That happens when the NPC is not a tenant, the tenant has not moved in yet, the flat has furniture only, the tenant has no event of that kind, or the game's tenement event controller is not loaded. An event kind that is not in `TenementEventKind` also gives `false`.
- `OpenRentScreen` opens the game's rent screen for the NPC, and the player picks the flat. The framework does not rent anything itself. <span class="tech">(It calls `TenementResident.StartRent`, which marks the NPC as the open resident and opens the game menu on the tenement page.)</span>
- `OpenRentScreen` returns `false` when the screen can't be opened right now. That happens when the NPC is already a tenant or a relative of one, the tenement is not loaded, a rent or contractor screen is already open, a menu is open or the game is paused, input is blocked, the inventory is turned off (the game needs its keys to open the tenement page), the game has switched its menu off (while lockpicking, for example), the player is not holding the task item the game requires for renting, building mode is open, or a dialogue is on.
- `TotalIncome()` and `RawIncome()` give the rent the tenement earns. `RawIncome` is the full amount before the game divides it into payments. Both give `null` when the tenement is not loaded.
- `GeneralUpgrade`, `ApartmentUpgrade` and `FurnitureClearPercent` give `-1` when the tenement or the flat is not loaded. `ApartmentUpgrade` and `FurnitureClearPercent` take the flat number, as `Housing.Get` does.
- The calls that change the game return `false` while a save is loading or being written. These are `Rent`, `TriggerEvent` and `OpenRentScreen`.

### What you can't do (yet)

- You can't close the rent screen from your mod. The player closes it with the menu key, and the framework has no call for it.

## Details

Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono ([be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Logs go to `BepInEx\logs\obenseuer.moddingframework.tenants.log`.

Everything not listed in this reference is internal and may change without notice. Items marked experimental may change too.

### When you can call it

- Call everything from your mod's normal code, not from a background thread you started. The game and Unity only work safely on one thread, the main thread (see the [glossary](../glossary.md#main-thread)).
- It is safe to call any of it at any time, even on the main menu or while a save is loading. If the tenement or the flat is not loaded, the reads give `-1` or `null`, and the calls that change the game give `false`.

### What to check for and what comes back

- Nothing here throws. A `modId` that is not valid (it must be 1 to 64 letters, digits, `.`, `_` or `-`) makes a change call return `false`, and one line goes to the log.
- If the game itself fails while the framework works, you get that plain default instead of an error. The first failure of each member is written to the log as an error.

### If something fails to set up

This API does not change any of the game's methods, so there is no feature group to turn off. It has nothing to fail at start.
