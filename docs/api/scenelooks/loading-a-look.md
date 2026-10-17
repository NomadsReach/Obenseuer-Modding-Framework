---
title: Loading a look
parent: Scene looks
grand_parent: Places and travel
nav_order: 5
---

# Loading a look

`Load` reads one look fully from the game's files. It gives you a `SceneLook` handle, which is what `Apply` and `Reskin` take.

```csharp
public sealed class SceneLook    // a handle to one loaded look
{
    public string Scene { get; }          // the name of the scene the look came from
    public string Path { get; }           // the full path of the look's body root
    public string Name { get; }           // the name of the body root
    public bool Ready { get; }            // true when the look is loaded and usable
    public bool Failed { get; }           // true when the load was refused; see Reason
    public string Reason { get; }         // why it failed; "released" after Release
    public int MeshCount { get; }         // how many meshes the look has
    public int VertexCount { get; }       // how many vertices (model points) the look has
    public string[] BoneNames { get; }    // the names of its bones
}

public static class SceneLooks
{
    public static SceneLook Load(string modId, SceneLookInfo info);                          // loads a look you found with List or Find
    public static SceneLook Load(string modId, string sceneName, string rendererRoot);       // loads a look by its scene and path or name
}
```

- **`Load(modId, sceneName, rendererRoot)` reads one look completely.** It reads every skinned mesh, material, texture, bone and node under the body root. `rendererRoot` is either the full path (`Content/mr_duck/Wasteland Hide/mr_duck`) or a name that only one entry uses (`mr_duck`).
- **`Load(modId, info)` loads the look that a `SceneLookInfo` names.** It is the same load, with the same cache, as the other version. It refuses a `SceneLookInfo` that the framework did not build.
- **`Load` always gives back a handle, and the handle is never `null`.** On a failure, `Failed` is `true` and `Reason` says why. `Ready` is `true` only for a look that loaded.
- **Loading the same look twice is quick, because it is kept for the session.** A second `Load` of the same body root gives back the same handle and does not read the files again. Every `Load` takes a hold on the look for your mod, even when the look was already kept, so the handle can always be released.
  - Technical: one parsed look is cached for each level file and root for the session.
- **The whole look is refused if any part of it fails, so you never get half a body.** That happens when a model fails its checks, a bone is outside the look's file, the root bone cannot be found, a file it needs is not installed, or a texture does not fit its file.
  - Technical: the checks are a renderer failing its layout checks, a bone outside the look file, an unresolved root bone, a referenced file that is not installed, and a texture stream that does not fit its file.
- **A look uses memory, so keep one look for the whole session and release it when you stop using it.** As a guide, the Mr. Duck look is 1.56 MB of vertex data, 0.17 MB of index data and 8.45 MB of texture data.
