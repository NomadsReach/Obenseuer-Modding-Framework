---
title: Camera
parent: Player
grand_parent: Player life and skills
nav_order: 9
---

# Camera

`CameraView` changes how the camera sees the player. This part covers the player's body in first person, and third person with your mod's id. For the field of view and zoom, see [Screen effects](../screen-fx.md).

```csharp
public static class CameraView
{
    public static bool SetFirstPersonBody(string owner);      // keeps the player's body visible in first person
    public static bool ClearFirstPersonBody(string owner);    // lets the body hide again
    public static bool SetThirdPerson(string owner, float distance);   // asks for third person at this distance
    public static bool SetFirstPerson(string owner);          // gives up your request for third person
}
```

- **`SetThirdPerson(owner, distance)` asks for third person, as your mod's claim.** It adds your mod to the list of mods that want third person, and keeps the game's first-person lock cleared while any mod is on the list. It returns `false` when third person is not allowed (see `ThirdPersonAllowed`).
- **`SetFirstPerson(owner)` gives up your request.** When no mod is left, the game's lock is put back, the camera goes to first person, and it returns `true`. While another mod still wants third person, the camera stays and it returns `false`.
- **The versions without an owner share one claim of their own.** That covers `SetThirdPerson(distance)`, `SetFirstPerson()` and `Toggle()`. The framework's own F6 toggle (`Tweaks.Camera`) uses a separate claim, and turning `ThirdPersonToggle` off removes only that one. A new scene load drops every claim and puts the game's lock back.
- **`SetFirstPersonBody(owner)` keeps the player's body visible in first person, for selfies.** Normally the game hides the body in first person. While any mod holds this, the body stays shown. If the camera is already in first person, the body shows at once. It returns `false` when this part could not be set up, and throws an `ArgumentException` for a bad owner.
  - Technical: the game hides the body in `ThirdPersonCameraCollision.FirstPerson` through `HidePlayerModel`. A prefix on `HidePlayerModel` skips that hide while any owner is set.
- **`ClearFirstPersonBody(owner)` removes your claim.** When the last claim goes and the camera is in first person, the body is hidden again, as the game does. It returns `false` when your mod had no claim.
- **Several mods can hold it at once.** The body stays visible until all of them have cleared it.
- **A new scene load clears every claim,** the same as for the field of view override and hiding the HUD. Set yours again from your own `Events.SceneLoaded` handler.
- **The body is the game's own model, placed at the camera.** The head or neck may block the view, and the body may clip the camera. Third person is unchanged, because the body is already shown there. The Mods menu's model preview also hides the body itself, and while a claim is held, this hide is skipped there too.
  - Technical: the Mods menu preview calls `HidePlayerModel`, which the prefix skips.

```csharp
CameraView.SetFirstPersonBody("com.example.selfie");
CameraView.ClearFirstPersonBody("com.example.selfie");
```
