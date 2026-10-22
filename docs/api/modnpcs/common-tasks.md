---
title: Common tasks
parent: Mod NPCs
grand_parent: People
nav_order: 1
---

# Common tasks

This page uses `Vector3`, `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Every block on this page is a small mod of its own. Copy one whole, or keep its `using` lines and its two attributes and move the call into your own mod. All of them do their work inside `Events.WhenReady`, because adding, removing or reading people touches the world.

Add a person who comes back after every load, then look up their character number and list all the people your mod has added.

```csharp
using ObenseuerCore;
using UnityEngine;
using UnityEngine.SceneManagement;

[ModInfo("example.shop", "Shop Mod", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Shopkeeper : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            string scene = SceneManager.GetActiveScene().name;
            int saved = ModNpcs.SpawnSaved(Mod.Id, "shopkeeper", sourceId, "Sam", "Stone", spot, 90f, scene, false);
            if (saved == 0)
            {
                Log.Warn("The shopkeeper was refused. See the spawn limits.");
                return;
            }

            int characterId = ModNpcs.CharacterIdOf(Mod.Id, "shopkeeper");
            Log.Info("The shopkeeper is character " + characterId + ".");
            foreach (ModNpcInfo npc in ModNpcs.List(Mod.Id))
                Log.Info(npc.DisplayName + " is at " + npc.Position + ", alive: " + npc.Alive);
        });
    }
}
```

`SpawnSaved` returns `0` for the reasons in the [spawn limits](spawn.md#limits), and `CharacterIdOf` returns `0` when there is no such person. `List` gives you one `ModNpcInfo` for each person your mod has added right now.

Remove one person, or all of them. A scene change takes the person away anyway, so this is what a mod does when it wants to start from a known state.

```csharp
using ObenseuerCore;

[ModInfo("example.reset", "Reset", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ResetPeople : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            // Despawn removes one person, by the id you gave it.
            if (!ModNpcs.Despawn(Mod.Id, "shopkeeper"))
                Log.Warn("There was no shopkeeper to remove.");

            // Clear does the same for every person this mod added.
            if (!ModNpcs.Clear(Mod.Id))
                Log.Warn("This mod had no people to clear.");
        });
    }
}
```

`Despawn` and `Clear` return `false` when your mod has no matching person. They don't wait for the game, except for a person saved with `SpawnSaved` during a load or a save.

Add a background figure, take it away again, and remove all your figures. An extra is not a person: it stands and watches, and it is never saved. See [Extras](extras.md).

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.crowd", "Crowd", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Crowd : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 6f;
            GameObject extra = ModNpcs.SpawnExtra(Mod.Id, "crowd-1", sourceId, sourceId, spot + Vector3.right, 180f);
            if (extra == null)
            {
                Log.Warn("The extra was refused. See the extras limits.");
                return;
            }

            Log.Info("The extra stands at " + extra.transform.position + ".");
        });
    }
}
```

`SpawnExtra` returns `null` for the same reasons as `Spawn`, and the [extra limits](extras.md#limits) apply on top: one mod can have 64 extras, and all mods together 256. Take one away with `DespawnExtra(modId, extraId)`, or all of them away with `ClearExtras(modId)`, which returns how many it removed.

Add a person with a look taken from the game's own files. The look comes from [Scene looks](../scenelooks.md).

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.lookalike", "Lookalike", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class Lookalike : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            SceneLookInfo info = SceneLooks.FindByCharacter(sourceId);
            if (info == null)
            {
                Log.Warn("The game has no look for that character.");
                return;
            }

            SceneLook look = SceneLooks.Load(Mod.Id, info);
            if (!look.Ready)
            {
                Log.Warn("The look is not ready: " + look.Reason);
                return;
            }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            int id = ModNpcs.SpawnWithLook(Mod.Id, "lookalike", sourceId, look, "Lou", "Lookalike", spot, 0f);
            if (id == 0)
                Log.Warn("The lookalike was refused. See the look limits.");
        });
    }
}
```

`SceneLooks.Load` always gives you a `SceneLook`, so check `Ready` before you use it. `SpawnWithLook` returns `0` for the reasons in the [SpawnWithLook limits](spawnwithlook.md#limits), and it takes the person out again when the look can't be put on the body.
