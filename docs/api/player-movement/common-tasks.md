---
title: Common tasks
parent: Player movement
grand_parent: Player life and skills
nav_order: 1
---

# Common tasks

Hold the player still while your mod plays a scene, then let go. The player cannot move or look around while the lock is held:

```csharp
if (PlayerMovement.Lock("example.speedy")) Log.Info("The player is held.");
PlayerMovement.Unlock("example.speedy");
```

Hold the player and turn the camera toward something. The turn only happens in first person. `target` is the thing to look at, and `true` snaps the camera at once:

```csharp
void LookAtIt(Transform target)
{
    PlayerMovement.LockLookAt("example.speedy", target, true);
}
```

Stop only the mouse look, and leave walking free:

```csharp
PlayerMovement.LockLook("example.speedy");
PlayerMovement.UnlockLook("example.speedy");
```

Make the player crouch, then stand up again. `StandUp` returns `false` when there is a ceiling over the player and they stay crouched:

```csharp
PlayerMovement.Crouch();
if (!PlayerMovement.StandUp()) Log.Info("No room to stand up here.");
```

Give the player a higher jump, and take every change your mod made back:

```csharp
PlayerMovement.SetJumpMultiplier("example.speedy", 2f);
PlayerMovement.ClearMultipliers("example.speedy");
```
