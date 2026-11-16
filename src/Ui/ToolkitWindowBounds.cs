using System;

namespace ObenseuerCore.Ui
{
    internal static class ToolkitWindowBounds
    {
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
