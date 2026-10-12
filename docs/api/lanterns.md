# Lanterns API

Target: Obenseuer 0.5.05 (buildid 25839246), Unity 6000.0.77f1 Mono, BepInEx 5.4.23.5.
Namespace: `ObenseuerCore`. Patch owner: `obenseuer.moddingframework.lanterns`.
Files: `src/Api/Lanterns.cs`, `src/Api/LanternsPatches.cs`.

A lantern is a placed scene object (`Lantern`, an `Interactable`), not an inventory item. Its candle is a candle item from the inventory, which the player inserts. This area reads the lantern's candle timer and reports when a candle burns out. The candle items themselves are the inventory's (`docs/api/inventory.md`).

## Wiring

```csharp
internal static void Lanterns.Init();
```

Core calls this from `Core.Awake`.

`Init` installs one hook, `Lantern.CandleExtinguished`. If it is missing, the owner is disabled and one warning is logged to `BepInEx\logs\obenseuer.moddingframework.lanterns.log`. The reads keep working; `CandleBurntOut` stops.

## Threading and safety

- Main thread only.
- `Lanterns.IsAvailable` is false when the hook was not installed or the owner was disabled.
- Every member is safe at any time. With no save loaded, or on the main menu, reads return an empty list or `null`.
- Read-only. Nothing throws, and no argument throws. A null or empty `id` finds nothing.
- `CandleBurntOut` is raised synchronously after the game has finished the extinguish. A handler that throws is logged and the other handlers still run (`Events.Raise`).

## API

```csharp
public static class Lanterns
{
    public static List<LanternInfo> All();
    public static LanternInfo Get(string id);

    public static bool IsAvailable { get; }
    public static event Action<CandleBurntOutArgs> CandleBurntOut;
}

public sealed class LanternInfo
{
    public string Id { get; }              // SceneIds.Of: the save GUID, else the scene path
    public Vector3 Position { get; }       // world position
    public bool Lit { get; }               // a candle is burning (Lantern.candleTimer.remainingInSeconds > 0)
    public int RemainingSeconds { get; }   // seconds of candle left, 0 when not lit
}

public sealed class CandleBurntOutArgs
{
    public string Id { get; }              // the lantern's id, as LanternInfo.Id
    public Vector3 Position { get; }       // the lantern's world position
}
```

- `All` lists every `Lantern` in the scene.
- `Get(id)` returns one `LanternInfo`, or `null` when no lantern has that id.
- `RemainingSeconds` is a snapshot. It counts down while the game runs, so read it again for the current value.
- `CandleBurntOut` fires when the candle's timer runs out. It does not fire when the player puts the candle out by hand, or when another script calls `Lantern.CandleExtinguished` while the candle still has time left. The payload is the lantern's id and position, read after the game has put the candle out. After a save loads, every lantern whose candle ran out while saved fires once on the first timer tick.

## Not wrapped

- Lighting a lantern (`Lantern.CandleInserted`) and putting it out by hand (`Lantern.CandleExtinguished` through `Interact`). `CandleInserted` does not take the candle from the inventory; `Interact` does (`Items/Lantern.cs:94`). A call from a mod would give a free candle, so no write is exposed.
- The candle's burn time, which comes from the candle item's fuel data (`Items/Lantern.cs:128`) unless `overrideCandleTime` is set.
- The candle item itself. It is an inventory item and is read through `docs/api/inventory.md`.
