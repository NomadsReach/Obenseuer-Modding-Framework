using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Outputs;

namespace ObenseuerCore
{
    public sealed class RelayInfo
    {
        public string Id { get; private set; }

        public bool FireOnce { get; private set; }

        public bool Fired { get; private set; }

        public bool Enabled { get; private set; }

        internal RelayInfo(string id, bool fireOnce, bool fired, bool enabled)
        {
            Id = id;
            FireOnce = fireOnce;
            Fired = fired;
            Enabled = enabled;
        }
    }

    public sealed class RelayArgs
    {
        public string Id { get; private set; }

        public bool FireOnce { get; private set; }

        public bool Enabled { get; private set; }

        internal RelayArgs(string id, bool fireOnce, bool enabled)
        {
            Id = id;
            FireOnce = fireOnce;
            Enabled = enabled;
        }
    }

    public static class SceneLogic
    {
        internal const string Owner = "obenseuer.moddingframework.scenelogic";
        private static bool installed;
        private static readonly FieldInfo taskItemsField = AccessTools.Field(typeof(Relay), "requiredTaskItems");
        private static readonly FieldInfo objectivesField = AccessTools.Field(typeof(Relay), "requiredObjectives");
        private static readonly FieldInfo variablesField = AccessTools.Field(typeof(Relay), "requiredDialogueVariables");

        public static event Action<RelayArgs> Fired;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<RelayInfo> Relays()
        {
            List<RelayInfo> list = new List<RelayInfo>();
            try
            {
                foreach (Relay relay in SceneIds.Active<Relay>())
                {
                    list.Add(new RelayInfo(SceneIds.Of(relay), relay.fireOnceOnly, relay.firedOnce, RelayEnabled(relay)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneLogic.Relays", e);
            }
            return list;
        }

        public static bool Fire(string id)
        {
            try
            {
                Relay relay = SceneIds.Find<Relay>(id);
                if (relay == null || SaveGate.Busy || !CanFire(relay)) return false;
                relay.triggerOutputs();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneLogic.Fire", e);
                return false;
            }
        }

        public static bool SetEnabled(string id, bool enabled)
        {
            try
            {
                if (SaveGate.Busy) return false;
                Relay relay = SceneIds.Find<Relay>(id);
                if (relay != null)
                {
                    if (enabled) relay.enableOutputs();
                    else relay.disableOutputs();
                    return RelayEnabled(relay) == enabled;
                }
                TriggerMultiple trigger = SceneIds.Find<TriggerMultiple>(id);
                if (trigger == null) return false;
                if (enabled) trigger.enableOutputs();
                else trigger.disableOutputs();
                return TriggerEnabled(trigger) == enabled;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "SceneLogic.SetEnabled", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = SceneLogicPatches.Install();
            if (!installed) CorePatcher.DisableOwner(Owner);
        }

        internal static bool HasHandlers
        {
            get
            {
                return Fired != null;
            }
        }

        internal static bool CanFire(Relay relay)
        {
            return relay.gameObject != null && relay.gameObject.activeInHierarchy && !(relay.firedOnce && relay.fireOnceOnly) && RequirementsMet(relay);
        }

        private static bool RequirementsMet(Relay relay)
        {
            List<TaskItem> items = (List<TaskItem>)taskItemsField.GetValue(relay);
            List<ObjectiveStatusInfo> objectives = (List<ObjectiveStatusInfo>)objectivesField.GetValue(relay);
            List<DialogueVariable> variables = (List<DialogueVariable>)variablesField.GetValue(relay);
            return (items.Count == 0 || TaskItemsManager.instance.CheckIfPlayerHasTaskItem(items)) && (objectives.Count == 0 || TaskController.instance.CheckObjectiveStatusInfos(objectives)) && (variables.Count == 0 || DialogueVariable.IsAllTrue(variables));
        }

        internal static void RaiseFired(Relay relay)
        {
            RelayArgs args = new RelayArgs(SceneIds.Of(relay), relay.fireOnceOnly, RelayEnabled(relay));
            Events.Raise(Fired, "SceneLogic.Fired", h => h(args));
        }

        private static bool RelayEnabled(Relay relay)
        {
            if (relay.outputs == null) return true;
            foreach (output item in relay.outputs)
            {
                if (item.disabled) return false;
            }
            return true;
        }

        private static bool TriggerEnabled(TriggerMultiple trigger)
        {
            if (trigger.outputs == null) return true;
            foreach (triggerOutput item in trigger.outputs)
            {
                if (item.disabled) return false;
            }
            return true;
        }
    }
}
