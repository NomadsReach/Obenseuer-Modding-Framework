---
title: Apartments and tenants
parent: Housing
grand_parent: Places and travel
nav_order: 2
---

# Apartments and tenants

This is the full list of what the API gives you, one section for each part. Text in brackets at the end of a note is extra detail for experienced modders, and you can skip it on a first read.

`Housing` lists the apartments in the tenement and the tenants in them, and tells you when tenants move or pay rent. An apartment's number is the number the game shows (for example "Apartment: 2").

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Housing</code> <span>apartments, tenants and rent events</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class Housing
{
    public static List<ApartmentInfo> AllApartments();
    public static ApartmentInfo Get(int apartmentId);
    public static TenantInfo GetTenant(int apartmentId);

    public static event Action<TenantArgs> TenantMovedIn;
    public static event Action<TenantArgs> TenantMovedOut;
    public static event Action<RentPayment> RentPaid;
    public static event Action<TenementEventArgs> TenementEventStarted;
    public static event Action<TenantArgs> EvictionStarted;
    public static event Action<TenantArgs> EvictionCancelled;

    public const float DefaultEvictionHours = 18f;
    public static bool StartEviction(int apartmentId);
    public static bool StartEviction(int apartmentId, float hours);
    public static bool CancelEviction(int apartmentId);

    public static bool IsAvailable { get; }
}
```

| Method | Returns | What it does |
|---|---|---|
| `AllApartments()` | `List<ApartmentInfo>` | Every apartment in the building, empty or not. |
| `Get(int apartmentId)` | `ApartmentInfo` | The apartment with this number. `null` when there is none. |
| `GetTenant(int apartmentId)` | `TenantInfo` | Who lives there. `null` when the apartment is vacant. |
| `StartEviction(int apartmentId)` | `bool` | Starts an eviction with the default hours. `true` when it started. |
| `StartEviction(int apartmentId, float hours)` | `bool` | Starts an eviction with your hours. `true` when it started. |
| `CancelEviction(int apartmentId)` | `bool` | Clears a pending eviction. `true` when it was cleared. |

| Property | Type | What it means |
|---|---|---|
| `DefaultEvictionHours` | `float` | The hours the game's own eviction button uses. |
| `IsAvailable` | `bool` | `false` when a tenant or rent event could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `TenantMovedIn` | `TenantArgs` | A tenant has just finished moving in. |
| `TenantMovedOut` | `TenantArgs` | A tenant has just left. |
| `RentPaid` | `RentPayment` | A rent payment has just been credited. |
| `TenementEventStarted` | `TenementEventArgs` | A random tenement event has just started. |
| `EvictionStarted` | `TenantArgs` | An eviction timer has just been set. |
| `EvictionCancelled` | `TenantArgs` | A pending eviction has just been cleared. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ApartmentInfo</code> <span>a snapshot of one apartment</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class ApartmentInfo
{
    public int Id { get; }
    public int Floor { get; }
    public string Name { get; }
    public TenantInfo Tenant { get; }
    public bool MovingIn { get; }
    public RentPayment Rent { get; }
    public bool EvictionPending { get; }
    public float EvictionHoursLeft { get; }
    public float MoveInHoursLeft { get; }
    public ApartmentType Type { get; }
    public string ManagerGuid { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The apartment's number. |
| `Floor` | `int` | The floor it is on. |
| `Name` | `string` | The name the game shows, like "Apartment: 2". |
| `Tenant` | `TenantInfo` | Who lives there. `null` when vacant. |
| `MovingIn` | `bool` | `true` when a tenant is assigned and the move-in timer is still running. |
| `Rent` | `RentPayment` | The rent owed for each payment round. Zero when vacant or still moving in. |
| `EvictionPending` | `bool` | `true` when an eviction timer is running. |
| `EvictionHoursLeft` | `float` | The game hours left on the eviction timer. `0` when none. |
| `MoveInHoursLeft` | `float` | The game hours left on the move-in timer. `0` when nobody is moving in. |
| `Type` | `ApartmentType` | The apartment's size. |
| `ManagerGuid` | `string` | The id of the apartment's furniture area, which `PlayerShops.Ensure` takes. `null` when there is none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TenantInfo</code> <span>one tenant</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TenantInfo
{
    public int Id { get; }
    public string Name { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `int` | The tenant's character number (NPC id). |
| `Name` | `string` | The tenant's name. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>RentPayment</code> <span>an amount of rent</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class RentPayment
{
    public int Oc { get; }
    public float Rm { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Oc` | `int` | The OC amount. |
