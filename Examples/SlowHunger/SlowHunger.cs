using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;

[BepInPlugin("example.slowhunger", "SlowHunger", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.10.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
#endif
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
