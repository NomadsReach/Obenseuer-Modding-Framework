using System;

namespace ObenseuerCore
{
    internal static class InventorySelfTest
    {
        internal const string Ok = "inventory take self-check ok";

        internal static string Run()
        {
            try
            {
                int removed;
                if (PlayerInventory.Account(3, 10, 7, out removed) != TakeResult.Exact || removed != 3) return "an exact take was not exact";
                if (PlayerInventory.Account(3, 10, 8, out removed) != TakeResult.Short || removed != 2) return "a short take was not short";
                if (PlayerInventory.Account(3, 10, 10, out removed) != TakeResult.Nothing || removed != 0) return "a take that removed nothing was not nothing";
                if (PlayerInventory.Account(3, 10, 6, out removed) != TakeResult.Over || removed != 4) return "a take that removed more than asked was not over";
                if (PlayerInventory.Account(3, 10, 12, out removed) != TakeResult.Nothing || removed != -2) return "a take that gained units was not nothing";
                if (PlayerInventory.Account(1, 1, 0, out removed) != TakeResult.Exact || removed != 1) return "a single exact take was not exact";
                if (PlayerInventory.IsLiquidCategory(null)) return "a null category list was read as a liquid";
                if (PlayerInventory.IsLiquidCategory(new string[0])) return "an empty category list was read as a liquid";
                if (PlayerInventory.IsLiquidCategory(new string[] { "Food" })) return "a non liquid category was read as a liquid";
                if (!PlayerInventory.IsLiquidCategory(new string[] { "Liquid" })) return "a liquid category was not read as a liquid";
                if (!PlayerInventory.IsLiquidCategory(new string[] { "Food", "Liquid", "Drink" })) return "a liquid among other categories was not read as a liquid";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return Ok;
        }
    }
}
