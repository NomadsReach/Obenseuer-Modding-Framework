---
title: Tame and wild
parent: Animal actions
grand_parent: Animals and plants
nav_order: 3
---

# Tame and wild

`SetTame` changes whether an animal is tame. The game treats the two very differently: an animal that is not tame runs away when the player comes within 1.5 units, and a wild animal that has an attack trigger can bite the player.

- **`SetTame` sets the animal's tame flag and returns `true` when it did.** It returns `false` when the animal is not live (see above), when it has no item data, or when the saved choices are full (see below).
  - Technical: it calls `Animal.ChangeTameStatus` and writes the same value to `AnimalStats.animalData.tame` (`animalData` is the animal's item data). An animal that becomes an item keeps the value (in `GetItem`), and an animal item sets it when it is placed.
- **Making an animal wild also takes it off the list of animals that follow the player.** Making it tame does not make it follow you. The game carries following animals with the player to the next level, so making an animal a follower would move it between levels. This API does not do that.
  - Technical: `tame = false` removes the animal from `AnimalController.followingAnimals`.
- **A cat that is near a cat-ears object becomes tame again.** The game checks once a second and tames every cat within 10 units that is not following the player. Make a cat wild only when it is away from one.
  - Technical: the game's `CatEars` component tames every cat within `range` (10 units) on each scan (one second by default) when the cat is not in the following list.
- **The choice is kept when the player loads the save again.** The framework writes the choice into its own file in the save folder, with your mod id and the animal. After each load of that scene the choice is applied to the animal again. An animal that has no save id keeps the choice until the player quits the game.
  - **A choice from a mod that is not installed is kept, but not applied.** It comes back when the mod is installed again.
  - Technical: the file is `mods/obenseuer.moddingframework.animalactions.json` in the save folder, separate from the game's own save. The animal's save id is `AnimalStats.GUID`, and choices are matched by scene name and that id.
- **There is a limit on saved choices.** One mod can save up to 1,024 choices, and the file holds 16,384 in all. When either is full, `SetTame` returns `false` and changes nothing. Changing an animal that already has a choice still works. Choices of other mods, including mods that are not installed, do not count toward your mod's 1,024.
- **A saved choice is dropped when the game changes the animal on its own.** At each save, a choice is dropped if its animal is loaded and now has a different tame value. A hurt animal always becomes wild, so it does not get its choice back after a load. A choice for an animal that is not loaded is kept.
  - Technical: `Animal.OnHurt` sets the flag to `false`.
- **Two animals that share a save id cannot have a saved choice.** An animal placed from an item or a spawner can have the same save id as another animal in its scene. While that is true, `SetTame` still changes the animal, but saves no choice, and a load does not restore it. The framework writes one line about it to the log.
- **A saved choice belongs to the save id, not to one animal.** The game never changes that id, so a copy made from the same prefab (the template the game copies) has the same id. If the copy is the only live animal with that id after a load, it gets the choice that was saved for the animal it was copied from. The framework cannot tell them apart.
