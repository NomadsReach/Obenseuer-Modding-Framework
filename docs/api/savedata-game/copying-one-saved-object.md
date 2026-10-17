---
title: Copying one saved object
parent: Game save data
grand_parent: Framework basics
nav_order: 2
---

# Copying one saved object

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A savable object is an object the game saves, such as the player's inventory. `Capture` copies its entry into text, and `Apply` puts text back into it. Neither writes to disk.

```csharp
public static class SaveData
{
    public static string Capture(UnityEngine.Object target);                  // the object's save entry as compact text, or null
    public static bool Apply(UnityEngine.Object target, string json);         // puts a save entry into the object; true when the game took it
    public static string CaptureByGuid(string guid);                          // the same as Capture, for the object with this GUID; null when none
    public static bool ApplyByGuid(string guid, string json);                 // obsolete: trusts any GUID; use the overload with a list
    public static bool ApplyByGuid(string guid, string json, ICollection<string> allowedGuids);   // the same as Apply, only for the GUIDs in the list
    public static string SelfCheck();                                         // runs a built-in test; see below
}
```

- **Only objects the game saves can be copied.** The object must be one the game saves and must have a GUID. Anything else gives `null` from `Capture` and `false` from `Apply`.
  - Technical: the target must be a `SavableScript` with a non-empty public `GUID` field, the same test the game's `SaveController.SerializeData` uses.
- **Nothing happens while a save is loading or saving.** `Capture` gives `null` and `Apply` gives `false`.
  - Technical: both check `SaveData.Busy`.
- **`Capture` gives the entry exactly as the game would write it in a save.** It is compact text in the game's `{"GUID":..,"Data":{..}}` form. It gives `null` when the object is not savable, a save or load is running, the object wrote no entry, or the capture failed (the failure is logged).
  - Technical: `Capture` calls the object's own `OnSavingGame()` with the game's save lists swapped for private ones. The save method's other side effects run too. For example, `Inventory` copies its slots into the fields it saves, and into private fields its load method reads.
- **`Apply` puts the entry into the object the way a load does.** It gives `true` only when the game took the entry. It gives `false` for text it cannot parse, a GUID that does not match, a type that does not match, text over the size or depth limit, a type name it refuses, a refused state, or a failure (logged). It never throws.
  - Technical: it calls `OnLoadingGame()` with the entry in the load lists. Steps after the deserialize run too: for `Inventory` that rebuilds the live slots and calls `StartInit` and `ForceCharacterItemsUpdate`.
- **`Apply` checks everything before it changes anything.** The text is size and depth checked, then parsed. The GUID must equal the object's GUID, and the parsed data must have exactly the object's type. If any check fails, the object is not touched.
- **`Apply` sets the entry's type to the object's own type, whatever the entry says.** That way the game builds an object of the right type. An entry with no type gets one the same way (the current `Globals.tnmt` writes `Inventory` this way).
  - Technical: `Apply` sets the `$type` on `Data` to the target's type name.
- **`Apply` refuses entries that could make the game build the wrong thing.** It refuses a `Type` and `Data` pair anywhere in the entry (the game would turn it into a type), and a type name below `Data` that is not text, is over 1,024 characters, names no type the game can find, names a type from another Unity library, or is made of more than 64 parts. It looks inside arrays, generic types and base types too, for example `UnityEngine.GameObject[]`, `List<GameObject>` and `BehaviorDesigner.Runtime.SharedGameObjectList`. Types from the save's own library and the game's are allowed, because the game's own saves use them (for example `TaskItemBlueprint` and `WieldableItem.Flashlight`). A refusal is written to the log once as `save data apply refused`.
  - Technical: "a Unity type from another assembly" includes one used as an array element, as a generic argument of the type or of a base type, or of an `IEnumerable<T>` or `IDictionary<K,V>` it implements.
- **`Apply` limits the size of the text.** It refuses text over 4,194,304 characters, nested more than 256 levels deep, that closes more brackets than it opens, or that has a `/` outside a string. Nothing is parsed or changed, and the refusal is logged once per session as `save data apply refused`.
  - Technical: depth counts `[` and `{` outside strings. The game's parser skips `//` and `/* */` comments and loops forever on a `/` followed by neither `/` nor `*`, so every `/` outside a string is refused.
- **`Apply` is not a true undo if it fails.** After the checks pass, `Apply` copies the object's current state first. If the load fails, it puts that copy back and returns `false`. That brings back the fields the save holds, but not what the first run did outside them (item lookups, events and screen changes are not undone, and the inventory's setup runs a second time). Treat a `false` from `Apply` as possibly half applied. If no copy can be made, `Apply` refuses.
  - Technical: the copy is a `Capture`, so the object's save method runs on the live object on every `Apply` that passes the checks, before the load runs. For `Inventory` the limit is wider: `Slots` and `CharacterSlots` are `[fsIgnore]`, so the copy does not hold them, and the load copies a saved slot only when its amount is above 0 and its item id is above -1, never clearing a slot. A failed `Apply` can therefore leave items in slots the copy left empty.
- **The "by GUID" calls only find objects that a load registered.** An object created after the load is not found. `Apply` registers the object under its GUID, as a load does, so `CaptureByGuid` finds it afterwards.
  - Technical: they resolve through `SaveController.GetObjectByGUID`.
- **When another player chooses the GUID, always use the overload with a list of allowed GUIDs.** `ApplyByGuid(guid, json)` can reach any object the game registered on load, including the inventory. A player who sends the GUID `Inventory` could replace the host's inventory. `ApplyByGuid(guid, json, allowedGuids)` returns `false` unless `guid` is in the list. A `null` list refuses every GUID. The list can be any `ICollection<string>`, such as a `HashSet<string>` or an array. The two-argument form is marked obsolete (the compiler shows a warning, not an error) and is only for code you fully trust.
- **Mod ids for saved data are not case sensitive.** `SaveData.Register("MyMod", ...)` and `Register("mymod", ...)` are the same mod, and the second call throws `InvalidOperationException`. Item and recipe ids from `RegistryIds` are case sensitive, so the two rules differ. See [Registry API](../registry.md).
  - Technical: `Register` keys its savers, loaders and the save file name without regard to case.
