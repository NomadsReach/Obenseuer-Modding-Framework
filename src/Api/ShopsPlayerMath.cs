using System;

namespace ObenseuerCore
{
    internal static class ShopsPlayerMath
    {
        internal const float DefaultMarkup = 1.2f;

        internal static float Factor(float gameFactor)
        {
            return gameFactor == 0f ? DefaultMarkup : gameFactor;
        }

        internal static int Price(int saleValue, float factor, bool hasOverride, int overrideValue)
        {
            if (hasOverride) return overrideValue < 0 ? 0 : overrideValue;
            double value = Math.Round((double)((float)saleValue * factor));
            if (double.IsNaN(value) || value <= 0d) return 0;
            if (value >= int.MaxValue) return int.MaxValue;
            return (int)value;
        }

        internal static bool CanTake(int slotCount, int slot, int stackAmount, int count)
        {
            return slot >= 0 && slot < slotCount && count > 0 && stackAmount >= count;
        }

        internal static bool CanSell(int heldItemId, int expectedItemId, float unitPrice)
        {
            return heldItemId == expectedItemId && unitPrice > 0f;
        }

        internal static bool CanDebit(float till, float value)
        {
            return value > 0f && till >= value;
        }

        internal static bool AreaFlag(bool converted, bool flag)
        {
            return flag && !converted;
        }
    }
}
