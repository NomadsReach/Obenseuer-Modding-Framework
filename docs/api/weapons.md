---
title: Obenseuer Modding Framework weapons API (weapons)
parent: API reference
nav_order: 89
---

# Obenseuer Modding Framework weapons API (weapons)

Status: experimental.
Target: Obenseuer 0.5.08 (buildid 25969064), Unity 6000.0.77f1 Mono, BepInEx 6 Unity.Mono (be.755 or newer, be.788 recommended) or BepInEx 5 (5.4.23 or later).
Namespace: `ObenseuerCore`. Log owners: `obenseuer.moddingframework.weapons`, `obenseuer.moddingframework.weapons.reload`, `obenseuer.moddingframework.weapons.melee` and `obenseuer.moddingframework.weapons.hit`. Logs go to `BepInEx\logs\obenseuer.moddingframework.weapons.log`.

Everything not listed here is internal. Items marked **experimental** may change.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`null`, `0`, or `false`). `Weapons.Reload` returns `false` in the same cases, and while a save loads or saves (`SaveData.Busy`).
- **Events.** `Weapons.IsAvailable` is false when any of the three weapon hooks was not installed, or one of the owners `obenseuer.moddingframework.weapons`, `.reload` or `.melee` was disabled. A missing hook stops only its own event, and the other two keep firing while `IsAvailable` is `false`. The reads keep working.
- **Cost.** The `Hit` hook writes two static fields on every call, and `MeleeWeapon.Update` calls `Hit` every frame while a melee weapon is held. The receiver `Damage` hooks return at once when no factor and no subscriber are set. The other hooks return at once while their event has no subscriber, so an unused event costs one null check per game call.
- **Events run inside the game's call.** Each event is raised from a postfix on the game method, on the main thread. Handlers run one at a time, so a handler that throws is logged and the other handlers still run.
- **Per event.** Each event has its own owner: `Fired` under `obenseuer.moddingframework.weapons`, `Reloaded` under `obenseuer.moddingframework.weapons.reload` and `MeleeSwung` under `obenseuer.moddingframework.weapons.melee`. If a hook is missing in this build, its owner is disabled, one warning is logged, and only its event never fires. `Hit` and the hit factors have their own owner, `obenseuer.moddingframework.weapons.hit`, and are not part of `IsAvailable`. If that hook set is incomplete, `SetHitFactor` returns `false` and `Hit` never fires.
- **Damage.** Weapon damage is not repeated here. Use `Survival.Damaged` (`float` lost, `DamageTypeNames` reason).

## Types

- `WeaponKind`: `None`, `Melee`, `Explosive`. There is no `Gun`: this game build has no firearm. See "Not in this version".
- `WeaponInfo` (read-only snapshot): `ItemId`, `Kind`, `AmmoLoaded`, `AmmoCapacity`.
- `WeaponFiredArgs` (event payload): `ItemId`, `Kind`, `AmmoLoaded`.
- `HitTarget`: `Other`, `Mining`, `Chopping`, `Breakable` and `Animal`. See Hit power.
- `HitArgs` (event payload): `Weapon` (`WeaponInfo`), `Target` (`HitTarget`), `Receiver` (`GameObject`), `BaseDamage` and `FinalDamage` (`int`).

## Reads

- `WeaponInfo Weapons.Equipped()`: the item in the primary (right) hand. Returns `null` when the hand is empty. `Kind` is `None` for an item that is not a weapon, such as a tool. `AmmoLoaded` and `AmmoCapacity` come from the item's ammo data, rounded down. Both are `0` when the item has no ammo data. The left hand is read by `EquippedLeft`.
- `WeaponInfo Weapons.EquippedLeft()`: the same snapshot for the left hand. It returns `null` when the left hand is empty. A flashlight held there has `Kind` `None`, and its battery shows as `AmmoLoaded` and `AmmoCapacity`. `Reload` still uses the primary hand only.
- A held `WieldableItem.BasicWieldable` or `WieldableItem.BasicWieldableItem` (for example `WieldableGuitar`) is reported by `Equipped` and `EquippedLeft` the same way, with `Kind` `None`. Its `equipped` and `fullyEquipped` flags are not exposed.
- `int Weapons.Ammo(int ammoItemId)`: the number of that item carried, from the inventory count (`PlayerInventory.Count`). For an ammo item that stores charge in its own ammo data, this counts the stacks, not the charge.

