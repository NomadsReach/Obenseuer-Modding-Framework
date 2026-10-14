using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class ResourceNodeInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public List<int> ItemIds { get; private set; }

        public int Stock { get; private set; }

        public int MaxStock { get; private set; }

        public int OwnerId { get; private set; }

        public bool PermissionToTake { get; private set; }

        public bool RespawnEnabled { get; private set; }

        internal ResourceNodeInfo(string id, Vector3 position, List<int> itemIds, int stock, int maxStock, int ownerId, bool permissionToTake, bool respawnEnabled)
        {
            Id = id;
            Position = position;
            ItemIds = itemIds;
            Stock = stock;
            MaxStock = maxStock;
            OwnerId = ownerId;
            PermissionToTake = permissionToTake;
            RespawnEnabled = respawnEnabled;
        }
    }

    public sealed class ResourceHitArgs
    {
        public string NodeId { get; private set; }

        public int Stock { get; private set; }

        public bool Yielded { get; private set; }

        internal ResourceHitArgs(string nodeId, int stock, bool yielded)
        {
            NodeId = nodeId;
            Stock = stock;
            Yielded = yielded;
        }
    }

    public sealed class ResourceDepletedArgs
    {
        public string NodeId { get; private set; }

        internal ResourceDepletedArgs(string nodeId)
        {
            NodeId = nodeId;
        }
    }

    public static class ResourceNodes
    {
        internal const string Owner = "obenseuer.moddingframework.resources";
        private static bool installed;
        private static readonly FieldInfo StockField = AccessTools.Field(typeof(WorkableResourceSource), "currentStock");
        private static readonly FieldInfo MaxStockField = AccessTools.Field(typeof(WorkableResourceSource), "maximumStock");
        private static readonly FieldInfo OutputItemsField = AccessTools.Field(typeof(WorkableResourceSource), "outputItems");

        public static event Action<ResourceHitArgs> Hit;

        public static event Action<ResourceDepletedArgs> Depleted;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<ResourceNodeInfo> All()
        {
            try
            {
                return ToInfos(Nodes());
            }
            catch (Exception e)
            {
                ApiLog.Once(ResourceNodes.Owner, "All", e);
                return new List<ResourceNodeInfo>();
            }
        }

        public static List<ResourceNodeInfo> Near(Vector3 position, float radius)
        {
            if (!(radius >= 0f)) throw new ArgumentException("radius must not be negative or NaN", "radius");
            List<ResourceNodeInfo> list = new List<ResourceNodeInfo>();
            try
            {
                foreach (WorkableResourceSource node in Nodes())
                {
                    if (Vector3.Distance(node.transform.position, position) <= radius) list.Add(ToInfo(node));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(ResourceNodes.Owner, "Near", e);
                return new List<ResourceNodeInfo>();
            }
        }

        public static ResourceNodeInfo Get(string id)
        {
            try
            {
                WorkableResourceSource node = SceneIds.Find<WorkableResourceSource>(id);
                return node == null ? null : ToInfo(node);
            }
            catch (Exception e)
            {
                ApiLog.Once(ResourceNodes.Owner, "Get", e);
                return null;
            }
        }

        public static bool SetStock(string id, int stock)
        {
            if (stock < 0) throw new ArgumentException("stock must not be negative", "stock");
            try
            {
                if (SaveGate.Busy) return false;
                WorkableResourceSource node = SceneIds.Find<WorkableResourceSource>(id);
                if (node == null) return false;
                int max = (int)MaxStockField.GetValue(node);
                int value = Math.Min(stock, max);
                StockField.SetValue(node, value);
                node.ChangeDeltaSecondsStatus(value < max);
                if (node.onStock != null) node.onStock.triggerOutputs((float)value / (float)max);
                if (node.onCurrentStock != null) node.onCurrentStock.triggerOutputs(value);
                node.UpdateState();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(ResourceNodes.Owner, "SetStock", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = StockField != null && MaxStockField != null && OutputItemsField != null && ResourceNodesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("resource node hooks incomplete, hit and depleted events are disabled");
        }

        internal static bool HasHandlers
        {
            get { return Hit != null || Depleted != null; }
        }

        internal static void RaiseHit(ResourceHitArgs args)
        {
            Events.Raise(Hit, "ResourceNodes.Hit", h => h(args));
        }

        internal static void RaiseDepleted(ResourceDepletedArgs args)
        {
            Events.Raise(Depleted, "ResourceNodes.Depleted", h => h(args));
        }

        private static List<WorkableResourceSource> Nodes()
        {
            List<WorkableResourceSource> nodes = new List<WorkableResourceSource>();
            foreach (WorkableResourceSource node in SceneIds.Active<WorkableResourceSource>())
            {
                if (node != null) nodes.Add(node);
            }
            return nodes;
        }

        private static List<ResourceNodeInfo> ToInfos(List<WorkableResourceSource> nodes)
        {
            List<ResourceNodeInfo> list = new List<ResourceNodeInfo>();
            foreach (WorkableResourceSource node in nodes) list.Add(ToInfo(node));
            return list;
        }

        internal static ResourceNodeInfo ToInfo(WorkableResourceSource node)
        {
            List<int> itemIds = new List<int>();
            List<ItemReference> outputs = (List<ItemReference>)OutputItemsField.GetValue(node);
            if (outputs != null)
            {
                foreach (ItemReference reference in outputs)
                {
                    if (reference != null && reference.ID > 0) itemIds.Add(reference.ID);
                }
            }
            SpawnSettings spawn = node.spawnSettings;
            return new ResourceNodeInfo(SceneIds.Of(node), node.transform.position, itemIds, (int)StockField.GetValue(node), (int)MaxStockField.GetValue(node), node.owner == null ? 0 : node.owner.ID, node.permissionToTake, spawn != null && spawn.respawnEnabled);
        }
    }
}
