using System;

namespace ObenseuerCore
{
    internal enum TakeResult
    {
        Exact,
        Short,
        Over,
        Nothing
    }

    public static partial class PlayerInventory
    {
        internal const string LiquidCategory = "Liquid";

        internal static TakeResult Account(int requested, int before, int after, out int removed)
        {
            removed = before - after;
            if (removed <= 0) return TakeResult.Nothing;
            if (removed == requested) return TakeResult.Exact;
            return removed > requested ? TakeResult.Over : TakeResult.Short;
        }

        internal static bool LiquidRisk(bool itemIsLiquid, bool slotMatchesLiquidId)
        {
            return itemIsLiquid || slotMatchesLiquidId;
        }

        internal static bool IsLiquidCategory(string[] categories)
        {
            return categories != null && Array.IndexOf(categories, LiquidCategory) >= 0;
        }
    }
}