## Writes

- `bool Weapons.Reload()`: calls the game's own reload for the primary-hand item, the same call the Primary Reload key makes (`PlayerHandItems.ReloadUse`). It returns `true` only when the game took the reload, which shows as an ammo item leaving the inventory. It returns `false` when a menu is open, item use is disabled, nothing is held, or the held item has no ammo data or is `None`. It also returns `false` when the game refuses (cooldown, ammo already full, no ammo item carried), because those refusals consume nothing. The ammo refills after the game's cooldown (1.6 s for a Detonator). Trigger pulls are not simulated.

## Events

- `event Action<WeaponFiredArgs> Fired`: raised when a Detonator press uses a charge. `Kind` is `Explosive`. Nothing else raises it.
- `event Action<WeaponInfo> MeleeSwung`: raised when a melee swing's hit window opens. It fires once per swing, not per hit.
- `event Action<WeaponInfo> Reloaded`: raised when a Detonator reload starts and its ammo item is consumed. The refill lands later, so the snapshot shows the ammo from before the refill.
- `event Action<HitArgs> Hit`: raised once for each melee hit that a receiver accepts, before the receiver applies the damage. `BaseDamage` is the weapon's damage and `FinalDamage` is the damage after the factor. For a resource node both are before the skill bonus. A resource node with no stock, or a receiver that refuses the damage type, does not raise it. See Hit power.

## Hit power

Melee hits scale per target kind. The factors are kept for the session only.

```csharp
public static bool SetHitFactor(string modId, HitTarget target, float factor);
public static bool SetHitFactor(string modId, HitTarget target, float factor, bool chanceRounding);
public static bool ClearHitFactors(string modId);
```

- `SetHitFactor` takes a factor from 0 to 10. `1` leaves the hit alone, `0` deals no damage and `2` doubles it. The scaled damage is `Math.Round` of damage times factor (`Api/PayoutFactors.cs:33`), so a half rounds to the even neighbour. Damage is a whole number, so low-damage tools lose fine factors: on damage 1, `0.5` gives 0, `1.4` gives 1, `1.5` gives 2 and `2.5` gives 2. It returns `false` when the mod id is invalid, the target is not a `HitTarget`, the factor is `NaN`, infinite, negative or above 10, or the hit hooks are unavailable.
- The four-argument overload with `chanceRounding` set to `true` keeps the fraction as a chance instead of rounding it. The whole part of the scaled damage is kept, and one more point is added with a probability equal to the fraction, using `UnityEngine.Random`. On damage 1, a factor of `1.3` gives 2 about 30% of the hits and 1 otherwise. A factor of `1.5` gives 2 about half the time. A factor of `0.5` gives 0 or 1, one half of the time each. The three-argument overload is `chanceRounding` `false`, so it keeps the `Math.Round` above.
- The rounding follows the factor. The newest call for a target supplies its factor, and that same entry's `chanceRounding` decides the rounding for every hit on the target. Factors are not multiplied, so an older `chanceRounding` `true` factor does not apply once a newer call for the target sets `false`, and the reverse.
- `ClearHitFactors(modId)` removes that mod's factors for every target. It returns `false` when the mod id is invalid or the mod had none.
- Per target, the newest call wins. Factors for different targets do not interact, and two mods' values are not multiplied. When a second mod sets a target that another mod already set, the log names both mods once, and the newest value applies.
- A Single scene load clears every factor, and so does loading a save, which loads the player's level as a Single scene load. A neighbouring scene loaded additively does not. Set factors again from `Events.Loaded` or `Events.SceneLoaded` after each reset.
- Scope: only the damage of melee hits, from `MeleeWeapon.Hit`. Other weapons, explosions and the player's own damage are not changed.

| Target | Receiver | How it is detected |
| --- | --- | --- |
| `Mining` | resource node (`WorkableResourceSource`) whose skill is `Mining` | the node's `skillType` field, the skill the game gains on each hit |
| `Chopping` | resource node that is not a mining node, hit with the damage type `Chop` | the `DamageType` the melee weapon passes |
| `Other` | any other resource node | fallback |
| `Breakable` | `Breakable`, `BreakableLock`, and `SpawnItemOnBreak` that is not an animal | the class |
| `Animal` | `SpawnItemOnBreak` whose `objectType` is `Animal` | the `objectType` field |

