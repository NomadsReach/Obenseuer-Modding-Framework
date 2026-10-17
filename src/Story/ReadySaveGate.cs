namespace ObenseuerCore
{
    internal enum SessionKind
    {
        SaveLoad,
        LevelChange,
        NewGame
    }

    internal sealed class ReadySaveGate
    {
        internal bool ShouldArm(SessionKind kind)
        {
            return kind != SessionKind.LevelChange;
        }
    }
}
