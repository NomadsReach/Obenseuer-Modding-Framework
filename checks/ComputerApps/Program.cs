using System;
using ObenseuerCore;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class Program
{
    private static void Check(bool value) { if (!value) throw new Exception("Computer app lifecycle check failed"); }
    private static RectTransform Target(string name) { return (RectTransform)new GameObject(name, typeof(RectTransform)).transform; }
    private static void Main()
    {
        ComputerApps.Init();
        RectTransform target = Target("screen"), nextTarget = Target("next");
        bool allow = false, nested = true;
        int opened = 0, closed = 0;
        ComputerApps.AppClosed += args => { throw new InvalidOperationException(); };
        ComputerApps.AppClosed += args => { if (args.AppId == "one") closed++; };
        Check(ComputerApps.Register("mod", "one", "One", null, host =>
        {
            opened++;
            Check(ComputerApps.PinnedScreen == target);
            ComputerApps.SetCloseGuard(host, () => { nested = ComputerApps.RequestClose("one"); return allow; });
        }));
        CoreInput.Busy = true;
        Check(!ComputerApps.OpenPinned("mod", "one", target) && opened == 0 && UiEvents.Users == 0);
        CoreInput.Busy = false;
        Check(ComputerApps.OpenPinned("mod", "one", target) && opened == 1 && UiEvents.Users == 1);
        Check(ComputerApps.OpenPinned("mod", "one", target) && opened == 1);
        Check(!ComputerApps.ClosePinned("one", null) && !ComputerApps.ClosePinned("one", nextTarget) && !ComputerApps.ClosePinned("two", target));
        Check(ComputerApps.PinnedScreen == target && ComputerAppsDesk.HideCalls == 0);
        Check(!ComputerApps.RequestClose("one") && !nested && closed == 0 && UiEvents.Users == 1);
        Check(!ComputerApps.OpenPinned("mod", "one", nextTarget) && ComputerApps.PinnedScreen == target);
        Check(ComputerApps.ClosePinned("one", target) && ComputerApps.PinnedScreen == null && ComputerAppsDesk.HideCalls == 0 && closed == 1 && UiEvents.Users == 0);
        Check(!ComputerApps.ClosePinned("one", target));
        Check(ComputerApps.OpenPinned("mod", "one", target));
        allow = true;
        Check(ComputerApps.RequestClose("one") && !nested && closed == 2 && UiEvents.Users == 0 && ComputerApps.PinnedScreen == null);
        Check(ComputerApps.OpenPinned("mod", "one", target));
        target.gameObject.SetActive(false); ComputerApps.Tick();
        Check(closed == 3 && UiEvents.Users == 0 && ComputerApps.PinnedScreen == null);
        Check(!ComputerApps.OpenPinned("mod", "one", target));
        target.gameObject.SetActive(true);
        Check(ComputerApps.OpenPinned("mod", "one", target));
        Events.Scene("another", LoadSceneMode.Additive); Check(closed == 3 && UiEvents.Users == 1);
        Events.Scene("menu", LoadSceneMode.Single); Check(closed == 4 && UiEvents.Users == 0);
        Check(ComputerApps.Register("mod", "two", "Two", null, host => ComputerApps.SetCloseGuard(host, () => false)));
        Check(ComputerApps.OpenPinned("mod", "one", target));
        Action<ComputerAppClosedArgs> replace = args => { if (args.AppId == "one") Check(ComputerApps.OpenPinned("mod", "two", nextTarget)); };
        ComputerApps.AppClosed += replace;
        Check(!ComputerApps.OpenPinned("mod", "one", nextTarget) && ComputerApps.PinnedScreen == nextTarget);
        Check(!ComputerApps.RequestClose("two") && UiEvents.Users == 1);
        ComputerApps.AppClosed -= replace; ComputerApps.Close("two");
        Check(UiEvents.Users == 0);
        Check(ComputerApps.Register("mod", "bad", "Bad", null, host => { throw new InvalidOperationException(); }));
        Check(!ComputerApps.OpenPinned("mod", "bad", target) && UiEvents.Users == 0 && ComputerApps.PinnedScreen == null);
        Console.WriteLine("Computer app lifecycle checks passed");
    }
}
