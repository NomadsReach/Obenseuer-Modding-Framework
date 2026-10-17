---
title: Locking only the mouse look
parent: Player movement
grand_parent: Player life and skills
nav_order: 5
---

# Locking only the mouse look

A look lock stops the player turning the camera with the mouse, and leaves walking free. It is counted for each mod id like `Lock`.

```csharp
public static class PlayerMovement
{
    public static bool LockLook(string modId);     // stops the mouse look; false if it could not
    public static bool UnlockLook(string modId);   // lets go of your newest look lock; false if you hold none
}
```

- **`LockLook(modId)` stops the player's mouse look and leaves movement free.** Two `LockLook` calls from one mod need two `UnlockLook` calls. It returns `false` and takes nothing when the camera is not loaded, while a save is loading or being written, or when the rotation hooks did not set up. A bad mod id throws an `ArgumentException`.
  - Technical: it sets the game's rotation flag through `ThirdPersonCameraController.DisableRotation`.
- **`UnlockLook(modId)` releases the newest look lock that your mod took.** It returns `false` if your mod holds none.
- **The game can still turn rotation on and off itself, and your lock stays.** Rotation is off while the game wants it off or any mod holds a look lock. So when the game turns rotation back on (in a chair, at a computer, on a ladder, in the cards game), it never ends your lock. And `UnlockLook` never turns rotation back on if the game turned it off.
  - Technical: postfixes on `DisableRotation` and `EnableRotation` track whether the game wants rotation off. The framework's own calls are ignored.
- **A single-scene load clears every look lock, and so does the player dying.** The camera may be replaced, so the game's tracked state is reset. Take your lock again from your own handler. There is no event for the main menu; the main menu is loaded as a single scene.
  - Technical: the death case is `Survival.PlayerDied`.
