---
title: Saving
parent: Landmarks
grand_parent: Places and travel
nav_order: 4
---

# Saving

The framework keeps your landmarks in its own file rather than in the game's save, because the game's save cannot hold a landmark that has no game file behind it.

## Things to know

- Your landmarks are saved in a separate file in the save's mods folder. The file is `obenseuer.moddingframework.landmarks.json`. It has one entry for each mod, landmark and map: the mod id, id, title, map, position, icon path (relative to the plugins folder), priority and size.
- The game's own save never holds your landmarks, so removing your mod does not break a save. The game loads with no error. The entries of a mod that is gone stay in the framework's file but are not drawn, so its markers leave the map, and a reinstall brings them back. <span class="tech">(The game's save keeps a landmark only as an addressable path, which a new landmark does not have. While `SaveController.SaveGame` runs, the mod landmarks are taken out of the game's map list and put back afterwards, even if the save throws. `SavingDone` fires inside `SaveGame`, after the game clears its own saving flag but before the finalizer puts the mod landmarks back. That is why a `Create`, `Mark`, `Remove` or `Clear` called from a `Saved` handler is refused while they are detached.)</span>
- Your landmarks come back when the game finishes loading the map. The framework adds the saved entries for the loaded map again, without a notification and without firing `Added`.
- The restore runs only for a mod that is present, which means its `modId` is the GUID of a loaded plugin, or the mod has already called `Create`, `Mark`, `Remove` or `Clear` this session. The first such call of a session also draws that mod's other saved entries for the loaded map. This also covers moving between maps in one session. <span class="tech">(It runs after `MapController.OnLoadingGame` and its 0.7 second delay. A loaded plugin is one in `UnityChainloader.Instance.Plugins`.)</span>
- A landmark made with `Mark` or with a file icon comes back by itself.
- A new game and the main menu empty the list in memory.
- A hand-edited file is checked when it is read. An entry is skipped if it has a bad mod id, id or map, a position that is not a real number, a negative size, or is a repeat. A saved icon path must be relative and stay inside the plugins folder. Any other path is skipped, with one line in the log, so a downloaded save cannot make the game read a file from another machine. <span class="tech">(Skipped paths include a network path, `..` and another drive.)</span>

## Limits

- A landmark made with the `Sprite` overload cannot save its icon, so it comes back only after your mod has called `Create` for it again that session.
- If a mod that is not present has a saved `Sprite` landmark, that entry is dropped when the file is read, so it does not use one of the 1024 places. `Create` then adds it again at the position your mod gives.
- A mod whose `modId` is not its plugin GUID is only counted as present after its first call this session. To be safe, use the plugin GUID as `modId`. You can remove your entries, drawn or not, with `Clear(modId)`.
