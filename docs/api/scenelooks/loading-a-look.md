---
title: Loading a look
parent: Scene looks
grand_parent: Places and travel
nav_order: 5
---

# Loading a look

`Load` reads one look fully from the game's files. It gives you a `SceneLook` handle, which is what `Apply` and `Reskin` take.

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneLook</code> <span>a handle to one loaded look</span></p>
<div class="api-pair" markdown="1">

```csharp
public sealed class SceneLook
{
    public string Scene { get; }
    public string Path { get; }
    public string Name { get; }
    public bool Ready { get; }
    public bool Failed { get; }
    public string Reason { get; }
    public int MeshCount { get; }
    public int VertexCount { get; }
    public string[] BoneNames { get; }
}
```

| Property | Type | What it means |
|---|---|---|
| `Scene` | `string` | The name of the scene the look came from. |
| `Path` | `string` | The full path of the look's body root. |
| `Name` | `string` | The name of the body root. |
| `Ready` | `bool` | `true` when the look is loaded and usable. |
| `Failed` | `bool` | `true` when the load was refused. See `Reason`. |
| `Reason` | `string` | Why it failed. It is `"released"` after `Release`. |
| `MeshCount` | `int` | How many meshes the look has. |
| `VertexCount` | `int` | How many vertices (model points) the look has. |
| `BoneNames` | `string[]` | The names of its bones. |

</div>
</div>

<div class="api-type" markdown="1">
<p class="api-type-head"><code>SceneLooks</code> <span>loads a look</span></p>
<div class="api-pair" markdown="1">

```csharp
public static class SceneLooks
{
    public static SceneLook Load(string modId, SceneLookInfo info);
    public static SceneLook Load(string modId, string sceneName, string rendererRoot);
}
```

| Method | Returns | What it does |
|---|---|---|
| `Load(string modId, SceneLookInfo info)` | `SceneLook` | Loads a look you found with `List`, `FindByName` or `FindByCharacter`. |
| `Load(string modId, string sceneName, string rendererRoot)` | `SceneLook` | Loads a look by its scene and path or name. |

</div>
</div>

## Things to know

- `Load(modId, sceneName, rendererRoot)` reads one look completely. It reads every skinned mesh, material, texture, bone and node under the body root. `rendererRoot` is either the full path (`Content/mr_duck/Wasteland Hide/mr_duck`) or a name that only one entry uses (`mr_duck`).
- `Load(modId, info)` loads the look that a `SceneLookInfo` names. It is the same load, with the same cache, as the other version. It refuses a `SceneLookInfo` that the framework did not build.
- `Load` always gives back a handle, and the handle is never `null`. On a failure, `Failed` is `true` and `Reason` says why. `Ready` is `true` only for a look that loaded.
- Loading the same look twice is quick, because it is kept for the session. A second `Load` of the same body root gives back the same handle and does not read the files again. Every `Load` takes a hold on the look for your mod, even when the look was already kept, so the handle can always be released. <span class="tech">(one parsed look is cached for each level file and root for the session)</span>

## Limits

- The whole look is refused if any part of it fails, so you never get half a body. That happens when a model fails its checks, a bone is outside the look's file, the root bone cannot be found, a file it needs is not installed, or a texture does not fit its file. <span class="tech">(the checks are a renderer failing its layout checks, a bone outside the look file, an unresolved root bone, a referenced file that is not installed, and a texture stream that does not fit its file)</span>
- A look uses memory, so keep one look for the whole session and release it when you stop using it. As a guide, the Mr. Duck look is 1.56 MB of vertex data, 0.17 MB of index data and 8.45 MB of texture data.
