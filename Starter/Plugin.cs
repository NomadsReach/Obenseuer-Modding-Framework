using ObenseuerCore;

[ModInfo("example.myfirstmod", "My First Mod", "0.1.0")]
[ModDependency("obenseuer.moddingframework", "0.11.0")]
public class MyFirstMod : FrameworkMod
{
    protected override void OnLoad()
    {
        Log.Info("loaded");
        ModSetting<float> speed = Config.Bind("General", "Speed", 1f, "Walk speed factor.", 0.5f, 2f);
        Events.WhenReady(() => Hud.ShowHint(Mod.Id, "My First Mod", new[] { "Hello from My First Mod." }));
    }
}
