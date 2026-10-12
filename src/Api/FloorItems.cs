using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class FloorItems
    {
        private const string ErrorPath = "Prefabs/Items/ERROR";

        internal static void Init()
        {
            bool installed = CorePatcher.PatchCore(typeof(DestructibleList), "OnLoadingGameDestructibleList", Type.EmptyTypes, null, null, null, false, ModelPatches.Owner, Priority.Last, CorePatcher.Handler(typeof(FloorItems), "Transpile"));
            if (installed) CoreLog.For(ModelPatches.Owner).Info("floor item load patch installed");
            else CoreLog.For(ModelPatches.Owner).Warn("floor item load patch not installed, dropped modded items may load as the ERROR model");
        }

        internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            List<int> sites = LoadSites(codes, typeof(InstantiatedPrefab), "PrefabPath", ResourcesLoad());
            if (sites.Count != 1)
            {
                CoreLog.For(ModelPatches.Owner).Warn("floor item load patch: found " + sites.Count + " Resources.Load(PrefabPath) sites, expected 1, vanilla loading kept");
                return codes;
            }
            int call = sites[0];
            codes[call - 1].opcode = OpCodes.Call;
            codes[call - 1].operand = CorePatcher.Handler(typeof(FloorItems), "Resolve");
            codes[call].opcode = OpCodes.Nop;
            codes[call].operand = null;
            return codes;
        }

        internal static List<int> LoadSites(List<CodeInstruction> codes, Type holder, string field, MethodInfo load)
        {
            List<int> sites = new List<int>();
            if (load == null) return sites;
            for (int i = 1; i < codes.Count; i++)
            {
                FieldInfo read = codes[i - 1].operand as FieldInfo;
                if (codes[i - 1].opcode == OpCodes.Ldfld && read != null && read.DeclaringType == holder && read.Name == field && codes[i].opcode == OpCodes.Call && load.Equals(codes[i].operand)) sites.Add(i);
            }
            return sites;
        }

        internal static MethodInfo ResourcesLoad()
        {
            foreach (MethodInfo method in typeof(Resources).GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "Load" && !method.IsGenericMethod && parameters.Length == 1 && parameters[0].ParameterType == typeof(string)) return method;
            }
            return null;
        }

        internal static UnityEngine.Object Resolve(InstantiatedPrefab prefab)
        {
            UnityEngine.Object loaded = Resources.Load(prefab.PrefabPath);
            try
            {
                ItemStack stack = prefab.data as ItemStack;
                if (stack == null || (loaded != null && prefab.PrefabPath != ErrorPath)) return loaded;
                GameObject model = ModelOf(stack);
                return model == null ? loaded : model;
            }
            catch (Exception e)
            {
                ApiLog.Once(ModelPatches.Owner, "FloorItems.Resolve", e);
                return loaded;
            }
        }

        internal static void FixSavedPath(InstantiatedPrefab data, CollectibleItem collectible)
        {
            if (Loads(data.PrefabPath)) return;
            Item item = collectible.Item.Item;
            if (item == null || item.Appearance == null) return;
            string many = item.Appearance.PrefabPathMany;
            string path = collectible.amount > 1 && Loads(many) ? many : item.Appearance.PrefabPath;
            if (Loads(path)) data.PrefabPath = path;
        }

        private static GameObject ModelOf(ItemStack stack)
        {
            Item item = ItemDatabase.instance == null ? null : ItemDatabase.instance.FetchItemByID(stack.itemId);
            if (item == null || item.Appearance == null) return null;
            GameObject model = stack.itemAmount != 1 && item.PrefabMany != null ? item.PrefabMany : item.Prefab;
            return model == null || model == Resources.Load<GameObject>(ErrorPath) ? null : model;
        }

        private static bool Loads(string path)
        {
            return !string.IsNullOrEmpty(path) && path != ErrorPath && Resources.Load<GameObject>(path) != null;
        }
    }
}