Not included:

- NPCs. No class a melee hit reaches takes damage from it. Only the classes in the table implement `IDamageReceiver`. The NPC branch of `MeleeWeapon.Hit` starts crime, barks and relays and computes no damage, so an NPC factor would do nothing.
- Wild animals. A melee hit does not reach them. Animal props, the `Animal` target above, are included.
- Guns. This build has none.

The game code has no prefab data, so which nodes use the `Mining` skill and which weapons use the `Chop` type is not listed here.

## Flashlight

The flashlight is the hand item `WieldableItem.Flashlight`. These calls read the primary hand first and then the left hand, so they find a flashlight in either. `Equipped` and `Reload` do not. `EquippedLeft` reports a left-hand flashlight as an item with `Kind` `None`. A flashlight worn in a character slot is not in either hand and is not read.

```csharp
public static bool? FlashlightOn();
public static float? FlashlightCharge();
public static bool SetFlashlightOn(bool on);
```

- `FlashlightOn` is `true` while the beam is lit: the switch is on and the battery has charge. It reads the game's private `on` field. When the battery runs out, `DrainCharge` turns the beam off but leaves `on` set (`Flashlight`), so `FlashlightOn` reads `false` until the battery has charge again. During a reload animation `FlashlightOn` can read `true` while the beam is off: `Reload` sets the charge to full and keeps `on`, and the beam comes back only when `OnReloadDone` (`Flashlight`, called from `FlashlightAnimationEvents`) runs `TurnOnLight`. It is `false` for a held flashlight that is not lit, and `null` when no flashlight is held.
- `FlashlightCharge` is the item's `ammo / maxAmmo` (`ItemAmmodata`), from `0` to `1`. It is not rounded. It is `null` when no flashlight is held or the item has no ammo data.
- `SetFlashlightOn` calls the game's own `TurnOn` or `TurnOff`. It returns `true` when the beam state equals `on` afterwards. It returns `false` when no flashlight is held, a menu is open, item use is disabled (the same gate as `Reload`), or a save or load is running. With a flat battery, turning on leaves the beam off, so it returns `false`, and the game shows its red notice and reload tutorial (`Flashlight`). After a failed turn-on the switch stays in the on position, and `TurnOn` returns at once while `fullyEquipped` is true (`Flashlight`), so call `SetFlashlightOn(false)` to reset it.
- Turning the beam on starts the battery drain (`SetDrainChargeStatus`, `Flashlight`). The drain runs every second through `TimeOfDayAzure.SecondsPassed`.
- Not in the save: the switch and the beam are not saved by the flashlight. The charge is the item's ammo meta, which the inventory save keeps.
- No charge write is offered. The game changes the charge in two places, the drain (`DrainCharge`, `Flashlight`) and the reload from an ammo item in the player's inventory (`onReloadUse`). Writing `ammo` directly would skip the inventory step.

## Not in this version

- **Guns.** The game has no firearm that the player can hold. The seven hand item classes are `Osrat`, `BasicWieldable`, `BasicWieldableItem`, `Detonator`, `Flashlight`, `MeleeWeapon` and `Dosimeter`, and none of them fires. `GunShoot` and `GunAim` are a stock particle-effect demo: `ParticleMenu` only toggles a `gunGameObject` for weapon particle effects. They read `Input.GetButtonDown("Fire1")`, are not hand items and keep no ammo, so no event or `WeaponKind` is offered for them.
- **Melee hits.** `MeleeWeapon.Hit` runs every frame, so it is not a swing point. `Hit` is raised from the receiver's damage call, so a frame with no receiver hit raises nothing.
- **Left hand.** `Reload` uses the primary hand only. `EquippedLeft` reads the left hand.
- **Osrat and the dosimeter.** They are hand items with ammo data, but they are not weapons. `Kind` is `None` for them.

## Save loads

The framework does not gate this event on a save load. A handler that should ignore events raised while a save loads can check `SaveData.Busy`, which is true while a save is loading or being written.
