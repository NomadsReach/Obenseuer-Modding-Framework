---
title: Camera
parent: Player
grand_parent: Player life and skills
nav_order: 9
---

# Camera

`CameraView` changes how the camera sees the player. This part covers the player's body in first person, and third person with your mod's id. For the field of view and zoom, see [Screen effects](../screen-fx.md).

<div class="api-type" markdown="1">
<p class="api-type-head"><code>CameraView</code> <span>body and third-person claims</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class CameraView
{
    public static bool SetFirstPersonBody(string owner);
    public static bool ClearFirstPersonBody(string owner);
    public static bool SetThirdPerson(string owner, float distance);
    public static bool SetFirstPerson(string owner);
}
```

| Method | Returns | What it does |
|---|---|---|
| `SetFirstPersonBody(string owner)` | `bool` | Keeps the player's body visible in first person, for selfies. |
| `ClearFirstPersonBody(string owner)` | `bool` | Removes your claim to keep the body visible. |
| `SetThirdPerson(string owner, float distance)` | `bool` | Asks for third person at this distance. |
| `SetFirstPerson(string owner)` | `bool` | Gives up your request for third person. |

</div>
</div>

## Example

<div class="api-example" markdown="1">

```csharp
CameraView.SetFirstPersonBody("com.example.selfie");
CameraView.ClearFirstPersonBody("com.example.selfie");
```

</div>

## Things to know

- `SetThirdPerson(owner, distance)` asks for third person as your mod's claim. It adds your mod to the list of mods that want third person, and keeps the game's first-person lock cleared while any mod is on the list. It returns `false` when third person is not allowed (see `ThirdPersonAllowed`).
- `SetFirstPerson(owner)` gives up your request. When no mod is left, the game's lock goes back on and the camera goes to first person, and it returns `true`. While another mod still wants third person, the camera stays where it is and it returns `false`.
- The versions without an owner share one claim of their own. That covers `SetThirdPerson(distance)`, `SetFirstPerson()` and `Toggle()`. The framework's own F6 toggle (`Tweaks.Camera`) uses a separate claim, and turning `ThirdPersonToggle` off removes only that one. A new scene load drops every claim and puts the game's lock back.
- `SetFirstPersonBody(owner)` keeps the player's body visible in first person, for selfies. Normally the game hides the body in first person. While any mod holds this, the body stays shown, and if the camera is already in first person the body shows at once. It returns `false` when this part could not be set up, and throws an `ArgumentException` for a bad owner. <span class="tech">(The game hides the body in `ThirdPersonCameraCollision.FirstPerson` through `HidePlayerModel`. A prefix on `HidePlayerModel` skips that hide while any owner is set.)</span>
- `ClearFirstPersonBody(owner)` removes your claim. When the last claim goes and the camera is in first person, the body is hidden again, as the game does. It returns `false` when your mod had no claim.
- Several mods can hold the body claim at once. The body stays visible until all of them have cleared it.
- A new scene load clears every claim, the same as for the field of view override and hiding the HUD. Set yours again from your own `Events.SceneLoaded` handler.
- Third person is unchanged, because the body is already shown there. The Mods menu's model preview also hides the body itself, and while a claim is held, this hide is skipped there too. <span class="tech">(The Mods menu preview calls `HidePlayerModel`, which the prefix skips.)</span>

## Limits

- The body is the game's own model, placed at the camera. The head or neck may block the view, and the body may clip the camera.
