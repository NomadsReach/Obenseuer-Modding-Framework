---
title: Common tasks
parent: NPCs
grand_parent: People
nav_order: 1
---

# Common tasks

```csharp
NpcInfo npc = Npcs.Find("Some Name");
if (npc != null) Log.Info(npc.Name + " is at " + npc.Position + " in " + npc.CurrentScene);
```

```csharp
foreach (RelationshipValue relationship in Relationships.All())
    Log.Info(relationship.Name + ": " + relationship.Disposition);
```

```csharp
Relationships.RelationshipChanged += change => Log.Info(change.Name + " went from " + change.Previous + " to " + change.Current);
```

```csharp
int following = 0;
foreach (AnimalInfo animal in Animals.All())
    if (animal.Following) following++;
Log.Info(following + " animals are following you.");
```

```csharp
foreach (PhoneContact contact in Phones.All())
    Log.Info(contact.Id + ": " + contact.Number);
```
