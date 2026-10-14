using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class GrowingSlotInfo
    {
        public string SpotId { get; private set; }

        public string PlantId { get; private set; }

        public bool Collectible { get; private set; }

        internal GrowingSlotInfo(string spotId, string plantId, bool collectible)
        {
            SpotId = spotId;
            PlantId = plantId;
            Collectible = collectible;
        }
    }

    public sealed class GrowingStationInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public int SlotCount { get; private set; }

        public bool Paused { get; private set; }

        public bool ManualHarvest { get; private set; }

        public List<GrowingSlotInfo> Slots { get; private set; }

        internal GrowingStationInfo(string id, Vector3 position, int slotCount, bool paused, bool manualHarvest, List<GrowingSlotInfo> slots)
        {
            Id = id;
            Position = position;
            SlotCount = slotCount;
            Paused = paused;
            ManualHarvest = manualHarvest;
            Slots = slots;
        }
    }

    public static class GrowingStations
    {
        internal const string Owner = "obenseuer.moddingframework.growingstations";

        public static List<GrowingStationInfo> All()
        {
            List<GrowingStationInfo> list = new List<GrowingStationInfo>();
            try
            {
                foreach (Growing station in SceneIds.Active<Growing>())
                {
                    if (station != null) list.Add(ToInfo(station));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "All", e);
                return new List<GrowingStationInfo>();
            }
        }

        public static GrowingStationInfo Get(string id)
        {
            try
            {
                Growing station = SceneIds.Find<Growing>(id);
                return station == null ? null : ToInfo(station);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Get", e);
                return null;
            }
        }

        public static bool SetPaused(string modId, string id, bool paused)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                Growing station = SceneIds.Find<Growing>(id);
                if (station == null) return false;
                if (paused) station.PauseGrowth();
                else station.UnpauseGrowth();
                return station.paused == paused;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetPaused", e);
                return false;
            }
        }

        public static bool SetRotting(string modId, string id, bool rotting)
        {
            ModIds.Require(modId, "modId");
            if (SaveGate.Busy) return false;
            try
            {
                Growing station = SceneIds.Find<Growing>(id);
                if (station == null) return false;
                if (rotting) station.StartRot();
                else station.StopRot();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SetRotting", e);
                return false;
            }
        }

        private static GrowingStationInfo ToInfo(Growing station)
        {
            List<GrowingSlotInfo> slots = new List<GrowingSlotInfo>();
            foreach (GrowingSpot spot in station.GrowingSlots)
            {
                if (spot == null || !spot.HasPlant()) continue;
                grow plant = spot.Plant.GetComponent<grow>();
                bool collectible = plant == null && spot.Plant.GetComponentInChildren<CollectibleItem>() != null;
                slots.Add(new GrowingSlotInfo(SceneIds.Of(spot), plant == null ? null : plant.GUID, collectible));
            }
            return new GrowingStationInfo(SceneIds.Of(station), station.transform.position, station.GrowingSlots.Count, station.paused, station.ManualHarvest, slots);
        }
    }
}
