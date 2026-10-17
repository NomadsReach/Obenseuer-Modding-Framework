---
title: Common tasks
parent: Surveillance
grand_parent: Law and crime
nav_order: 1
---

# Common tasks

List the cameras in the scene, with how far and how wide each one sees.

```csharp
foreach (SurveillanceCameraInfo camera in Surveillance.Cameras())
    Log.Info(camera.Id + ": " + (camera.Active ? "on" : "off") + ", sees " + camera.Range + " m across " + camera.Fov + " degrees");
```

Switch one camera off. Take the id from `Surveillance.Cameras()`.

```csharp
string id = "some id from Surveillance.Cameras()";
bool off = Surveillance.SetCameraActive(id, false);
Log.Info(off ? "The camera is off." : "Could not switch the camera.");
```

Check whether someone could see a crime right now.

```csharp
if (Surveillance.IsPlayerWitnessed())
    Log.Info("Someone could see a crime right now.");
```

Check whether the player is in a trespassing area.

```csharp
if (Surveillance.IsTrespassing)
    Log.Info("The player is trespassing.");
```

Check whether every event works.

```csharp
if (!Surveillance.IsAvailable) Log.Warn("Some surveillance events are off.");
```
