---
title: Obenseuer Modding Framework NPC API (npcs)
parent: API reference
nav_order: 51
---

# Obenseuer Modding Framework NPC API (npcs)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owners: `obenseuer.moddingframework.npcs`, `obenseuer.moddingframework.npcs.died` and `obenseuer.moddingframework.npcs.relationship`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

Everything not listed here is internal. Items marked **experimental** may change.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- `Npcs.IsAvailable` is false when the spawn, despawn or died hooks were not installed, or the owner `obenseuer.moddingframework.npcs` or `obenseuer.moddingframework.npcs.died` was disabled. `Relationships.IsAvailable` is false when the relationship hook was not installed or the owner `obenseuer.moddingframework.npcs.relationship` was disabled. It does not follow `Npcs.IsAvailable`.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`0`, `0f`, `false`, `null`, or an empty list). The only writer in the area is `Animals.Damage`.
- **Bad arguments.** `Npcs.Find` throws `ArgumentException` for a null or empty string. `Relationships.GetDisposition` and `Relationships.Has` return `0f` and `false` for a null or empty id, and log it once. `Animals.Damage` throws `ArgumentException` for an amount that is not finite or is `0` or less. Other arguments never throw.
- **Game failures.** When a read throws inside the game, it returns its neutral default. The first throw of each member is logged at Error. Later throws of that member are not logged again.
- **Events run inside the game's call.** Each event is raised from a postfix on the game method, on the main thread. Handlers run one at a time, so a handler that throws is logged and the other handlers still run. Do not start scene changes or NPC changes synchronously from a handler.
- **Per event.** Spawned and Despawned share the owner `obenseuer.moddingframework.npcs`, because they read the same list membership. Died has the owner `obenseuer.moddingframework.npcs.died`, and RelationshipChanged has `obenseuer.moddingframework.npcs.relationship`. If any hook of a group is missing in this build, that group's owner is disabled, one warning is logged, and only that group's events stop. The reads keep working.

## NPCs

```csharp
public sealed class NpcInfo
{
    public int Id { get; }
    public string Name { get; }          // Character.Name, the first name. null when the NPC has no character.
    public string DisplayName { get; }   // Character.FullName, first and last name. null when the NPC has no character.
    public Vector3 Position { get; }
    public bool Alive { get; }
    public string CurrentScene { get; }  // null when unknown.
}

public static class Npcs
{
    public static event Action<NpcInfo> Spawned;
    public static event Action<NpcInfo> Despawned;
    public static event Action<NpcInfo> Died;

    public static List<NpcInfo> All();
    public static NpcInfo Get(int id);           // null when no NPC has the id
    public static NpcInfo Find(string name);    // null when no NPC matches
}
```

- **Character definitions, including NPCs not in the scene:** see [characters.md](characters.md).
- **Which NPCs.** The list is the game's NPC database (`StreamingAssets/NPCBehavior.json`). `All` returns one entry for each database NPC, including NPCs that are not in the loaded scene. A scene instance with no database entry is not listed here. `InScene` lists it, see [NPCs in the loaded scenes](#npcs-in-the-loaded-scenes).
- **Position.** While the NPC is active in the loaded scene, this is its live transform position. Otherwise it is the last position the game stores for it. An off-screen NPC is moved along the game's path between scenes, and the stored position follows that path.
- **Alive.** `false` only when the scene instance is in the game's Dead state. The game keeps no death flag for an NPC that is not in a scene, so an off-screen NPC reports `true`. In 0.5.05 nothing in the game sets the Dead state except `NPCInfo.Kill`, and the game never calls it.
- **CurrentScene.** The scene name the game stores for the NPC.
- **Find.** Matches `Name` or `DisplayName`, ignoring case. The first match in database order wins, so duplicate names return the first NPC.
- Spawned and Despawned are not gated on a save load. They fire while a save loads, when the scene's NPCs are registered and removed, so a mod that tracks NPCs by these events still sees each one added and removed. They can also fire for an NPC of a neighbouring level that the game loads additively ahead of time, so an NPC can be reported before it is in the active scene.
- **Spawned** fires when an NPC instance is first registered in the loaded scene. For a spawn made by `Character.Spawn`, that is before the instance's `Start` runs. It does not fire again for the same instance.
- **Despawned** fires when a scene instance is destroyed, for example on a scene unload. The game deactivating an NPC when it leaves the scene does not fire Despawned.
- **Died** fires when the NPC instance's state changes from a non-Dead state to Dead. It does not fire when a save with a Dead NPC is loaded again.

## NPCs in the loaded scenes

