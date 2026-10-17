---
title: Common tasks
parent: Instruments
grand_parent: Objects and devices
nav_order: 1
---

# Common tasks

List the guitars in the scene and say which ones can play a strum right now.

```csharp
foreach (GuitarInfo guitar in Instruments.All())
    Log.Info(guitar.Id + " held=" + guitar.Equipped + " canPlay=" + guitar.CanPlayNote);
```

Play one strum on every guitar that can play. The first argument is your mod's id.

```csharp
foreach (GuitarInfo guitar in Instruments.All())
    if (guitar.CanPlayNote) Instruments.PlayNote(Mod.Id, guitar.Id);
```

Play a short song of four strums on a guitar. The numbers are the waits in seconds: 0 before the first strum, then half a second, half a second and one second after each strum before it.

```csharp
string guitarId = "put an id from All() here";
bool started = Instruments.PlaySong(Mod.Id, guitarId, new float[] { 0f, 0.5f, 0.5f, 1f });
Log.Info("Song started: " + started);
```

Stop everything your mod is playing.

```csharp
Instruments.Clear(Mod.Id);
```
