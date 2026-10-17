---
title: Common tasks
parent: Landmarks
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

Check whether the player has a landmark, and unlock it if not. The id is the landmark's id in the game, which you can see by listing `Landmarks.All()`.

```csharp
if (!Landmarks.Has("some.landmark.id"))
    Landmarks.Unlock("some.landmark.id");
```

List every landmark the game has loaded and whether the player owns it.

```csharp
foreach (LandmarkInfo landmark in Landmarks.All())
    Log.Info(landmark.Id + ": " + landmark.Title + (landmark.Owned ? " (owned)" : ""));
```

Create a new landmark with your own icon. The icon is a PNG or JPEG file inside your mod's folder, here next to your mod's DLL.

```csharp
string folder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
string icon = System.IO.Path.Combine(folder, "well.png");
Landmarks.Create(Mod.Id, "well", "Old well", new Vector3(120f, 0f, -40f), icon);
```

Move a landmark you made. There is no move call, so remove it and create it again. Then take away all your landmarks.

```csharp
Landmarks.Remove(Mod.Id, "well");
Landmarks.Mark(Mod.Id, "well", "Old well", new Vector3(130f, 0f, -40f));
Landmarks.Clear(Mod.Id);
```
