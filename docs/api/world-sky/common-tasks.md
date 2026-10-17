---
title: Common tasks
parent: World sky
grand_parent: Places and travel
nav_order: 1
---

# Common tasks

Read what the sky looks like now.

```csharp
SkyState now = WorldSky.Current;
if (now != null) Log.Info("Fog density " + now.FogDensity + ", sun intensity " + now.SunIntensity);
```

Make the clouds disappear and the stars bright.

```csharp
SkyOverride night = new SkyOverride();
night.CloudDensity = 0f;
night.StarsIntensity = 2f;
WorldSky.Set(Mod.Id, night);
```

Put the values back to what the game does.

```csharp
WorldSky.Clear(Mod.Id);
```

Set your values again after every load, because they are not kept.

```csharp
Events.SceneLoaded += scene =>
{
    SkyOverride dim = new SkyOverride();
    dim.AmbientIntensity = 0.3f;
    WorldSky.Set(Mod.Id, dim);
};
```
