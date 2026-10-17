---
title: Common tasks
parent: Scene looks
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

Find a look by its name, load it, and put it on a body that your mod has spawned. `body` here is the NPC's object in the scene. `Apply` tells you why it refused through `reason`.

```csharp
GameObject body = GameObject.Find("My Mod NPC");
SceneLookInfo info = SceneLooks.FindByName("Interior Tenement Speakeasy", "mr_duck");
if (body != null && info != null)
{
    SceneLook look = SceneLooks.Load(Mod.Id, info);
    string reason;
    if (!look.Ready)
        Log.Warn("Could not load the look: " + look.Reason);
    else if (!SceneLooks.Apply(Mod.Id, look, body, out reason))
        Log.Warn("Could not apply the look: " + reason);
    else
        Log.Info("The look is on the body.");
}
```

Put a look on an NPC that is already in the scene and moving about. This works the same as `Apply`.

```csharp
GameObject npc = GameObject.Find("Some Live NPC");
SceneLookInfo info = SceneLooks.FindByCharacter("Interior Tenement Speakeasy", 1);
if (npc != null && info != null)
{
    SceneLook look = SceneLooks.Load(Mod.Id, info);
    string reason;
    if (look.Ready && !SceneLooks.Reskin(Mod.Id, look, npc, out reason))
        Log.Warn("Reskin refused: " + reason);
}
```

Take the look off a body, then let go of the look so its memory can be freed.

```csharp
GameObject body = GameObject.Find("My Mod NPC");
SceneLookInfo info = SceneLooks.FindByName("Interior Tenement Speakeasy", "mr_duck");
if (body != null && info != null)
{
    SceneLook look = SceneLooks.Load(Mod.Id, info);
    string reason;
    SceneLooks.Remove(Mod.Id, body, out reason);
    SceneLooks.Release(Mod.Id, look);
}
```
