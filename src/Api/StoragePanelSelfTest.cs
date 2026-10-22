using System;

namespace ObenseuerCore
{
    internal static class StoragePanelSelfTest
    {
        internal static string Run()
        {
            try
            {
                if (StoragePanel.FirstFree(new bool[0]) != -1) return "an empty list reports a free slot";
                if (StoragePanel.FirstFree(new[] { false, true }) != 0) return "the first empty slot is not 0";
                if (StoragePanel.FirstFree(new[] { true, false, true }) != 1) return "the first free slot among taken slots is not 1";
                if (StoragePanel.FirstFree(new[] { true, true }) != -1) return "a full list reports a free slot";
                if (StoragePanel.AddButton(null, "Store", p => { })) return "a button without a mod id was added";
                if (StoragePanel.AddButton("selfcheck.storage", "", p => { })) return "a button without a label was added";
                if (StoragePanel.AddButton("selfcheck.storage", "Store", null)) return "a button without a function was added";
                if (StoragePanel.RemoveButton("selfcheck.storage")) return "a button that was never added was removed";
                if (StoragePanel.FreeSlot(null) != -1) return "a null container reports a free slot";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "storage panel self-check ok";
        }
    }
}
