using ObenseuerCore;

namespace $safeprojectname$
{
    [ModInfo("example.$safeprojectname$", "$safeprojectname$", "0.1.0")]
    [ModDependency("obenseuer.moddingframework", "0.11.0")]
    public class $safeprojectname$ : FrameworkMod
    {
        protected override void OnLoad()
        {
            Log.Info("loaded");
            ModSetting<float> speed = Config.Bind("General", "Speed", 1f, "Walk speed factor.", 0.5f, 2f);
            Events.WhenReady(() => Hud.ShowHint(Mod.Id, "$safeprojectname$", new[] { "Hello from $safeprojectname$." }));
        }
    }
}