```csharp
public sealed class SceneNpcInfo
{
    public string Scene { get; }         // the loaded scene the NPC object is in
    public string Path { get; }          // name path from the scene root, Root/Child/Grandchild (WorldObjects.PathOf)
    public string Handle { get; }        // the save GUID when the component has one, else scene:Name[index]/...
    public bool IsDirector { get; }      // true when the character id has a database entry
    public int Id { get; }               // Character.ID, -1 when the NPC has no character
    public string Name { get; }          // Character.Name, or the GameObject name when the NPC has no character
    public string DisplayName { get; }   // Character.FullName, null when the NPC has no character
    public Vector3 Position { get; }     // the object's position at the call
    public bool Active { get; }          // GameObject.activeInHierarchy at the call
    public bool Present { get; }         // see below
}

public static class Npcs
{
    public static List<SceneNpcInfo> InScene();
}
```

- `InScene` lists every `NPCInfo` component in the loaded scenes, inactive ones included, whether or not the NPC has a database entry. It is a snapshot taken at the call. It scans every object in the loaded scenes, so call it on an event or a timer, not every frame.
- `All` is unchanged. It lists database NPCs only. An NPC with no database entry is listed by `InScene` and not by `All`, if its object has an `NPCInfo` component. An object without one is listed by neither.
- `Handle` is the component's `GUID` field when it is not empty, otherwise `scene:Name[index]/...` from the scene root. It is stable only for an NPC placed in the scene with a serialized `NPCInfo.GUID`. `Character.Spawn` instantiates the prefab and does not set `GUID`, so a spawned NPC carries the prefab's value, which copies can share, or it has no GUID and its path changes when siblings are reordered. A name with `/`, `[` or `]` can make two NPCs give the same path (see [worldobjects.md](worldobjects.md)). Use `Id` for spawned NPCs.
- `Present` is true when `Active` is true. It is also true when the object is inactive only because its `FadeGameObject` hid it, by distance or by camera culling. That means the component is on the NPC's own GameObject, `hideAlways` is false, `isHidden` or the private `culled` flag is true, and the parent is active. A database NPC also needs its database `CurrentScene` to equal `Scene`. So an NPC the game moved to another scene is not present, even if its `FadeGameObject` still has `isHidden` set.
- `FadeGameObject` toggles one thing: the GameObject it is on, with `SetActive`. `Hide` and `Show` call `gameObject.SetActive(false)` and `SetActive(true)`. `UpdateFade` hides when the distance to the player is over `hideDistance`, which defaults to 20 m, and shows again within that distance. `isHidden` tracks the distance hide. `hideAlways` (set by `ChangeHiddenStatus`) hides at any distance. `culling` also calls `Hide` and `Show` but sets only the private `culled` flag, not `isHidden`. `Present` reads `culled` through a `FieldInfo` from `AccessTools.Field`, so a culled NPC is present under the same rules. If that field cannot be found, `Present` uses `isHidden` only, and the owner logs one warning. While the game saves, `FadeGameObjectController.ShowAll` shows every object.
- `Present` is read from the game's fields. It is not a game flag. A scene NPC that another script hid while `isHidden` was still set reads as present.

## Relationships

```csharp
public sealed class RelationshipValue
{
    public string Id { get; }
    public string Name { get; }
    public float Disposition { get; }   // -100 to 100
    public float Target { get; }        // the value Disposition drifts toward
    public bool Enabled { get; }
}

public sealed class RelationshipChange
{
    public string Id { get; }
    public string Name { get; }
    public float Previous { get; }
    public float Current { get; }
}

public static class Relationships
{
    public static event Action<RelationshipChange> RelationshipChanged;

    public static List<RelationshipValue> All();
    public static float GetDisposition(string id);   // 0 when the player does not have the relationship
    public static bool Has(string id);               // true when the player has the relationship
}
```

- **No per-NPC value.** In 0.5.05, a relationship value belongs to a `Relationship` asset, not to an NPC. The game has no NPC-keyed value to read.
- **No writer.** The game moves `Disposition` toward `Target` once a day, and no game method sets either one. This facade does not change them.
- **RelationshipChanged** fires when the game's daily drift changes a value. It fires once per day for each relationship that moves. It does not fire when the list of relationships changes.

## Phones

```csharp
[Flags]
public enum PhoneNetwork { None = 0, Obenseuer = 1, Outside = 2, Wasteland = 4 }

public sealed class PhoneContact
{
    public string Id { get; }            // the PhoneNumber asset name
    public string Number { get; }        // digits, * for 10 and # for 11
    public PhoneNetwork Network { get; }
    public float AnswerChance { get; }
}

public static class Phones
{
    public static List<PhoneContact> All();
}
```

