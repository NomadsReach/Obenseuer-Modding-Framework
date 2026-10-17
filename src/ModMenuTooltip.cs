using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ObenseuerCore
{
    internal sealed class ModMenuTooltipLease
    {
        private object owner;
        private object tooltip;

        internal void Claim(object row, object instance)
        {
            owner = row;
            tooltip = instance;
        }

        internal void Changed(object instance)
        {
            if (ReferenceEquals(tooltip, instance)) Claim(null, null);
        }

        internal bool Release(object row, object instance)
        {
            if (owner == null || (row != null && !ReferenceEquals(owner, row))) return false;
            bool matches = ReferenceEquals(tooltip, instance);
            Claim(null, null);
            return matches;
        }
    }

    internal sealed class ModMenuTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly ModMenuTooltipLease lease = new ModMenuTooltipLease();
        private static bool attempted;
        private static bool ready;
        internal string Title;
        internal string Detail;

        public void OnPointerEnter(PointerEventData data)
        {
            if (!Install() || ToolTip.instance == null) return;
            ToolTip tooltip = ToolTip.instance;
            tooltip.Activate(Title, Detail);
            lease.Claim(this, tooltip);
        }

        public void OnPointerExit(PointerEventData data)
        {
            Clear(this);
        }

        private void OnDisable()
        {
            Clear(this);
        }

        private void OnDestroy()
        {
            Clear(this);
        }

        internal static void Clear()
        {
            Clear(null);
        }

        private static void Clear(object row)
        {
            ToolTip tooltip = ToolTip.instance;
            if (lease.Release(row, tooltip) && tooltip != null) tooltip.Deactivate();
        }

        private static void Changed(ToolTip __instance)
        {
            lease.Changed(__instance);
        }

        private static bool Install()
        {
            if (attempted) return ready;
            attempted = true;
            ready = true;
            MethodInfo prefix = typeof(ModMenuTooltip).GetMethod("Changed", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo[] methods = typeof(ToolTip).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "Activate" && method.Name != "ConstructDataString" && !(method.Name == "Deactivate" && parameters.Length == 0)) continue;
                Type[] args = new Type[parameters.Length];
                for (int j = 0; j < args.Length; j++) args[j] = parameters[j].ParameterType;
                if (!CorePatcher.PatchCore(typeof(ToolTip), method.Name, args, prefix, null, null, false, ModMenu.Owner)) ready = false;
            }
            return ready;
        }
    }
}
