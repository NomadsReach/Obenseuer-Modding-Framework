---
title: The model preview
parent: UI
grand_parent: Screen and UI
nav_order: 5
---

# The model preview

This page uses `Transform`, `Mesh` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

A model preview draws a 3D model, such as the player, inside a window. The player can drag to turn it and use the wheel to zoom.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>ModelPreview</code> <span>a 3D view of a model</span></p>
[`ModUi.ModelPreview`](controls.md) builds one of these for a transform you pass in, with the framing you choose.

<div class="api-pair" markdown="1">

```csharp
public sealed class ModelPreview : MonoBehaviour
{
    public PreviewFraming Framing { get; set; }
    public bool ShowFramingSwitch { get; set; }
    public RenderTexture Texture { get; }
    public int Renderers { get; }
    public void Refresh();
    public int Dispose();
}
```

| Method | Returns | What it does |
|---|---|---|
| `Refresh()` | `void` | Picks up parts that appeared since the last call, and reframes. |
| `Dispose()` | `int` | Puts everything back and gives how many parts it had recorded. It is safe to call twice. |

| Property | Type | What it means |
|---|---|---|
| `Framing` | `PreviewFraming` | Whole body or face. Changing it reframes at once. |
| `ShowFramingSwitch` | `bool` | Shows or hides the Body and Face switch in the top right corner. It is `true` by default. |
| `Texture` | `RenderTexture` | The picture the preview draws into. |
| `Renderers` | `int` | How many parts of the model the preview is drawing. |

</div>
</div>

## Things to know

- A preview draws the model's parts on a special layer while its window is open. This includes the player's model, which is switched on for the window and switched back off when the window closes. Only parts that are switched on are used. <span class="tech">(It draws the renderers on layer 31. The player's renderers are those of `PlayerLocator.playerRig` and `playerRigUnrigified`. Only renderers on active GameObjects are claimed.)</span>
- Only one preview can be open at a time. A second `ModUi.ModelPreview` makes the window it is in fault and close.
- Call `Refresh()` after the model changes. For example after a mesh swap, it picks up parts that were switched on since the last call.
- The preview's camera and lights are turned off while the tab that holds the preview is hidden.
- `Dispose()` puts back every change the preview made. It then shows or hides the player model to match the view the game is in now, first person or third person. A window disposes its previews when it closes. <span class="tech">(A skinned mesh's bounds are restored only when its shared mesh is the one that was recorded.)</span>
- The window logs one line starting with `model preview:` when the preview first settles. It tells the counts of parts recorded, active, visible and in the rig, and the first ten active parts that are not visible. Use it to find out why a part does not show.
