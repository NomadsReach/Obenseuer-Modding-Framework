using System;

namespace ObenseuerCore.Ui
{
    internal static class ToolkitWindowBounds
    {
        internal static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        internal static bool ValidSize(float value) { return Finite(value) && value > 0f; }
        internal static float Resize(float size, float delta, float minimum) { return Math.Max(minimum, size + delta); }

        internal static float X(float value, float windowWidth, float parentWidth)
        {
            return Math.Min(Math.Max(value, 48f - windowWidth), parentWidth - 48f);
        }

        internal static float Y(float value, float parentHeight)
        {
            return Math.Min(Math.Max(value, 0f), Math.Max(0f, parentHeight - 30f));
        }
    }
}
