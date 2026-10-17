---
title: How a clip looks on the NPC
parent: NPC animation
grand_parent: People
nav_order: 4
---

# How a clip looks on the NPC

## Things to know

- Your clip has to match the NPC's skeleton. Use a humanoid clip on a humanoid rig, or a generic clip whose bone names exist on the NPC's model. A clip that does not match plays nothing on the bones it cannot find.
- The NPC's own animation keeps running under your clip. Your clip is mixed in over the NPC's current pose, by how strongly it is faded in. At half strength you see half of your clip and half of the NPC's pose, and not a full replacement. <span class="tech">(The clip is a playable graph whose output weight is the fade. The graph has no copy of the NPC's controller, so the controller keeps running underneath.)</span>
- A clip with root motion can move the NPC. Root motion is movement that is recorded in the clip. It moves the NPC when the NPC's `Animator` has `applyRootMotion` on. Leave it off for a clip that should stay in place.
- Hiding, killing, or loading ends a clip. When the NPC's object is switched off or the NPC dies, its clip ends at once. It does not come back when the NPC returns, so play it again when you need it. A scene load that replaces the whole scene ends every clip too. <span class="tech">(The graph is destroyed. This is a `Single` scene load; additive loads do not end clips.)</span>
- Clips are not saved. Play them again after a load, for example from `Events.WhenReady`. A `Play` called directly from an `Events.SceneLoaded` handler gives `false`, because the game still counts as loading during that whole event. <span class="tech">(`SaveGate.Busy` is set for the whole `SceneLoaded` event.)</span>
