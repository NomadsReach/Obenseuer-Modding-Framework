---
title: Save data
parent: Getting started
nav_order: 6
---

# Save data

Save data is kept with each save game. Register it once in `Awake`.

```csharp
using System;
using BepInEx;
using BepInEx.Unity.Mono;
using ObenseuerCore;

[Serializable]
public class HelloState
{
    public int daysSeen;
}

[BepInPlugin("example.hello", "Example Hello", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
public class ExampleHello : BaseUnityPlugin
{
    private HelloState state = new HelloState();

    private void Awake()
    {
        SaveData.Register<HelloState>("example.hello", () => state, s => state = s ?? new HelloState());
        Events.DayPassed += day =>
        {
            if (SaveData.Busy) return;
            state.daysSeen++;
        };
    }
}
```

On BepInEx 5, write `"0.10.0"` instead, because BepInEx 5 cannot parse a version range, and drop `using BepInEx.Unity.Mono;` (`BaseUnityPlugin` then comes from `BepInEx`). See [Supporting BepInEx 5 too](packaging.md#supporting-bepinex-5-too).

- The data class needs a parameterless constructor. Public fields and `[SerializeField]` fields are saved. Properties are not.
- Lists, arrays, dictionaries, `Vector2`, `Vector3`, `Vector4`, `Quaternion` and `Color` work. Polymorphic types and top-level arrays do not.
- The data is one file, `<mod id>.json`, in the `mods` folder of the save.
- On a save, the framework calls the first function, which returns your data. When a load finishes, it calls the second one with the data it read.
- The loaded value is `null` when the file is missing or unreadable, so write `s ?? new HelloState()`.
- Do not change game state while `SaveData.Busy` is `true`. It is `true` during a load and in the frame a save starts.
- A second `Register` with the same id throws `InvalidOperationException`. Register once.

Next: [Patching](patching.md).
