---
title: Common tasks
parent: Housing
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

This page uses `Vector3`, `Quaternion` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

These snippets run from your mod's normal code, after a save is loaded.

List the apartments, their floors and who lives in them.

```csharp
foreach (ApartmentInfo apartment in Housing.AllApartments())
    Log.Info(apartment.Name + " on floor " + apartment.Floor + ": " + (apartment.Tenant == null ? "vacant" : apartment.Tenant.Name));

TenantInfo tenant = Housing.GetTenant(2);
if (tenant != null) Log.Info("Apartment 2 is home to " + tenant.Name);
```

Start an eviction with the game's normal 18 hours, with your own number of hours, or call it off.

```csharp
if (Housing.StartEviction(2)) Log.Info("The eviction timer is running.");
if (Housing.StartEviction(3, 48f)) Log.Info("A two day eviction started.");
if (Housing.CancelEviction(2)) Log.Info("The tenant can stay.");

Housing.EvictionStarted += args => Log.Info("Eviction started for " + args.Tenant.Name);
```

List the furniture the player placed in each building area.

```csharp
foreach (string areaId in Building.AllAreaIds())
    Log.Info(areaId + " holds " + Building.GetPlacedFurniture(areaId).Count + " pieces you can move");
```

Place a piece of furniture in an area. Take the area id from `Building.AllAreaIds()` and the furniture id from `Building.Catalog()`. You get the new piece, or `null` when the game would not allow it.

```csharp
string areaGuid = "some id from Building.AllAreaIds()";
string furnitureId = "some id from Building.Catalog()";
FurnitureItem placed = Building.Place(areaGuid, furnitureId, Vector3.zero, Quaternion.identity, Quaternion.identity, 0, 0);
if (placed != null) Log.Info("Placed " + placed.Title);
```

React when furniture is placed or taken out.

```csharp
Building.FurniturePlaced += item => Log.Info(item.Title + " was placed in " + item.AreaId);
Building.FurnitureRemoved += item => Log.Info(item.Title + " was taken out of " + item.AreaId);
```

Move a piece, or take it out of the world. This needs `using UnityEngine;` for `Vector3` and `Quaternion`.

```csharp
string pieceGuid = "some Guid from a FurnitureItem";
bool moved = Building.Move(pieceGuid, new Vector3(1f, 0f, 2f), Quaternion.identity);
bool removed = Building.Remove(pieceGuid);
Log.Info("Moved: " + moved + ", removed: " + removed);
```

Find out which building area the player is in, and react when that changes.

```csharp
BuildAreaInfo area = Building.CurrentArea;
if (area != null) Log.Info("You are in area " + area.ManagerGuid + (area.RemoveOnly ? " (remove only)" : ""));
Building.AreaEntered += entered => Log.Info("Entered " + entered.ManagerGuid);
Building.AreaExited += left => Log.Info("Left " + left.ManagerGuid);
```

List all the furniture the game knows, and see which furniture blueprints the player has.

```csharp
foreach (FurnitureCatalogEntry entry in Building.Catalog())
    Log.Info(entry.Id + ": " + entry.Title + " (" + entry.Category + ")");

foreach (BlueprintInfo blueprint in Building.AllBlueprints())
    Log.Info(blueprint.Title + (blueprint.Known ? " is known" : " is not known yet"));

if (!Building.IsBlueprintKnown("some blueprint name")) Building.UnlockBlueprint("some blueprint name", true);
```

Check whether the events work.

```csharp
if (!Housing.IsAvailable) Log.Warn("Housing events are off.");
if (!Building.IsAvailable) Log.Warn("Building events are off.");
```
