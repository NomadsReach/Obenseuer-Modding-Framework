using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class InteractionArgs
    {
        public string Name { get; private set; }

        public string Kind { get; private set; }

        public Vector3 Position { get; private set; }

        public float Distance { get; private set; }

        public string Guid { get; private set; }

        public GameObject Target { get; private set; }

        internal InteractionArgs(GameObject target, Component interactable)
        {
            Name = target.name;
            Kind = interactable == null ? null : interactable.GetType().Name;
            Position = target.transform.position;
            Distance = Interactions.DistanceFromCamera(Position);
            Guid = Interactions.SaveGuid(target);
            Target = target;
        }
    }

    public static class Interactions
    {
        internal const string Owner = "obenseuer.moddingframework.interaction";
        private const string DefaultVerb = "Use";
        private static bool installed;

        private static readonly List<ModInteraction> pending = new List<ModInteraction>();
        private static readonly Dictionary<Type, FieldInfo> guidFields = new Dictionary<Type, FieldInfo>();

        internal static Component Dispatching;

        public static event Action<InteractionArgs> Used;

        public static bool Add(GameObject target, string modId, string label, Action use)
        {
            return Add(target, modId, label, DefaultVerb, use);
        }

        public static bool Add(GameObject target, string modId, string label, string verb, Action use)
        {
            if (target == null) throw new ArgumentNullException("target");
            ModIds.RequireLegacy(modId, "modId");
            if (label == null) throw new ArgumentNullException("label");
            if (use == null) throw new ArgumentNullException("use");
            if (SaveGate.Busy) return false;
            if (target != null && use != null)
            {
                try
                {
                    pending.RemoveAll(i => i == null);
                    ModInteraction old = target.GetComponent<ModInteraction>();
                    if (old != null && pending.Contains(old))
                    {
                        if (old == Dispatching) return false;
                        pending.Remove(old);
                        UnityEngine.Object.DestroyImmediate(old);
                    }
                }
                catch (Exception e)
                {
                    CoreLog.For(Owner).Error("Interactions.Add failed: " + e.ToString());
                    return false;
                }
            }
            return ComputerApps.AddInteraction(target, modId, label, string.IsNullOrWhiteSpace(verb) ? DefaultVerb : verb, use);
        }

        public static bool SetReason(GameObject target, string modId, Func<string> reason)
        {
            if (target == null) throw new ArgumentNullException("target");
            ModIds.Require(modId, "modId");
            try
            {
                ModInteraction interaction = target.GetComponent<ModInteraction>();
                if (interaction == null || interaction.ModId != modId || pending.Contains(interaction)) return false;
                interaction.Reason = reason;
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Interactions.SetReason failed: " + e.ToString());
                return false;
            }
        }

        public static bool Remove(GameObject target, string modId)
        {
            if (target == null) throw new ArgumentNullException("target");
            ModIds.Require(modId, "modId");
            try
            {
                ModInteraction interaction = target.GetComponent<ModInteraction>();
                if (interaction == null || interaction.ModId != modId) return false;
                interaction.DisableInteract();
                if (interaction.Moved) target.layer = interaction.OriginalLayer;
                if (interaction == Dispatching)
                {
                    pending.Add(interaction);
                    UnityEngine.Object.Destroy(interaction);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(interaction);
                }
                ComputerApps.ForgetInteraction(interaction);
                return true;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Interactions.Remove failed: " + e.ToString());
                return false;
            }
        }

        public static InteractionArgs Looking()
        {
            try
            {
                InteractObjects objects = InteractObjects.instance;
                if (objects == null || objects.currentGameObject == null) return null;
                GameObject target = objects.currentGameObject;
                return new InteractionArgs(target, FirstInteractable(target));
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("Interactions.Looking failed: " + e.ToString());
                return null;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = InteractionsPatches.Install();
            installed &= LocksPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("interaction hooks incomplete, Used, Locked and Unlocked events are disabled");
        }

        internal static bool HasUsedHandlers
        {
            get { return Used != null; }
        }

        internal static void RaiseUsed(InteractionArgs interactionEvent)
        {
            Events.Raise(Used, "Interactions.Used", h => h(interactionEvent));
        }

        internal static float DistanceFromCamera(Vector3 position)
        {
            ThirdPersonCameraController camera = ThirdPersonCameraController.instance;
            return camera == null ? 0f : Vector3.Distance(camera.transform.position, position);
        }

        internal static Component FirstInteractable(GameObject target)
        {
            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IInteractable) return behaviour;
            }
            return null;
        }

        internal static string SaveGuid(GameObject target)
        {
            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                FieldInfo field = GuidField(behaviour.GetType());
                if (field == null) continue;
                string guid = (string)field.GetValue(behaviour);
                if (!string.IsNullOrEmpty(guid)) return guid;
            }
            return null;
        }

        private static FieldInfo GuidField(Type type)
        {
            FieldInfo field;
            if (!guidFields.TryGetValue(type, out field))
            {
                field = type.GetField("GUID", BindingFlags.Instance | BindingFlags.Public);
                if (field != null && field.FieldType != typeof(string)) field = null;
                guidFields[type] = field;
            }
            return field;
        }
    }
}
