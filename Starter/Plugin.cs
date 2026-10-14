using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;

[BepInPlugin("example.myfirstmod", "My First Mod", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.10.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
#endif
public class MyFirstMod : BaseUnityPlugin
{
    private void Awake()
    {
        var mod = CoreMod.For(this);
        mod.Log.Info("loaded");
        Events.WhenReady(() => Hud.ShowHint(mod.Id, "My First Mod", new[] { "Hello from My First Mod." }));
    }
}
