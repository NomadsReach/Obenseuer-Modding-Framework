using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Travel
    {
        private static readonly Dictionary<PlayerLevelEntrypoints.Entrypoint, string> added = new Dictionary<PlayerLevelEntrypoints.Entrypoint, string>();

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

        public static bool AddEntrypoint(string name, Transform location)
        {
            return Add(string.Empty, name, location);
        }

        public static bool AddEntrypoint(string modId, string name, Transform location)
        {
            ModIds.Require(modId, "modId");
            return Add(modId, name, location);
        }

        public static bool RemoveEntrypoint(string name)
        {
            return Remove(string.Empty, name);
        }

        public static bool RemoveEntrypoint(string modId, string name)
        {
            ModIds.Require(modId, "modId");
            return Remove(modId, name);
        }

        private static bool Add(string modId, string name, Transform location)
        {
            if (string.IsNullOrEmpty(name) || location == null) return false;
            try
            {
                PlayerLevelEntrypoints points = PlayerLevelEntrypoints.instance;
                if (points == null || points.Entrypoints == null) return false;
                PlayerLevelEntrypoints.Entrypoint existing = points.GetEntrypoint(name);
                if (existing != null)
                {
                    string holder;
                    string by = added.TryGetValue(existing, out holder) ? (holder.Length == 0 ? "a mod" : holder) : "the level";
                    string from = modId.Length == 0 ? "a mod" : modId;
                    ApiLog.Once(PlayerState.Owner, "Travel.AddEntrypoint \"" + name + "\" from " + from, "the name already exists (added by " + by + "), the call is ignored");
                    return false;
                }
                PlayerLevelEntrypoints.Entrypoint entry = new PlayerLevelEntrypoints.Entrypoint(name, location);
                foreach (PlayerLevelEntrypoints.Entrypoint old in new List<PlayerLevelEntrypoints.Entrypoint>(added.Keys))
                {
                    if (!points.Entrypoints.Contains(old)) added.Remove(old);
                }
                points.Entrypoints.Add(entry);
                added[entry] = modId;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Travel.AddEntrypoint", e);
                return false;
            }
        }

        private static bool Remove(string modId, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            try
            {
                PlayerLevelEntrypoints points = PlayerLevelEntrypoints.instance;
                if (points == null || points.Entrypoints == null) return false;
                PlayerLevelEntrypoints.Entrypoint entry = points.Entrypoints.Find(candidate => candidate != null && candidate.Name == name && added.ContainsKey(candidate) && added[candidate] == modId);
                if (entry == null) return false;
                added.Remove(entry);
                return points.Entrypoints.Remove(entry);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Travel.RemoveEntrypoint", e);
                return false;
            }
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
