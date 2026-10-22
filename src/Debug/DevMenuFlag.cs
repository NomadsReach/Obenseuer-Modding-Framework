using System;

namespace ObenseuerCore
{
    [Serializable]
    internal sealed class DevMenuFlagData
    {
        public bool Modified;
    }

    internal static class DevMenuFlag
    {
        private const string SlotKey = "devmodified";
        private static SaveSlot<DevMenuFlagData> slot;
        private static bool announced;

        internal static bool Modified
        {
            get
            {
                if (slot == null || !slot.Value.Modified) return false;
                Announce();
                return true;
            }
        }

        internal static void Init()
        {
            try
            {
                slot = SaveData.Bind<DevMenuFlagData>(ModIds.CoreId, SlotKey);
            }
            catch (Exception e)
            {
                CoreLog.For(DevMenu.Owner).Error("the dev-modified flag could not be bound to the save, the menu still works and no save is flagged: " + e.ToString());
            }
        }

        internal static void Mark(bool succeeded)
        {
            if (slot == null || !ShouldMark(succeeded, slot.Value.Modified)) return;
            slot.Value.Modified = true;
            DevMenuWindow.Refresh();
            Announce();
        }

        internal static bool ShouldMark(bool succeeded, bool alreadyMarked)
        {
            return succeeded && !alreadyMarked;
        }

        private static void Announce()
        {
            if (announced) return;
            announced = true;
            CoreLog.For(DevMenu.Owner).Warn("this save is dev-modified, the Developer menu has changed its state, so a bug report from it should say so");
        }
    }
}
