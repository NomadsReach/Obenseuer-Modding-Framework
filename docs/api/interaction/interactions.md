---
title: Interactions
parent: Interaction and locks
grand_parent: Places and travel
nav_order: 2
---

# Interactions

This is the full list of what the API gives you, one section for each part. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

An interaction is the prompt the game shows when the player looks at something they can use. `Interactions` lets you add your own prompt to an object, listen for the player using any object, and read what they are looking at.

```csharp
public static class Interactions
{
    public static event Action<InteractionArgs> Used;   // the player has used an object

    public static bool IsAvailable { get; }             // false when the Used event or the lock events could not be set up

    // Gives an object a prompt that reads "label (Use)". Returns true when it worked.
    public static bool Add(GameObject target, string modId, string label, Action use);
    // The same, but the word in brackets is verb instead of Use.
    public static bool Add(GameObject target, string modId, string label, string verb, Action use);
    // Sets a red reason shown instead of the verb. The function returns the text, or null for no reason.
    public static bool SetReason(GameObject target, string modId, Func<string> reason);
    // Takes your prompt off the object. Returns true when it was removed.
    public static bool Remove(GameObject target, string modId);
    // What the crosshair is pointing at right now; null when it is pointing at nothing usable.
    public static InteractionArgs Looking();
}

public sealed class InteractionArgs    // one object the player used or is looking at
{
    public string Name { get; }          // the object's name in the scene
    public string Kind { get; }          // the kind of usable thing it is; ModInteraction for prompts made with Add; null when it has none
    public Vector3 Position { get; }     // where the object is, in world coordinates
    public float Distance { get; }       // metres from the camera to the object's centre
    public string Guid { get; }          // the object's save id, or null when it has none
    public GameObject Target { get; }    // the object itself
}
```

- **`Add` gives an object the game's own prompt and crosshair.** The prompt reads your `label`, then the verb in brackets, for example `Treadmill (Run)`. Pass the bare word as the verb, because the game adds the brackets. A verb that starts with `(` is shown as you gave it. A null, empty or blank verb shows `Use`, and the four-argument `Add` is the same as the five-argument one with `Use`.
- **`Add` returns `false` when it can't add the prompt.** That happens when the object has no collider (the shape that the crosshair hits), when it already has a prompt from the game (the game's prompt would win), when another mod already added a prompt to it, while the game is loading or saving a save, or when the framework can't change the game. In the case of another mod, the framework writes one warning that names both mods.
  - Technical: it needs a `Collider`, no game `Interactable` on the object, and no `ModInteraction` from a different mod id. A game `Interactable` makes the call return `false` and logs one warning.
- **`Add` throws an error for missing inputs.** A null object, label or `use` action throws `ArgumentNullException`, and an empty mod id throws `ArgumentException`. These checks come before the check for a save loading.
- **The prompt only works if the object sits on a layer the player can interact with.** If it already does, nothing changes. If not, `Add` moves the object to one that works, and `Remove` puts it back. If no such layer exists, `Add` returns `false`.
  - Technical: the layers tried are 17, 23, 29, 20 and 15, in that order. If the object's layer is one of these and `InteractObjects.MaskList` includes it, it stays. Otherwise the first listed layer that the mask includes is used.
- **`SetReason` shows a red reason in the prompt, the way the game shows a bed that can't be used yet.** Your function returns the text. A null or empty result means no reason, and the normal prompt shows. The reason is put in brackets like the verb, so `Too hungry` shows as `(Too hungry)`. While a reason is set, using the object plays the game's warning effect and does not run your `use` action. `SetReason(target, modId, null)` clears it. It returns `false` when your mod has no prompt on that object.
  - Technical: the function runs each time the game updates the prompt, which can be every frame, so keep it quick and run only code from your mod's normal code. In third person the list shows only the title, but selecting an entry still makes it the game's current interactable, so the use is still blocked. `Used` still fires, because the game still calls `Interact()`. If the function throws, the first error is logged once for that object and the object is treated as having no reason. A reason stays when the same mod calls `Add` again, and `Remove` drops it. `SetReason` requires a mod id of 1 to 64 characters of letters, digits, `.`, `_` or `-`; `Add` only requires a non-empty one.
- **`Remove` takes off only a prompt that your mod added.** It returns `false` otherwise. If you call `Remove` from inside the prompt's own `use` action, the removal waits until the end of the frame. An `Add` for the same object in that same call returns `false`, so call `Add` on its own to change the label.
- **`Used` fires after the player uses any object, including prompts made with `Add`.** `Guid` is the object's save id or `null`. `Distance` is measured from the camera to the object's centre. If another mod stops the game's own use step, `Used` can still fire.
  - Technical: it is raised after `InteractObjects.UseInteractable` called `Interact()` on the target. `Kind` is the component's class name. The postfix also runs after a skipped original, provided the framework's prefix ran first, and the prefix order was not checked.
- **`Looking()` reads what the crosshair hits within the game's reach of 3 metres.** `Kind` is `null` when the object has nothing the player can use. `Distance` goes to the object's centre, not to the spot the crosshair hits.
- **You can't cancel a use from `Used`, and you can't make an object hold-to-use.** The handlers' results are not collected, so there is no clean rule for skipping a use. No mod has asked for either.
