---
title: Interactions
parent: Interaction and locks
grand_parent: Places and travel
nav_order: 2
---

# Interactions

This page uses `Vector3`, `GameObject` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

An interaction is the prompt the game shows when the player looks at something they can use. `Interactions` lets you add your own prompt to an object, listen for the player using any object, and read what they are looking at.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>Interactions</code> <span>prompts on objects, and what the player uses</span></p>

This is the class you start from. `Add` gives an object a prompt, `Looking()` reads what the crosshair is pointing at, and the `Used` event tells you when the player uses any object.

<div class="api-pair" markdown="1">

```csharp
public static class Interactions
{
    public static event Action<InteractionArgs> Used;

    public static bool IsAvailable { get; }

    public static bool Add(GameObject target, string modId, string label, Action use);
    public static bool Add(GameObject target, string modId, string label, string verb, Action use);
    public static bool SetReason(GameObject target, string modId, Func<string> reason);
    public static bool Remove(GameObject target, string modId);
    public static InteractionArgs Looking();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Add(target, modId, label, use)` | `bool` | Gives an object a prompt that reads `label (Use)`. It is `true` when it worked. |
| `Add(target, modId, label, verb, use)` | `bool` | The same, but the word in brackets is `verb` instead of `Use`. |
| `SetReason(target, modId, reason)` | `bool` | Sets a red reason shown instead of the verb. The function returns the text, or `null` for no reason. |
| `Remove(target, modId)` | `bool` | Takes your prompt off the object. It is `true` when it was removed. |
| `Looking()` | `InteractionArgs` | What the crosshair is pointing at right now. It is `null` when it is pointing at nothing usable. |

| Property | Type | What it means |
|---|---|---|
| `IsAvailable` | `bool` | `false` when the `Used` event or the lock events could not be set up. |

| Event | Gives you | When it fires |
|---|---|---|
| `Used` | `InteractionArgs` | The player has used an object. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>InteractionArgs</code> <span>one object the player used or is looking at</span></p>

`Used` gives you this, and `Looking()` returns one too. `Kind` is `ModInteraction` for a prompt you made with `Add`.

<div class="api-pair" markdown="1">

```csharp
public sealed class InteractionArgs
{
    public string Name { get; }
    public string Kind { get; }
    public Vector3 Position { get; }
    public float Distance { get; }
    public string Guid { get; }
    public GameObject Target { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Name` | `string` | The object's name in the scene. |
| `Kind` | `string` | The kind of usable thing it is. It is `ModInteraction` for prompts made with `Add`, and `null` when it has none. |
| `Position` | `Vector3` | Where the object is, in world coordinates. |
| `Distance` | `float` | Metres from the camera to the object's centre. |
| `Guid` | `string` | The object's save id, or `null` when it has none. |
| `Target` | `GameObject` | The object itself. |

</div>
</div>

## Things to know

- `Add` gives an object the game's own prompt and crosshair. The prompt reads your `label`, then the verb in brackets, for example `Treadmill (Run)`. Pass the bare word as the verb, because the game adds the brackets. A verb that starts with `(` is shown as you gave it. A `null`, empty or blank verb shows `Use`, and the four-argument `Add` is the same as the five-argument one with `Use`. <span class="tech">(It needs a `Collider`, no game `Interactable` on the object, and no `ModInteraction` from a different mod id. A game `Interactable` makes the call return `false` and logs one warning.)</span>
- The prompt only works if the object sits on a layer the player can interact with. If it already does, nothing changes. If not, `Add` moves the object to one that works, and `Remove` puts it back. If no such layer exists, `Add` returns `false`. <span class="tech">(The layers tried are 17, 23, 29, 20 and 15, in that order. If the object's layer is one of these and `InteractObjects.MaskList` includes it, it stays. Otherwise the first listed layer that the mask includes is used.)</span>
- `SetReason` shows a red reason in the prompt, the way the game shows a bed that can't be used yet. Your function returns the text. A `null` or empty result means no reason, and the normal prompt shows. The reason is put in brackets like the verb, so `Too hungry` shows as `(Too hungry)`.
- While a reason is set, using the object plays the game's warning effect and does not run your `use` action. `SetReason(target, modId, null)` clears it. It returns `false` when your mod has no prompt on that object. <span class="tech">(The function runs each time the game updates the prompt, which can be every frame, so keep it quick and run only code from your mod's normal code. In third person the list shows only the title, but selecting an entry still makes it the game's current interactable, so the use is still blocked. `Used` still fires, because the game still calls `Interact()`. If the function throws, the first error is logged once for that object and the object is treated as having no reason. A reason stays when the same mod calls `Add` again, and `Remove` drops it. `SetReason` requires a mod id of 1 to 64 characters of letters, digits, `.`, `_` or `-`; `Add` only requires a non-empty one.)</span>
- `Remove` takes off only a prompt that your mod added. It returns `false` otherwise. If you call `Remove` from inside the prompt's own `use` action, the removal waits until the end of the frame. An `Add` for the same object in that same call returns `false`, so call `Add` on its own to change the label.
- `Used` fires after the player uses any object, including prompts made with `Add`. `Guid` is the object's save id or `null`. `Distance` is measured from the camera to the object's centre. If another mod stops the game's own use step, `Used` can still fire. <span class="tech">(It is raised after `InteractObjects.UseInteractable` called `Interact()` on the target. `Kind` is the component's class name. The postfix also runs after a skipped original, provided the framework's prefix ran first, and the prefix order was not checked.)</span>
- `Looking()` reads what the crosshair hits within the game's reach of 3 metres. `Kind` is `null` when the object has nothing the player can use. `Distance` goes to the object's centre, not to the spot the crosshair hits.

## Limits

- `Add` returns `false` when it can't add the prompt. That happens when the object has no collider (the shape that the crosshair hits), when it already has a prompt from the game (the game's prompt would win), when another mod already added a prompt to it, while the game is loading or saving a save, or when the framework can't change the game. In the case of another mod, the framework writes one warning that names both mods.
- `Add` throws an error for missing inputs. A null object, label or `use` action throws `ArgumentNullException`, and an empty mod id throws `ArgumentException`. These checks come before the check for a save loading.
- You can't cancel a use from `Used`, and you can't make an object hold-to-use. The handlers' results are not collected, so there is no clean rule for skipping a use. No mod has asked for either.
