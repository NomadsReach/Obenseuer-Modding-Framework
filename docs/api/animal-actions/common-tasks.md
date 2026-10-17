---
title: Common tasks
parent: Animal actions
grand_parent: Animals and plants
nav_order: 1
---

# Common tasks

Make an animal sit.

```csharp
List<AnimalInfo> animals = Animals.All();
if (animals.Count > 0) AnimalActions.Sit(Mod.Id, animals[0].Id);
```

Keep a pig asleep until your mod lets it go, then let go of everything your mod holds.

```csharp
AnimalInfo pig = Animals.All().Find(a => a.Kind == AnimalKind.Pig);
if (pig != null) AnimalActions.SleepPermanently(Mod.Id, pig.Id);
AnimalActions.Clear(Mod.Id);
```

Send a cat after a target. The target must be an object with health, such as a rat. Here the target is found by the rat's name in the scene.

```csharp
AnimalInfo cat = Animals.All().Find(a => a.Kind == AnimalKind.Cat);
AnimalInfo rat = Animals.All().Find(a => a.Kind == AnimalKind.Rat);
if (cat != null && rat != null)
{
    GameObject ratObject = GameObject.Find(rat.Name);
    if (ratObject != null) AnimalActions.Hunt(Mod.Id, cat.Id, ratObject.transform);
}
```

Make the first tame animal wild.

```csharp
AnimalInfo tame = Animals.All().Find(a => a.Tame);
if (tame != null) AnimalActions.SetTame(Mod.Id, tame.Id, false);
```
