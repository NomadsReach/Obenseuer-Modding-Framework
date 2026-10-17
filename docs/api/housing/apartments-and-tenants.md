---
title: Apartments and tenants
parent: Housing
grand_parent: Places and travel
nav_order: 2
---

# Apartments and tenants

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

`Housing` lists the apartments in the tenement and the tenants in them, and tells you when tenants move or pay rent. An apartment's number is the number the game shows (for example "Apartment: 2").

```csharp
public static class Housing
{
    public static List<ApartmentInfo> AllApartments();       // every apartment in the building, empty or not
    public static ApartmentInfo Get(int apartmentId);        // the apartment with this number; null when there is none
    public static TenantInfo GetTenant(int apartmentId);     // who lives there; null when the apartment is vacant

    public static event Action<TenantArgs> TenantMovedIn;            // a tenant has just finished moving in
    public static event Action<TenantArgs> TenantMovedOut;           // a tenant has just left
    public static event Action<RentPayment> RentPaid;                // a rent payment has just been credited
    public static event Action<TenementEventArgs> TenementEventStarted;   // a random tenement event has just started
    public static event Action<TenantArgs> EvictionStarted;          // an eviction timer has just been set
    public static event Action<TenantArgs> EvictionCancelled;        // a pending eviction has just been cleared

    public const float DefaultEvictionHours = 18f;           // the hours the game's own eviction button uses
    public static bool StartEviction(int apartmentId);                  // starts an eviction with the default hours; true when it started
    public static bool StartEviction(int apartmentId, float hours);     // starts an eviction with your hours; true when it started
    public static bool CancelEviction(int apartmentId);                 // clears a pending eviction; true when it was cleared

    public static bool IsAvailable { get; }                  // false when a tenant or rent event could not be set up
}

public sealed class ApartmentInfo    // a snapshot of one apartment
{
    public int Id { get; }                 // the apartment's number
    public int Floor { get; }              // the floor it is on
    public string Name { get; }            // the name the game shows, like "Apartment: 2"
    public TenantInfo Tenant { get; }      // who lives there; null when vacant
    public bool MovingIn { get; }          // true when a tenant is assigned and the move-in timer is still running
    public RentPayment Rent { get; }       // the rent owed for each payment round; zero when vacant or still moving in
    public bool EvictionPending { get; }   // true when an eviction timer is running
    public float EvictionHoursLeft { get; }    // game hours left on the eviction timer; 0 when none
    public float MoveInHoursLeft { get; }      // game hours left on the move-in timer; 0 when nobody is moving in
    public ApartmentType Type { get; }         // the apartment's size
    public string ManagerGuid { get; }         // the id of the apartment's furniture area, which PlayerShops.Ensure takes; null when there is none
}

public sealed class TenantInfo    // one tenant
{
    public int Id { get; }                 // the tenant's character number (NPC id)
    public string Name { get; }            // the tenant's name
}

public sealed class RentPayment    // an amount of rent
{
    public int Oc { get; }                 // the OC amount
    public float Rm { get; }               // the RM amount
}

public sealed class TenantArgs    // what the tenant and eviction events tell you
{
    public int ApartmentId { get; }        // the apartment's number
    public TenantInfo Tenant { get; }      // the tenant the event is about
}

public sealed class TenementEventArgs    // what TenementEventStarted tells you
{
    public string Id { get; }              // the event's id from the game's data; may be null
    public TenementEventKind Kind { get; } // what sort of event it is
    public int TenantId { get; }           // the character number of the tenant the event is about; 0 when none
}

public enum TenementEventKind
{
    None,                // the game's None event type
    SpawnTrash,          // the game's SpawnTrash event type
    PlayMusic,           // the game's PlayMusic event type
    MakeNoise,           // the game's MakeNoise event type
    BreakApartment,      // the game's BreakApartment event type
    Die,                 // the game's Die event type
    SpawnCrazyTeddies,   // the game's SpawnCrazyTeddies event type
    Complain             // the game's Complain event type
}

public enum ApartmentType
{
    Small,     // a small apartment
    Medium,    // a medium apartment
    Large,     // a large apartment
    Shop,      // a shop space
    Unknown    // the game gives this apartment no size
}
```

- **The apartment number is the one the game shows. It is unique in the building.** `AllApartments()` gives every apartment, empty or not, in the game's own order. `Get` gives `null` for a number that does not exist.
  - Technical: the number is the game's `apartmentNumber`.
- **`GetTenant` gives `null` when the apartment is vacant, or when the tenant has moved out and left their furniture behind.**
- **`ApartmentInfo.ManagerGuid` is the id of the apartment's furniture area.** It is the id you pass to `PlayerShops.Ensure` (see [Player shops](../economy/player-shops.md#player-shops)). It is `null` when the game links no furniture area to the apartment, and also for a short time after a save loads, before the game links it.
  - Technical: it is the GUID of the apartment's furniture manager.
- **Rent is paid in rounds, not per tenant.** The game does not keep a paid or unpaid mark for each room, so use `ApartmentInfo.Rent` for what is owed next, and `RentPaid` for when it is paid.
- **`TenantMovedIn` fires when the move-in timer has finished and the tenant is in.** `Tenant` is the new tenant.
- **`TenantMovedOut` fires when a tenant leaves.** That happens through an eviction or when a new tenant replaces them. `Tenant` is the tenant who left.
- **`RentPaid` fires when a rent round is credited.** `Oc` and `Rm` are the totals for all apartments together, the same amounts the game adds to the wallet. A round is only announced when the game really pays.
- **`TenementEventStarted` fires when a random tenement event happens, such as noise or trash.** What an event does is set by the scene: the game triggers the relay and optional spawner that the scene lists for that kind and tenant, and the event can also send mail and change dialogue variables. `TenementEventStarted` fires after the random chance for that event succeeded, and also for an event started with `Tenants.TriggerEvent` (see [Tenants](../tenants.md)).
- **`EvictionStarted` fires when an eviction timer is set.** That is the case for `StartEviction`, for the game's own button, and for anything else that sets one. `Tenant` is the tenant who is being evicted. The tenant does not leave until the timer ends, and then `TenantMovedOut` fires.
- **`EvictionCancelled` fires when a pending eviction is cleared.** That is the case for `CancelEviction` and for the game's button. `Tenant` is the tenant who stays.
- **`ApartmentInfo.Type` is the apartment's size from the game's data.** A `Shop` is a shop space. `Unknown` means the game gives the apartment no size, so its size is not known.
- **`Housing.IsAvailable` is `false` when a tenant or rent event could not be set up.** See "If something fails to set up" below.

Renovation cost and build-time tweaks are in `TweaksRenovations`. This page does not change them.
