using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class TankInfo
    {
        public string Id { get; private set; }

        public int LiquidItemId { get; private set; }

        public float Amount { get; private set; }

        public float Capacity { get; private set; }

        public bool Locked { get; private set; }

        public bool Global { get; private set; }

        internal TankInfo(string id, int liquidItemId, float amount, float capacity, bool locked, bool global)
        {
            Id = id;
            LiquidItemId = liquidItemId;
            Amount = amount;
            Capacity = capacity;
            Locked = locked;
            Global = global;
        }
    }

    public sealed class TankChangedArgs
    {
        public string Id { get; private set; }

        public int LiquidItemId { get; private set; }

        public float Amount { get; private set; }

        public bool Locked { get; private set; }

        internal TankChangedArgs(string id, int liquidItemId, float amount, bool locked)
        {
            Id = id;
            LiquidItemId = liquidItemId;
            Amount = amount;
            Locked = locked;
        }
    }

    public static class Tanks
    {
        internal const string Owner = "obenseuer.moddingframework.tanks";
        private static bool installed;

        public static event Action<TankChangedArgs> Changed;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<TankInfo> All()
        {
            List<TankInfo> list = new List<TankInfo>();
            try
            {
                foreach (LiquidStorage tank in SceneIds.Active<LiquidStorage>())
                {
                    list.Add(ToInfo(tank));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Tanks.All", e);
            }
            return list;
        }

        public static TankInfo Get(string id)
        {
            try
            {
                LiquidStorage tank = SceneIds.Find<LiquidStorage>(id);
                return tank == null ? null : ToInfo(tank);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Tanks.Get", e);
                return null;
            }
        }

        public static bool Set(string id, int itemId, float amount)
        {
            try
            {
                LiquidStorage tank = SceneIds.Find<LiquidStorage>(id);
                if (tank == null || SaveGate.Busy || IsMainSource(tank) || tank.Capacity <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
                Item item = ItemRegistry.FindInDatabase(itemId);
                if (item == null) return false;
                float value = OS.Items.ItemLiquidData.SetAndGetLiquidAmount(Math.Min(Math.Max(0f, amount), tank.Capacity));
                if (value > tank.Capacity) value = tank.Capacity;
                if (tank.Liquid == null || tank.Liquid.ID != itemId || tank.IsEmpty()) tank.SetLiquid(new ItemReference(item));
                tank.LiquidAmount = value;
                Raise(tank);
                return tank.Liquid != null && tank.Liquid.ID == itemId && tank.LiquidAmount == value;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Tanks.Set", e);
                return false;
            }
        }

        public static bool Empty(string id)
        {
            try
            {
                LiquidStorage tank = SceneIds.Find<LiquidStorage>(id);
                if (tank == null || SaveGate.Busy || IsMainSource(tank)) return false;
                if (tank.Liquid == null && tank.LiquidAmount <= 0f) return true;
                tank.OnEmpty();
                Raise(tank);
                return tank.Liquid == null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Tanks.Empty", e);
                return false;
            }
        }

        public static bool SetLocked(string id, bool locked)
        {
            try
            {
                LiquidStorage tank = SceneIds.Find<LiquidStorage>(id);
                if (tank == null || tank._Lock == null || SaveGate.Busy || IsMainSource(tank)) return false;
                if (locked) tank.Lock();
                else tank.Unlock();
                Raise(tank);
                return tank._Lock.Locked == locked;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Tanks.SetLocked", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = true;
        }

        private static void Raise(LiquidStorage tank)
        {
            TankChangedArgs args = new TankChangedArgs(SceneIds.Of(tank), LiquidId(tank), tank.LiquidAmount, IsLocked(tank));
            Events.Raise(Changed, "Tanks.Changed", h => h(args));
        }

        private static bool IsMainSource(LiquidStorage tank)
        {
            WaterSourceController water = WaterSourceController.instance;
            return water != null && water.globalStorage == tank;
        }

        private static int LiquidId(LiquidStorage tank)
        {
            return tank.Liquid == null ? 0 : tank.Liquid.ID;
        }

        private static bool IsLocked(LiquidStorage tank)
        {
            return tank._Lock != null && tank._Lock.Locked;
        }

        private static TankInfo ToInfo(LiquidStorage tank)
        {
            return new TankInfo(SceneIds.Of(tank), LiquidId(tank), tank.LiquidAmount, tank.Capacity, IsLocked(tank), tank.global);
        }
    }
}
