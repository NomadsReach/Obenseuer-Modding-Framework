# Obenseuer Modding Framework NPC API (npcs)

Status: experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.npcs`. Logs go to `BepInEx\logs\obenseuer.moddingframework.npcs.log`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- `Npcs.IsAvailable` is false when the NPC hooks were not installed or the owner was disabled. `Relationships.IsAvailable` returns the same value, because `RelationshipChanged` comes from those hooks.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`0`, `0f`, `false`, `null`, or an empty list). The only writer in the area is `Animals.Damage`.
- **Bad arguments.** `Npcs.Find`, `Relationships.GetDisposition` and `Relationships.Has` throw `ArgumentException` for a null or empty string. `Animals.Damage` throws `ArgumentException` for an amount that is not finite or is `0` or less. Other arguments never throw.
- **Game failures.** When a read throws inside the game, it returns its neutral default. The first throw of each member is logged at Error. Later throws of that member are not logged again.
- **Events run inside the game's call.** Each event is raised from a postfix on the game method, on the main thread. Handlers run through `Events.Raise`, so a handler that throws is logged and the other handlers still run. Do not start scene changes or NPC changes synchronously from a handler.
- **All or nothing.** The NPC event hooks are one patch set under the owner `obenseuer.moddingframework.npcs`. If any hook is missing in this build, the owner is disabled, one warning is logged, and the events never fire. The reads keep working.

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

- **Which NPCs.** The list is the game's NPC database (`StreamingAssets/NPCBehavior.json`). `All` returns one entry for each database NPC, including NPCs that are not in the loaded scene. A scene instance with no database entry is not listed.
- **Position.** While the NPC is active in the loaded scene, this is its live transform position. Otherwise it is the last position the game stores for it. An off-screen NPC is moved along the game's path between scenes, and the stored position follows that path.
- **Alive.** `false` only when the scene instance is in the game's Dead state. The game keeps no death flag for an NPC that is not in a scene, so an off-screen NPC reports `true`. In 0.5.05 nothing in the game sets the Dead state except `NPCInfo.Kill`, and the game never calls it.
- **CurrentScene.** The scene name the game stores for the NPC.
- **Find.** Matches `Name` or `DisplayName`, ignoring case. The first match in database order wins, so duplicate names return the first NPC.
- **Spawned** fires when an NPC instance is first registered in the loaded scene. For a spawn made by `Character.Spawn`, that is before the instance's `Start` runs. It does not fire again for the same instance.
- **Despawned** fires when a scene instance is destroyed, for example on a scene unload. The game deactivating an NPC when it leaves the scene does not fire Despawned.
- **Died** fires when the NPC instance's state changes from a non-Dead state to Dead. It does not fire when a save with a Dead NPC is loaded again.

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
- **No add.** The game has no runtime method that adds a number. `Phone.additionalNumbers` is set in the inspector only, so this area does not expose an add.

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
}

public static class Animals
{
    public static int Count { get; }                // live animal instances, from the game's own counter
    public static List<AnimalInfo> All();        // live animal instances in the loaded scene
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
- `Animals.All` lists the animal objects in the loaded scene. `Kind` is `Other` for animal types this facade does not name. A destroyed animal is not listed. Animal positions are the live transform positions.
- `Animals.All` scans every object in the scene each call. Do not call it every frame. Call it on a timer or in response to an event, and cache the result.
- `Id` is the object's instance id (`GetInstanceID`). It is valid for this session only and is not saved, so look an animal up again after a load. `Health` and `MaxHealth` are the game's `ItemAnimalData.currentHealth` and `maxHealth` (`Items/OS.Items/ItemAnimalData.cs:29,31`), read through the animal's `AnimalStats`. Both are `0` when the animal has no `AnimalStats` or no data.
- `Animals.Damage(id, amount)` calls the game's `AnimalStats.Damage` (`NPC/AnimalStats.cs:145-159`) with the Violence damage type. It lowers `Health` by `amount`. An animal with an immune Violence type takes no damage. The game then does the hurt reaction, death, drops and the destroy. The game's `AnimalStats.OnSavingGame` (`NPC/AnimalStats.cs:31`) saves the animal's data. It returns `false` while a save loads, when the damage controller is not loaded, when no live animal has that `Id`, or when the animal has no `AnimalStats`, or is dead (its `Health` is `0` or less, so a second call does not run `Death` again). It returns `true` once the game's damage call has run. It throws `ArgumentException` for an amount that is not finite or is `0` or less. Inactive animals are not found. Not verified in game.
- `Animals.Damage` scans every animal, like `Animals.All`, so do not call it every frame. Call it in response to an event.
- Smoke test for `Animals.Damage`: find a live chicken in `Animals.All`, call `Animals.Damage(chicken.Id, 1f)`, and expect `true`. Its `Health` drops by 1 on the next `All` call. Call it with a large amount and expect the chicken to die and be removed from `All` from the next frame on. Call it again and expect `false`. Call it with `0` and expect an `ArgumentException`. Call it with the `Id` of a destroyed animal and expect `false`.
- There is one osrat device, so there is no osrat count. The osrat triggers are scene objects with no registry, so they are not counted either.

## Init

- `Npcs.Init` is internal. Core calls it once at start to install the NPC event hooks. Reads do not need it.
