---
title: The rules
parent: Scene looks
grand_parent: Places and travel
nav_order: 2
---

# The rules

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

Some terms are used on this page. A rig is the skeleton that a model is bound to, so it can bend and move. A bone is one joint of that skeleton. A skinned mesh is a model that follows the skeleton. A body root is the top object of a character's model in a scene.

- **Every call must come from your mod's normal code, not from a background thread you started.** The game and Unity only work safely on one thread, the main thread (see the [glossary](../../glossary.md#main-thread)).
- **The look comes only from the game that is installed.** The framework finds the game's files next to the running game. No path is written into the framework, and no copy of the art is shipped or written.
  - Technical: files are found through `Application.dataPath`.
- **Only one Unity version is supported, and on any other version every call refuses.** `List` gives back an empty array, `FindByName` and `FindByCharacter` give back `null`, and `Load` gives back a handle that has failed. The framework writes the reason to the log once. Nothing is guessed for another version.
  - Technical: `Application.unityVersion` and each level file's metadata must be `6000.0.77f1`. A "level file" is one of the game's scene data files.
- **Nothing here throws an error.** A refusal or a game failure is written to the log once, and comes back as an empty array, `null`, a handle with `Failed` set and `Reason` filled in, or `false` with a `reason` you can read.
- **A body can have only one look.** `Apply` and `Reskin` refuse a body that already has a look. They also refuse a body that sits above or below a body that has one, so two looks cannot end up on one nested skeleton. The look is added under the body. The body's own models are switched off, not destroyed, and `Remove` switches them back on.
  - Technical: the body's own `SkinnedMeshRenderer`s are disabled, and only those. Each one comes back with the `enabled` value it had before.
- **A look is not saved.** If the scene changes, a look you applied is gone. A look on a spawned NPC has to be applied again after a load. See [Mod NPCs](../modnpcs.md) for `SpawnSaved`.
- **The framework only reads the game's files.** It never writes to them.
- **Faces keep their neutral shape.** The look's face shapes are read and checked, but are not applied yet.
  - Technical: blend shapes are not applied in this first version.
- **Animation always comes from the NPC you put the look on.** A look's own animation is never copied.
