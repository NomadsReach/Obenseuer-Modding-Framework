using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public static class Travel
    {
        public static bool UsingMinitrain
        {
            get
            {
                try
                {
                    TrainController trains = TrainController.instance;
                    return trains != null && trains.UsingMiniTrain();
                }
                catch (Exception e)
                {
                    ApiLog.Once(PlayerState.Owner, "Travel.UsingMinitrain", e);
                    return false;
                }
            }
        }

        public static List<string> Entrypoints()
        {
            List<string> names = new List<string>();
            PlayerLevelEntrypoints points = PlayerLevelEntrypoints.instance;
            if (points == null || points.Entrypoints == null) return names;
            foreach (PlayerLevelEntrypoints.Entrypoint entry in points.Entrypoints)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Name)) names.Add(entry.Name);
            }
            return names;
        }

        public static bool TeleportTo(string name)
        {
            if (string.IsNullOrEmpty(name) || SaveSlots.WorldBusy()) return false;
            try
            {
                PlayerLevelEntrypoints points = PlayerLevelEntrypoints.instance;
                PlayerLevelEntrypoints.Entrypoint entry = points == null ? null : points.GetEntrypoint(name);
                if (entry == null || entry.Location == null || PlayerLocator.instance == null) return false;
                entry.TeleportPlayer();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Travel.TeleportTo", e);
                return false;
            }
        }

        public static bool OpenFastTravelMenu()
        {
            try
            {
                GameUIController ui = GameUIController.instance;
                FastTravelController fastTravel = FastTravelController.instance;
                if (ui == null || fastTravel == null) return false;
                if (ui.IsMenuVisible && ui.currentPanel == MenuPanels.FastTravel) return false;
                fastTravel.ToggleMenu();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Travel.OpenFastTravelMenu", e);
                return false;
            }
        }
    }
}
