---
title: Common tasks
parent: NPC appearance
grand_parent: People
nav_order: 1
---

# Common tasks

Hide one body part of a character, then put everything back. `npcId` is the character's number and `"Hat"` stands for a part name taken from `Meshes`. `Mod.Id` is your mod's id (see the [glossary](../../glossary.md#mod-id)).

```csharp
void HideAndRestore(int npcId)
{
    bool hidden = NpcAppearance.SetMeshActive(Mod.Id, npcId, "Hat", false);
    Log.Info("Hidden: " + hidden);
    NpcAppearance.Restore(Mod.Id, npcId);
}
```

Make a character built from sliders a bit taller. `GetLook` gives the current look and `SetLook` applies a changed copy.

```csharp
void Taller(int npcId)
{
    PlayerLook.Look look = NpcAppearance.GetLook(npcId);
    if (look == null) return;
    look.height += 0.05f;
    NpcAppearance.SetLook(Mod.Id, npcId, look);
}
```

Change one blend shape on a character with a baked look.

```csharp
void Squint(int npcId, string mesh, string shape)
{
    bool done = NpcAppearance.SetBlendShape(Mod.Id, npcId, mesh, shape, 60f);
    Log.Info("Shape changed: " + done);
}
```

Undo every change your mod made to any character.

```csharp
NpcAppearance.Clear(Mod.Id);
```
