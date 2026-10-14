using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class DroneInfo
    {
        public string Id { get; private set; }

        public bool Active { get; private set; }

        public Vector3 Position { get; private set; }

        public float MaxSpeed { get; private set; }

        public string TargetNodeId { get; private set; }

        public string NextNodeId { get; private set; }

        internal DroneInfo(string id, bool active, Vector3 position, float maxSpeed, string targetNodeId, string nextNodeId)
        {
            Id = id;
            Active = active;
            Position = position;
            MaxSpeed = maxSpeed;
            TargetNodeId = targetNodeId;
            NextNodeId = nextNodeId;
        }
    }

    public sealed class DroneChangedArgs
    {
        public string Id { get; private set; }

        public bool Active { get; private set; }

        internal DroneChangedArgs(string id, bool active)
        {
            Id = id;
            Active = active;
        }
    }

    public sealed class DroneNodePassedArgs
    {
        public string NodeId { get; private set; }

        public string DroneId { get; private set; }

        public bool IsRouteEnd { get; private set; }

        internal DroneNodePassedArgs(string nodeId, string droneId, bool isRouteEnd)
        {
            NodeId = nodeId;
            DroneId = droneId;
            IsRouteEnd = isRouteEnd;
        }
    }

    public sealed class AirNodeInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public List<string> LinkedNodeIds { get; private set; }

        internal AirNodeInfo(string id, Vector3 position, List<string> linkedNodeIds)
        {
            Id = id;
            Position = position;
            LinkedNodeIds = linkedNodeIds;
        }
    }

    public static class Drones
    {
        internal const string Owner = "obenseuer.moddingframework.drones";
        private static bool installed;

        public static event Action<DroneChangedArgs> Changed;

        public static event Action<DroneNodePassedArgs> NodePassed;

        public static List<AirNodeInfo> Nodes()
        {
            List<AirNodeInfo> list = new List<AirNodeInfo>();
            try
            {
                foreach (Air_Node node in SceneIds.Active<Air_Node>())
                {
                    list.Add(NodeToInfo(node));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Drones.Nodes", e);
            }
            return list;
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<DroneInfo> All()
        {
            List<DroneInfo> list = new List<DroneInfo>();
            try
            {
                foreach (Drone drone in SceneIds.Active<Drone>())
                {
                    list.Add(ToInfo(drone));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Drones.All", e);
            }
            return list;
        }

        public static DroneInfo Get(string id)
        {
            Drone drone = Find(id);
            return drone == null ? null : ToInfo(drone);
        }

        public static bool SetActive(string id, bool active)
        {
            if (SaveGate.Busy) return false;
            Drone drone = Find(id);
            if (drone == null) return false;
            try
            {
                if (active) drone.TurnOn();
                else drone.TurnOff();
                return drone.active == active;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Drones.SetActive", e);
                return false;
            }
        }

        public static bool SetTarget(string id, string nodeId)
        {
            if (SaveGate.Busy) return false;
            Drone drone = Find(id);
            Air_Node node = SceneIds.Find<Air_Node>(nodeId);
            if (drone == null || node == null) return false;
            try
            {
                drone.SetTarget(node);
                return drone.targetNode == node;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Drones.SetTarget", e);
                return false;
            }
        }

        public static bool SetMaxSpeed(string id, float maxSpeed)
        {
            if (SaveGate.Busy) return false;
            Drone drone = Find(id);
            if (drone == null || !(maxSpeed > 0f) || float.IsInfinity(maxSpeed)) return false;
            try
            {
                drone.SetMaxSpeed(maxSpeed);
                return drone.maxSpeed == maxSpeed;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Drones.SetMaxSpeed", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = DronesPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static void RaiseChanged(Drone drone)
        {
            if (Changed == null) return;
            DroneChangedArgs args = new DroneChangedArgs(SceneIds.Of(drone), drone.active);
            Events.Raise(Changed, "Drones.Changed", h => h(args));
        }

        internal static void RaiseNodePassed(Air_Node node, Drone drone)
        {
            if (NodePassed == null) return;
            string droneId = drone != null && drone.targetNode == node ? SceneIds.Of(drone) : null;
            DroneNodePassedArgs args = new DroneNodePassedArgs(SceneIds.Of(node), droneId, node.NextNode == null);
            Events.Raise(NodePassed, "Drones.NodePassed", h => h(args));
        }

        private static Drone Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Drone drone in SceneIds.Active<Drone>())
                {
                    if (SceneIds.Of(drone) == id) return drone;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Drones.Find", e);
            }
            return null;
        }

        private static DroneInfo ToInfo(Drone drone)
        {
            Air_Node target = drone.targetNode;
            Air_Node next = target == null ? null : target.NextNode;
            return new DroneInfo(SceneIds.Of(drone), drone.active, drone.transform.position, drone.maxSpeed, NodeId(target), NodeId(next));
        }

        private static string NodeId(Air_Node node)
        {
            return node == null ? null : SceneIds.Of(node);
        }

        private static AirNodeInfo NodeToInfo(Air_Node node)
        {
            List<string> linked = new List<string>();
            if (node.nextNodes != null)
            {
                foreach (Air_Node next in node.nextNodes)
                {
                    if (next != null) linked.Add(SceneIds.Of(next));
                }
            }
            return new AirNodeInfo(SceneIds.Of(node), node.transform.position, linked);
        }
    }
}
