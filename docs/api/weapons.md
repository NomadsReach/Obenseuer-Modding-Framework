# Obenseuer Modding Framework weapons API (weapons)

Status: experimental. Not checked in game.
Game: Obenseuer 0.5.05 (build 25839246), Unity Mono.
Namespace: `ObenseuerCore`. Log owner: `obenseuer.moddingframework.weapons`. Logs go to `BepInEx\logs\obenseuer.moddingframework.weapons.log`.

Everything not listed here is internal. Items marked **experimental** are not checked in game.

## Rules

- **Main thread only.** Call every member and handle every event on Unity's main thread.
- **Availability.** Every member is safe at any time: main menu, loading, no save loaded. Reads return a neutral default (`null`, `0`, or `false`). `Weapons.Reload` returns `false` in the same cases.
- **Events.** `Weapons.IsAvailable` is false when the weapon hooks were not installed or the owner was disabled. The events never fire then. The reads keep working.
- **Cost.** Each hook returns at once while its event has no subscriber, so an unused event costs one null check per game call.
- **Events run inside the game's call.** Each event is raised from a postfix on the game method, on the main thread. Handlers run through `Events.Raise`, so a handler that throws is logged and the other handlers still run.
- **All or nothing.** The three event hooks are one patch set under the owner `obenseuer.moddingframework.weapons`. If any hook is missing in this build, the owner is disabled, one warning is logged, and the events never fire.
- **Damage.** Weapon damage is not repeated here. Use `Survival.Damaged` (`float` lost, `DamageTypeNames` reason).

## Types

- `WeaponKind`: `None`, `Melee`, `Explosive`. There is no `Gun`: this game build has no firearm. See "Not in this version".
- `WeaponInfo` (read-only snapshot): `ItemId`, `Kind`, `AmmoLoaded`, `AmmoCapacity`.
- `WeaponFiredArgs` (event payload): `ItemId`, `Kind`, `AmmoLoaded`.

## Reads

- `WeaponInfo Weapons.Equipped()`: the item in the primary (right) hand. Returns `null` when the hand is empty. `Kind` is `None` for an item that is not a weapon, such as a tool. `AmmoLoaded` and `AmmoCapacity` come from the item's ammo data, rounded down. Both are `0` when the item has no ammo data. The left hand is not read.
- `int Weapons.Ammo(int ammoItemId)`: the number of that item carried, from the inventory count (`PlayerInventory.Count`). For an ammo item that stores charge in its own ammo data, this counts the stacks, not the charge.

## Writes

- `bool Weapons.Reload()`: calls the game's own reload for the primary-hand item, the same call the Primary Reload key makes (`PlayerHandItems.ReloadUse`). It returns `true` when the call was made. It returns `false` when a menu is open, item use is disabled, nothing is held, or the held item has no ammo data or is `None`. The game can still refuse the reload (cooldown, ammo already full, no ammo item carried), so `true` means the request was sent. The ammo refills after the game's cooldown (1.6 s for a Detonator). Trigger pulls are not simulated.

## Events

- `event Action<WeaponFiredArgs> Fired`: raised when a Detonator press uses a charge. `Kind` is `Explosive`. Nothing else raises it.
- `event Action<WeaponInfo> MeleeSwung`: raised when a melee swing's hit window opens. It fires once per swing, not per hit.
- `event Action<WeaponInfo> Reloaded`: raised when a Detonator reload starts and its ammo item is consumed. The refill lands later, so the snapshot shows the ammo from before the refill.

## Not in this version

- **Guns.** The game has no firearm that the player can hold. The seven hand item classes are `Osrat`, `BasicWieldable`, `BasicWieldableItem`, `Detonator`, `Flashlight`, `MeleeWeapon` and `Dosimeter`, and none of them fires. `GunShoot` and `GunAim` are a stock particle-effect demo: `ParticleMenu` only toggles a `gunGameObject` for weapon particle effects. They read `Input.GetButtonDown("Fire1")`, are not hand items and keep no ammo, so no event or `WeaponKind` is offered for them.
- **Melee hits.** `MeleeWeapon.Hit` runs every frame. There is no per-hit event.
- **Left hand.** `Equipped` and `Reload` use the primary hand only.
- **Osrat and the dosimeter.** They are hand items with ammo data, but they are not weapons. `Kind` is `None` for them.
