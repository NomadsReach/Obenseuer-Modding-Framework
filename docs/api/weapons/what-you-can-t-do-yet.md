---
title: What you can't do (yet)
parent: Weapons
grand_parent: Player life and skills
nav_order: 8
---

# What you can't do (yet)

- You can't read or react to guns. The game has no gun the player can hold. The seven hand item classes are `Osrat`, `BasicWieldable`, `BasicWieldableItem`, `Detonator`, `Flashlight`, `MeleeWeapon` and `Dosimeter`, and none of them fires. The two gun scripts in the game files are a demo for particle effects. They are not hand items and keep no ammo, so there is no event and no `WeaponKind` for them. <span class="tech">(`GunShoot` and `GunAim` read `Input.GetButtonDown("Fire1")`, and `ParticleMenu` only toggles a `gunGameObject`.)</span>
- You can't use `Hit` as a swing event. `Hit` fires for each hit a target accepts. A swing that hits nothing raises nothing. Use `MeleeSwung` for swings. <span class="tech">(`MeleeWeapon.Hit` runs every frame, so it is not a swing point. `Hit` is raised from the target's damage call.)</span>
- `Reload` works on the right hand only. `EquippedLeft` reads the left hand.
- The osrat and the dosimeter are not weapons. They have ammo data, but `Kind` is `None` for them.
- Hit factors do not apply to NPCs or wild animals. A melee hit on an NPC starts a crime reaction but does not deal damage that this API could scale, and a melee hit does not reach wild animals. Animal props, the `Animal` target, are covered. <span class="tech">(Only the classes in the Hit power table implement `IDamageReceiver`. The NPC branch of `MeleeWeapon.Hit` starts crime, barks and relays and computes no damage.)</span>
