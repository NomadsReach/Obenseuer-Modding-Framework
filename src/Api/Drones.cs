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

    public static class Drones
    {
        internal const string Owner = "obenseuer.moddingframework.drones";
        private static bool installed;

        public static event Action<DroneChangedArgs> Changed;

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
                foreach (Drone drone in UnityEngine.Object.FindObjectsByType<Drone>(UnityEngine.FindObjectsSortMode.None))
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

        private static Drone Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            try
            {
                foreach (Drone drone in UnityEngine.Object.FindObjectsByType<Drone>(UnityEngine.FindObjectsSortMode.None))
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
    }
}
