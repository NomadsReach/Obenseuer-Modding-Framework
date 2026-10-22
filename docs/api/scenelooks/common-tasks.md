---
title: Common tasks
parent: Scene looks
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

This page uses `GameObject`, `Vector3` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Every block on this page is a small mod of its own. Copy one whole, or keep its `using` lines and its two attributes and move the calls into your own mod. All of them do their work inside `Events.WhenReady`, because they read people and the world. A mod NPC's object is named `modId/npcId`, so `GameObject.Find(Mod.Id + "/guard")` is the object of the mod NPC whose id is `guard`.

Find a look by its name, load it, and put it on a body that your mod has spawned. `Apply` tells you why it refused through `reason`.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.applylook", "Apply a Look", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ApplyLook : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            if (ModNpcs.Spawn(Mod.Id, "guard", sourceId, "Duck", "Guard", spot, 0f) == 0)
            {
                Log.Warn("The guard was refused. See the spawn limits.");
                return;
            }

            GameObject body = GameObject.Find(Mod.Id + "/guard");
            SceneLookInfo info = SceneLooks.FindByName("Interior Tenement Speakeasy", "mr_duck");
            if (body == null || info == null) return;

            SceneLook look = SceneLooks.Load(Mod.Id, info);
            string reason;
            if (!look.Ready)
                Log.Warn("Could not load the look: " + look.Reason);
            else if (!SceneLooks.Apply(Mod.Id, look, body, out reason))
                Log.Warn("Could not apply the look: " + reason);
            else
                Log.Info("The look is on the body.");
        });
    }
}
```

`ModNpcs.Spawn` gives `0` for the reasons in the [spawn limits](../modnpcs/spawn.md#limits). `Load` always gives a handle, and `Ready` says whether it loaded. `Apply`, `Reskin` and `Remove` give `false` with a `reason` when they refuse, for the reasons in the [apply limits](putting-a-look-on-a-body.md#limits).

Put a look on an NPC that is already in the scene and moving about. The block lists the NPC objects in the scene, gets each one's object from its scene and its path, and calls `Reskin`.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.reskinlive", "Reskin a Live NPC", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class ReskinLive : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            SceneLookInfo info = SceneLooks.FindByCharacter("Interior Tenement Speakeasy", 1);
            if (info == null) return;

            foreach (SceneNpcInfo npc in Npcs.InScene())
            {
                if (!npc.Present || npc.Scene != "Interior Tenement Speakeasy") continue;

                GameObject live = WorldObjects.FindByPath(npc.Scene, npc.Path, true);
                if (live == null) continue;

                SceneLook look = SceneLooks.Load(Mod.Id, info);
                if (!look.Ready) return;
                if (!SceneLooks.Reskin(Mod.Id, look, live, out string reason))
                    Log.Warn("Reskin refused: " + reason);
                return;
            }
        });
    }
}
```

Take the look off a body, then let go of the look so its memory can be freed.

```csharp
using ObenseuerCore;
using UnityEngine;

[ModInfo("example.removelook", "Remove a Look", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public sealed class RemoveLook : FrameworkMod
{
    protected override void OnLoad()
    {
        Events.WhenReady(() =>
        {
            int sourceId = 0;
            foreach (CharacterInfo character in Characters.All())
                if (!character.IsPlayer) { sourceId = character.Id; break; }

            Vector3 spot = PlayerState.Position + Vector3.forward * 3f;
            if (ModNpcs.Spawn(Mod.Id, "guard", sourceId, "Duck", "Guard", spot, 0f) == 0)
            {
                Log.Warn("The guard was refused. See the spawn limits.");
                return;
            }

            GameObject body = GameObject.Find(Mod.Id + "/guard");
            SceneLookInfo info = SceneLooks.FindByName("Interior Tenement Speakeasy", "mr_duck");
            if (body == null || info == null) return;

            SceneLook look = SceneLooks.Load(Mod.Id, info);
            if (!look.Ready)
            {
                Log.Warn("Could not load the look: " + look.Reason);
                return;
            }

            string reason;
            if (!SceneLooks.Apply(Mod.Id, look, body, out reason))
            {
                Log.Warn("Could not apply the look: " + reason);
                return;
            }

            if (!SceneLooks.Remove(Mod.Id, body, out reason))
                Log.Warn("Could not remove the look: " + reason);
            SceneLooks.Release(Mod.Id, look);
        });
    }
}
```

`Release` lets go of every hold your mod has on the look. It does not take the look off the body, which is what `Remove` does.
