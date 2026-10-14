using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public static class ComputerDesk
    {
        internal const string Owner = "obenseuer.moddingframework.computerdesk";
        private static readonly FieldInfo screenField = AccessTools.Field(typeof(InteractableComputer), "computerScreen");

        public static bool SetWallpaper(string modId, string computerId, Sprite wallpaper)
        {
            if (!ValidMod(modId) || string.IsNullOrEmpty(computerId) || wallpaper == null || screenField == null) return false;
            try
            {
                InteractableComputer computer = ComputerContents.FindComputer(computerId);
                ComputerScreen screen = computer == null ? null : screenField.GetValue(computer) as ComputerScreen;
                if (screen == null) return false;
                screen.ChangeDesktopWallpaper(wallpaper);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetWallpaper", e);
                return false;
            }
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
