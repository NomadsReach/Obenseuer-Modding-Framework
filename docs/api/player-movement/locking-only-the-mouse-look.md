---
title: Locking only the mouse look
parent: Player movement
grand_parent: Player life and skills
nav_order: 5
---

# Locking only the mouse look

A look lock stops the player turning the camera with the mouse, and leaves walking free. It is counted for each mod id like `Lock`.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>PlayerMovement</code> <span>stop and restore the mouse look</span></p>

Call `LockLook` to stop the mouse look while the player can still move, and `UnlockLook` to give it back.

<div class="api-pair" markdown="1">

```csharp
public static class PlayerMovement
{
    public static bool LockLook(string modId);
    public static bool UnlockLook(string modId);
}
```

| Method | Returns | What it does |
|---|---|---|
| `LockLook(modId)` | `bool` | Stops the mouse look. `false` if it could not. |
| `UnlockLook(modId)` | `bool` | Lets go of your newest look lock. `false` if you hold none. |

</div>
</div>

## Things to know

- `LockLook(modId)` stops the player's mouse look and leaves movement free. Two `LockLook` calls from one mod need two `UnlockLook` calls. <span class="tech">(it sets the game's rotation flag through `ThirdPersonCameraController.DisableRotation`.)</span>
- The game can still turn rotation on and off itself, and your lock stays. Rotation is off while the game wants it off or any mod holds a look lock. So when the game turns rotation back on (in a chair, at a computer, on a ladder, in the cards game), it never ends your lock.
- `UnlockLook` never turns rotation back on if the game turned it off. <span class="tech">(postfixes on `DisableRotation` and `EnableRotation` track whether the game wants rotation off. The framework's own calls are ignored.)</span>

## Limits

- `LockLook` returns `false` and takes nothing when the camera is not loaded, while a save is loading or being written, or when the rotation hooks did not set up. A bad mod id throws an `ArgumentException`.
- A single-scene load clears every look lock, and so does the player dying. The camera may be replaced, so the game's tracked state is reset. Take your lock again from your own handler. <span class="tech">(the death case is `Survival.PlayerDied`.)</span>
- There is no event for the main menu. The main menu is loaded as a single scene.
