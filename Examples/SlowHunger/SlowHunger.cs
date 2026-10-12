using BepInEx;
using ObenseuerCore;

[BepInPlugin("example.slowhunger", "SlowHunger", "0.1.0")]
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
public class SlowHunger : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("SlowHunger");

    private void Awake()
    {
        Survival.Decay.Set(Stat.Hunger, "example.slowhunger", 0.5f, 0f);
        Survival.OnCrossed("example.slowhunger", Stat.Hunger, 80f, (stat, value, rising) =>
            Log.Info(stat + " crossed 80, rising " + rising + ", value " + value));
        Survival.PlayerDied += reason => Log.Info("player died, reason " + reason);
    }
}
