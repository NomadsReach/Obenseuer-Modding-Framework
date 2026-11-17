using System;
using ObenseuerCore.Ui;

internal static class Program
{
    private static void Main()
    {
        if (ToolkitWindowBounds.X(100, 600, 1280) != 100) throw new Exception("position changed inside bounds");
        if (ToolkitWindowBounds.X(-900, 600, 1280) != -552) throw new Exception("left title-bar limit");
        if (ToolkitWindowBounds.X(2000, 600, 1280) != 1232) throw new Exception("right title-bar limit");
        if (ToolkitWindowBounds.Y(-20, 720) != 0) throw new Exception("top limit");
        if (ToolkitWindowBounds.Y(900, 720) != 690) throw new Exception("bottom title-bar limit");
        if (ToolkitWindowBounds.Y(10, 10) != 0) throw new Exception("small parent");
        if (ToolkitWindowBounds.Resize(600, -900, 360) != 360 || ToolkitWindowBounds.Resize(450, 50, 240) != 500) throw new Exception("resize bounds");
        if (!ToolkitWindowBounds.ValidSize(360) || ToolkitWindowBounds.ValidSize(0) || ToolkitWindowBounds.ValidSize(float.NaN) || ToolkitWindowBounds.ValidSize(float.PositiveInfinity)) throw new Exception("resize minimum");
    }
}
