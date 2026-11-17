using System;
using System.Collections.Generic;
using ObenseuerCore;

internal sealed class HostCapture
{
    internal object Host, Guard;
}

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
        DeskCloseGate<object> desk = new DeskCloseGate<object>();
        object first = new object(), second = new object();
        List<object> hosts = new List<object> { first, second };
        Func<List<object>> snapshot = () => new List<object>(hosts);
        bool finished = false, nestedDesk = true;
        Func<object, object, bool> same = (a, b) => object.ReferenceEquals(a, b);
        if (desk.Request(snapshot, item => item != second, same, () => { finished = true; return true; }) || finished) throw new Exception("desk veto");
        if (!desk.Request(snapshot, item => true, same, () => { nestedDesk = desk.Request(snapshot, item => true, same, () => true); finished = true; return true; }) || nestedDesk || !finished) throw new Exception("desk finish reentry");
        finished = false;
        if (desk.Request(snapshot, item => { hosts.Add(new object()); return true; }, same, () => { finished = true; return true; }) || finished) throw new Exception("desk host mutation");
        if (!desk.Request(snapshot, item => true, same, () => true)) throw new Exception("desk gate did not reset");
        try { desk.Request(snapshot, item => { throw new InvalidOperationException(); }, same, () => true); throw new Exception("desk guard exception ignored"); }
        catch (InvalidOperationException) { }
        if (desk.Request(snapshot, item => true, same, () => false) || !desk.Request(snapshot, item => true, same, () => true)) throw new Exception("desk finish or exception recovery");
        DeskCloseGate<HostCapture> guardChanges = new DeskCloseGate<HostCapture>();
        object guard = new object();
        Func<List<HostCapture>> capture = () => new List<HostCapture> { new HostCapture { Host = first, Guard = guard } };
        if (guardChanges.Request(capture, item => { guard = new object(); return true; }, (a, b) => ReferenceEquals(a.Host, b.Host) && ReferenceEquals(a.Guard, b.Guard), () => true)) throw new Exception("desk guard replacement");
    }
}