| `Rm` | `float` | The RM amount. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TenantArgs</code> <span>what the tenant and eviction events tell you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TenantArgs
{
    public int ApartmentId { get; }
    public TenantInfo Tenant { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `ApartmentId` | `int` | The apartment's number. |
| `Tenant` | `TenantInfo` | The tenant the event is about. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TenementEventArgs</code> <span>what TenementEventStarted tells you</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class TenementEventArgs
{
    public string Id { get; }
    public TenementEventKind Kind { get; }
    public int TenantId { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Id` | `string` | The event's id from the game's data. May be `null`. |
| `Kind` | `TenementEventKind` | What sort of event it is. |
| `TenantId` | `int` | The character number of the tenant the event is about. `0` when none. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>TenementEventKind</code> <span>the kinds of tenement event</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum TenementEventKind
{
    None,
    SpawnTrash,
    PlayMusic,
    MakeNoise,
    BreakApartment,
    Die,
    SpawnCrazyTeddies,
    Complain
}
```

| Value | Meaning |
|---|---|
| `None` | The game's None event type. |
| `SpawnTrash` | The game's SpawnTrash event type. |
| `PlayMusic` | The game's PlayMusic event type. |
| `MakeNoise` | The game's MakeNoise event type. |
| `BreakApartment` | The game's BreakApartment event type. |
| `Die` | The game's Die event type. |
| `SpawnCrazyTeddies` | The game's SpawnCrazyTeddies event type. |
| `Complain` | The game's Complain event type. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ApartmentType</code> <span>the apartment's size</span></p>
<div class="api-pair" markdown="1">

```csharp
public enum ApartmentType
{
    Small,
    Medium,
    Large,
    Shop,
    Unknown
}
```

| Value | Meaning |
|---|---|
| `Small` | A small apartment. |
| `Medium` | A medium apartment. |
| `Large` | A large apartment. |
| `Shop` | A shop space. |
| `Unknown` | The game gives this apartment no size. |

</div>
</div>

## Things to know

- The apartment number is the one the game shows, and it is unique in the building. `AllApartments()` gives every apartment, empty or not, in the game's own order. `Get` gives `null` for a number that does not exist. <span class="tech">(The number is the game's `apartmentNumber`.)</span>
- `GetTenant` gives `null` when the apartment is vacant, or when the tenant has moved out and left their furniture behind.
- `ApartmentInfo.ManagerGuid` is the id you pass to `PlayerShops.Ensure` (see [Player shops](../economy/player-shops.md#player-shops)). It is `null` when the game links no furniture area to the apartment, and also for a short time after a save loads, before the game links it. <span class="tech">(It is the GUID of the apartment's furniture manager.)</span>
- Rent is paid in rounds, not per tenant. The game does not keep a paid or unpaid mark for each room, so use `ApartmentInfo.Rent` for what is owed next, and `RentPaid` for when it is paid.
- `TenantMovedIn` fires when the move-in timer has finished and the tenant is in. `Tenant` is the new tenant.
- `TenantMovedOut` fires when a tenant leaves, through an eviction or when a new tenant replaces them. `Tenant` is the tenant who left.
- `RentPaid` fires when a rent round is credited. `Oc` and `Rm` are the totals for all apartments together, the same amounts the game adds to the wallet. A round is only announced when the game really pays.
- `TenementEventStarted` fires when a random tenement event happens, such as noise or trash. The scene sets what an event does: the game triggers the relay and optional spawner that the scene lists for that kind and tenant, and the event can also send mail and change dialogue variables.
- `TenementEventStarted` fires after the random chance for that event succeeded. It also fires for an event started with `Tenants.TriggerEvent` (see [Tenants](../tenants.md)).
- `EvictionStarted` fires when an eviction timer is set. That covers `StartEviction`, the game's own button, and anything else that sets one. `Tenant` is the tenant being evicted. The tenant does not leave until the timer ends, and then `TenantMovedOut` fires.
- `EvictionCancelled` fires when a pending eviction is cleared. That covers `CancelEviction` and the game's button. `Tenant` is the tenant who stays.
- `ApartmentInfo.Type` is the apartment's size from the game's data. A `Shop` is a shop space. `Unknown` means the game gives the apartment no size, so its size is not known.
- `Housing.IsAvailable` is `false` when a tenant or rent event could not be set up. See "If something fails to set up" below.
- Renovation cost and build-time tweaks are in `TweaksRenovations`. This page does not change them.
