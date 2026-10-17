---
title: Custom landmarks
parent: Landmarks
grand_parent: Places and travel
nav_order: 3
---

# Custom landmarks

A custom landmark is one your mod makes. It does not exist in the game's files, so the framework keeps it in its own save file and draws it on the map for you.

```csharp
public static class Landmarks
{
    public static bool Create(string modId, string id, string title, Vector3 position, Sprite icon, int priority = 0, int width = 0, int height = 0, bool showNotification = true);   // a new landmark with an icon already in memory
    public static bool Create(string modId, string id, string title, Vector3 position, string iconPath, int priority = 0, int width = 0, int height = 0, bool showNotification = true);   // a new landmark with an icon from a file
    public static bool Mark(string modId, string id, string title, Vector3 position, bool showNotification = false);   // a new landmark with a red dot icon
    public static bool Remove(string modId, string id);   // removes one of your landmarks
    public static bool Clear(string modId);               // removes all of your landmarks
}
```

- **`Create` makes a new landmark for your mod and shows it on the map.** `modId` and `id` are each 1 to 64 characters of letters, digits, `.`, `_` or `-`. `title` is the text shown when the player points at the marker. `priority` decides which marker draws on top (a larger number is on top). `width` and `height` are the marker's size in map units, and `0` keeps the normal marker size. Use your mod's plugin GUID as `modId`, because the framework only brings your landmarks back after a load while your mod is installed (see [Saving](saving.md#saving)).
  - Technical: the landmark's game id is `modId + ":" + id`, so it cannot clash with a game landmark or another mod's. That is the id `Has`, `All` and `Added` use. The plugin must be a loaded plugin GUID for the restore. The size is the marker prefab's, so `0` keeps the prefab's size.
- **The `Sprite` overload takes an icon that only exists in memory, and the `iconPath` overload takes a picture file.** For a file, pass the full path of a PNG or JPEG that is no more than 1024 pixels on a side. The file must be inside the game's plugins folder (`BepInEx\plugins`). A network path, a path that leaves the folder, a path on another drive and a path that is not a full path are refused.
  - Technical: the file is loaded by the framework's icon loader and the path is saved relative to `BepInEx.Paths.PluginPath`. A path with `..` that still ends up inside the folder is accepted, because the path is made absolute before the check.
- **`Mark` is `Create` with a ready-made red dot icon and no notification.** The dot is 20 by 20 map units. The map only draws markers as landmarks, so a marker is a landmark with a stock icon.
- **`Create` and `Mark` give back `true` when the landmark is on the map afterwards.** They give back `false`, and place nothing, for these reasons:
  - an invalid argument, a position that is not a real number, a negative size, or an `iconPath` that cannot be read;
  - an `iconPath` outside the plugins folder (this is logged once);
  - `IsAvailable` is `false`;
  - a save or a load is running;
  - the call came from a `Saved` handler before the framework had put the mod landmarks back (see [Saving](saving.md#saving));
  - the map is still loading, or no map or map picture is loaded.

  When only the map is not ready, the framework keeps your landmark for the session, so a landmark of your mod that was saved still comes back when the load finishes.
- **If the player's save already has this landmark on the loaded map, `Create` keeps the saved position and the existing marker.** To move one, call `Remove` and then `Create`. A marker that is already drawn keeps its look until the scene loads again.
- **You can make at most 128 landmarks per mod and 1024 in all.** `Create` gives back `false` over the limit.
- **`Remove` deletes one of your landmarks from every saved map and hides its marker, and `Clear` does that for all of them.** Both give back `true` when something was removed. They give back `false` during a save or a load, and from a `Saved` handler before the mod landmarks were put back (see [Saving](saving.md#saving)). The hidden marker stays in the scene until the scene loads again, because the game has no call to delete a marker.
- **`Added` also fires for `Create` and `Mark`, the first time the marker is drawn.** It carries the landmark's game id. It does not fire for markers that are restored on a load.
