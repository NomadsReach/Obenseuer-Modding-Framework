using System;

namespace ObenseuerCore.Ui
{
    internal static class ToolkitPinnedBounds
    {
        internal static bool Project(float[] x, float[] y, float[] z, float screenHeight, out float left, out float top, out float width, out float height)
        {
            left = top = width = height = 0;
            if (x == null || y == null || z == null || x.Length < 4 || y.Length < 4 || z.Length < 4 || !ToolkitWindowBounds.ValidSize(screenHeight)) return false;
            for (int i = 0; i < 4; i++)
                if (!ToolkitWindowBounds.Finite(x[i]) || !ToolkitWindowBounds.Finite(y[i]) || !ToolkitWindowBounds.ValidSize(z[i])) return false;
            left = Math.Max(x[0], x[1]);
            float right = Math.Min(x[2], x[3]);
            float bottom = Math.Max(y[0], y[3]);
            float upper = Math.Min(y[1], y[2]);
            top = screenHeight - upper;
            width = right - left; height = upper - bottom;
            return ToolkitWindowBounds.Finite(top) && ToolkitWindowBounds.Finite(width) && ToolkitWindowBounds.Finite(height) && width >= 64 && height >= 64;
        }
    }
}
