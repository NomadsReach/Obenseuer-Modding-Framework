using System;
using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;

[Serializable]
public class HelloState
{
    public int daysSeen;
}

[BepInPlugin("example.hellocore", "HelloCore", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.11.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
#endif
public class HelloCore : BaseUnityPlugin
{
    private static readonly CoreLog Log = CoreLog.For("HelloCore");
    private HelloState state = new HelloState();

    private void Awake()
    {
        SaveData.Register<HelloState>("example.hellocore", () => state, s => state = s ?? new HelloState());
        Events.HourPassed += hour => Log.Info("hour " + hour);
        Events.DayPassed += day =>
        {
            state.daysSeen++;
            Log.Info("day " + day + ", days seen " + state.daysSeen);
        };
        Events.Loaded += () => Log.Info("loaded, Wallet.Balance " + Wallet.Balance);
    }
}
