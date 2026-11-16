using System;
using ObenseuerCore;

internal static class Program
{
    private static void Main()
    {
        CloseRequestGate gate = new CloseRequestGate();
        if (!gate.Check(() => true) || gate.Check(() => false)) throw new Exception("guard decision");
        bool nested = true;
        if (!gate.Check(delegate { nested = gate.Check(() => true); return true; }) || nested) throw new Exception("nested close");
        try { gate.Check(delegate { throw new InvalidOperationException(); }); }
        catch (InvalidOperationException) { }
        if (!gate.Check(() => true)) throw new Exception("gate did not reset after exception");
    }
}
