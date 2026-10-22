---
title: NPCs
parent: People
grand_parent: API reference
has_children: true
nav_order: 11
---

# NPCs

Status: experimental.

## What it is

This API lists NPCs and their spawns, despawns and deaths, and reads [relationships](relationships.md) and the [phone book](phones.md). It also finds and hurts [cats, chicks, chickens, pigs and rats](animals.md). The osrat is [on the weapons page](weapons/osrat-signal-detector.md#osrat-signal-detector), not an animal.

## When to use it

- Log a named NPC's death.
- Count NPCs or list the living in a scene.

## Quick example

Logs NPC deaths, and counts NPCs once a save loads.

```csharp
using ObenseuerCore;

[ModInfo("example.npcwatch", "NPC Watch", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class NpcWatch : FrameworkMod
{
    protected override void OnLoad()
    {
        Npcs.Died += npc => Log.Info((npc.DisplayName ?? npc.Name ?? "An NPC") + " died in " + npc.CurrentScene);
        Events.WhenReady(() =>
        {
            int alive = 0;
            foreach (NpcInfo npc in Npcs.All())
                if (npc.Alive) alive++;
            Log.Info("The game knows " + Npcs.All().Count + " NPCs, " + alive + " alive.");
        });
    }
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

public enum PhoneRefusal
{
    None, NotReady, SaveBusy, BadInput, BadNumber,
    BadNetwork, BadAnswerChance, Duplicate, UnknownConversation, Failed
}

public static class Phones
{
    public static List<PhoneContact> All();
    public static PhoneContact Add(string modId, string number, PhoneNetwork network,
        float answerChance, string conversationTitle, out PhoneRefusal refusal);
    public static bool Remove(string modId, string number);
    public static int Clear(string modId);
}
```

- `All` lists the numbers the game can dial: entries that are not `notIndexed` and have digits.
- `Add` puts a number in the phone book and points it at a conversation you made with [dialogue authoring](dialogue-authoring.md). It returns the contact, or `null` with a reason in `refusal`. Calling it again with the same mod and number updates that entry instead of adding a second one, so it is safe to call on every start.
- **Add it again every time the game starts.** The phone book is never saved <span class="tech">(`PhoneNumberDatabase.database` is a static list the game fills from `Resources` once per run)</span>. Call `Add` from your mod's `OnLoad` or from `Events.WhenReady`.
- `Add` can accept a number before the game has built its phone book, which happens when a mod calls it very early. The contact comes back with `refusal` set to `NotReady`, `All` lists it, and the framework puts it in the book at the next scene load. It is not diallable until then, and it is dropped with a log if the number turns out to clash with a built in one. A number may be at most 10 keys, the length of the dial pad.
- A number is refused when the mod id is not valid, the number is empty or holds anything but `0` to `9`, `*` or `#`, the number is already in the book, the network is `None`, the answer chance is outside 0 to 1, or no conversation of that title is loaded.
- `Remove` takes your own number out again, and `Clear` takes out every number your mod added and returns how many went.
- **Keep a mod number to outgoing calls.** The game saves the phone's incoming call <span class="tech">(`Interaction/Phone.cs` marks `incomingCall` with `fsProperty` and writes it in `OnSavingGame`)</span>, and a mod number is built at runtime rather than loaded from the game's assets, so it has no asset path to save by and would be written out by value. Dialling out never touches that field.
- A mod number is dialled the same way as the game's own, from a wall phone and from the handheld phone <span class="tech">(both reach `PhoneNumberDatabase.CallNumber`)</span>.
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
