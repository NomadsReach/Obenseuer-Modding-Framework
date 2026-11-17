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
        float left, top, width, height;
        float[] x = { 100, 110, 610, 620 }, y = { 100, 410, 400, 105 }, z = { 1, 1, 1, 1 };
        if (!ToolkitPinnedBounds.Project(x, y, z, 600, out left, out top, out width, out height) || left != 110 || top != 200 || width != 500 || height != 295) throw new Exception("screen projection");
        z[2] = 0;
        if (ToolkitPinnedBounds.Project(x, y, z, 600, out left, out top, out width, out height)) throw new Exception("behind camera");
        z[2] = 1; x[1] = float.NaN;
        if (ToolkitPinnedBounds.Project(x, y, z, 600, out left, out top, out width, out height)) throw new Exception("invalid projection");
        x[1] = 600;
        if (ToolkitPinnedBounds.Project(x, y, z, 600, out left, out top, out width, out height)) throw new Exception("screen too small");
    }
}
