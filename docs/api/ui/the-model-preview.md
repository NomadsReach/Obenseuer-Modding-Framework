---
title: The model preview
parent: UI
grand_parent: Screen and UI
nav_order: 5
---

# The model preview

A model preview draws a 3D model, such as the player, inside a window. The player can drag to turn it and use the wheel to zoom.

```csharp
public sealed class ModelPreview : MonoBehaviour
{
    public PreviewFraming Framing { get; set; }      // whole body or face; changing it reframes at once
    public bool ShowFramingSwitch { get; set; }      // shows or hides the Body and Face switch; true by default
    public RenderTexture Texture { get; }            // the picture the preview draws into
    public int Renderers { get; }                    // how many parts of the model the preview is drawing
    public void Refresh();                           // picks up parts that appeared since the last call, and reframes
    public int Dispose();                            // puts everything back and gives how many parts it had
}
```

- **A preview draws the model's parts on a special layer while its window is open.** This includes the player's model, which is switched on for the window and switched back off when the window closes. Only parts that are switched on are used.
  - Technical: it draws the renderers on layer 31. The player's renderers are those of `PlayerLocator.playerRig` and `playerRigUnrigified`. Only renderers on active GameObjects are claimed.
- **Only one preview can be open at a time.** A second `ModUi.ModelPreview` makes the window it is in fault and close.
- **`ShowFramingSwitch` shows or hides the Body and Face switch in the top right corner of the preview.**
- **Call `Refresh()` after the model changes.** For example after a mesh swap, it picks up parts that were switched on since the last call.
- **The preview's camera and lights are turned off while the tab that holds the preview is hidden.**
- **`Dispose()` puts back every change the preview made, and it is safe to call twice.** It then shows or hides the player model to match the view the game is in now, first person or third person. It gives how many parts it had recorded. A window disposes its previews when it closes.
  - Technical: a skinned mesh's bounds are restored only when its shared mesh is the one that was recorded.
- **The window logs one line starting with `model preview:` when the preview first settles.** It tells the counts of parts recorded, active, visible and in the rig, and the first ten active parts that are not visible. Use it to find out why a part does not show.