- `All` lists the numbers the game can dial: entries that are not `notIndexed` and have digits.
- **No add.** The game has no runtime method that adds a number. `Phone.additionalNumbers` is set in the inspector only, so this area does not expose an add. `PhoneNumberDatabase.database` is a static list filled from `Resources` at start, and no save code reads or writes it, so a runtime add would be lost on the next load. The write is skipped, and `Phones.All` stays read only.
- Placing a call is in [Phone lines](phone-lines.md).

## Animals and osrats

```csharp
public enum AnimalKind { Other = 0, Cat = 1, Chick = 2, Chicken = 3, Pig = 4, Rat = 5 }

public sealed class AnimalInfo
{
    public AnimalKind Kind { get; }
    public string Name { get; }          // the scene object name
    public Vector3 Position { get; }
    public bool Tame { get; }
    public bool Following { get; }       // in the game's list of animals following the player
    public bool Running { get; }
    public int Id { get; }               // the scene instance id, valid for this session only
    public float Health { get; }         // the game's animalData.currentHealth, 0 without AnimalStats
    public int MaxHealth { get; }        // the game's animalData.maxHealth, 0 without AnimalStats
    public float Hunger { get; }         // the game's animalData.currentHunger, 0 without AnimalStats
    public int MaxHunger { get; }        // the game's animalData.maxHunger, 0 without AnimalStats
    public float Age { get; }            // the game's animalData.currentAge, 0 without AnimalStats
    public int FullGrownAge { get; }     // the game's animalData.fullGrownAge, 0 without AnimalStats
}

public static class Animals
{
    public static int Count { get; }                // live animal instances, from the game's own counter
    public static List<AnimalInfo> All();        // live animal instances in the active scene
    public static bool Damage(int animalId, float amount);   // the game's damage path, Violence type
}

public static class Osrats
{
    public static int Strength { get; }           // strength last set on the osrat manager, 0 when there is no signal
    public static float RadiusStrength { get; }   // radius strength last set by an osrat trigger
    public static bool IsBeeping { get; }
    public static bool IsEquipped { get; }
    public static bool IsSignalDisturbed { get; }
}
```

- `Animals.Count` is the game's counter. It goes up when an animal starts and down when it is destroyed. It is approximate if an animal is destroyed before it starts.
- `Animals.All` lists the animal objects in the active scene. `Kind` is `Other` for animal types this facade does not name. A destroyed animal is not listed. Animal positions are the live transform positions.
- `Animals.All` scans every object in the active scene each call. Do not call it every frame. Call it on a timer or in response to an event, and cache the result.
- `Id` is the object's instance id (`GetInstanceID`). It is valid for this session only and is not saved, so look an animal up again after a load. `Health` and `MaxHealth` are the game's `ItemAnimalData.currentHealth` and `maxHealth`, read through the animal's `AnimalStats`. Both are `0` when the animal has no `AnimalStats` or no data. `Hunger`, `MaxHunger`, `Age` and `FullGrownAge` are read the same way from `currentHunger`, `maxHunger`, `currentAge` and `fullGrownAge`, as the game's raw values. `FullGrownAge` is `0` until the game first calls `ItemAnimalData.SetDefaultValues`, which it does when a growing animal first ages or when stats data with a `0` value is assigned. The flags `hungerEnabled` and `growingEnabled` are not exposed.
- `Animals.Damage(id, amount)` calls the game's `AnimalStats.Damage` with the Violence damage type. It lowers `Health` by `amount`. An animal with an immune Violence type takes no damage. The game then does the hurt reaction, death, drops and the destroy. The game's `AnimalStats.OnSavingGame` saves the animal's data. It returns `false` while a save loads, when the damage controller is not loaded, when no live animal has that `Id`, or when the animal has no `AnimalStats`, or is dead (its `Health` is `0` or less, so a second call does not run `Death` again). It returns `true` once the game's damage call has run. It throws `ArgumentException` for an amount that is not finite or is `0` or less. Inactive animals are not found.
- `Animals.Damage` scans every animal, like `Animals.All`, so do not call it every frame. Call it in response to an event.
- `Animals.Damage(id, amount)` returns `true` and lowers the animal's `Health`, which shows on the next `All` call. A lethal amount kills the animal, and it is gone from `All` from the next frame on. It returns `false` for a dead or destroyed animal, and throws `ArgumentException` for an amount of `0`.
- The other animal scene classes are not wrapped: `AnimalAnimationEvents`, `AnimalAttackTrigger`, `AnimalFoodDetector` and `CatEars` run the game's own behaviour and hold no state a mod needs. `AnimalSpawner` is listed by `Spawners` (`docs/api/spawners.md`).
- There is one osrat device, so there is no osrat count. The osrat triggers are scene objects with no registry, so they are not counted either.

## If a hook cannot be installed

- The framework installs the NPC event hooks at start. Reads do not need them.
