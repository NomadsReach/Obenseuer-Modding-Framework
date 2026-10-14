using System;
using System.Collections.Generic;
using OS.Items;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class RegistryHeld
    {
        private static readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();

        internal static bool Set(string modGuid, int itemId, ModelHandle model)
        {
            Entry entry;
            if (entries.TryGetValue(itemId, out entry))
            {
                if (entry.Mod != modGuid)
                {
                    CoreLog.For(ModelPatches.Owner).Warn("held model for item " + itemId + " refused: " + entry.Mod + " already set it, and " + modGuid + " cannot replace it");
                    return false;
                }
            }
            else
            {
                entry = new Entry { Mod = modGuid };
                entries.Add(itemId, entry);
            }
            if (entry.Model != model)
            {
                entry.Model = model;
                if (entry.Prefab != null) Drop(itemId, entry);
            }
            List<Item> list = ItemDatabase.database;
            if (list != null && list.Count > 0) Apply(list);
            return true;
        }

        internal static void Apply(List<Item> list)
        {
            foreach (KeyValuePair<int, Entry> pair in entries)
            {
                Item item = ItemRegistry.Find(list, pair.Key);
                ItemEquipable action = item == null ? null : item.GetActionOfType<ItemEquipable>() as ItemEquipable;
                if (action == null)
                {
                    ApiLog.Once(ModelPatches.Owner, "held model item " + pair.Key, "item " + pair.Key + " is not an equipable item in the catalogue, so no held model is set");
                    continue;
                }
                if (action.wieldPrefab != null && action.wieldPrefab == pair.Value.Prefab) continue;
                string refusal;
                try
                {
                    refusal = Prepare(pair.Key, pair.Value, action);
                }
                catch (Exception e)
                {
                    ApiLog.Once(ModelPatches.Owner, "held model item " + pair.Key, e);
                    continue;
                }
                if (refusal != null)
                {
                    ApiLog.Once(ModelPatches.Owner, "held model item " + pair.Key, "not applied, the template's model is kept: " + refusal);
                    continue;
                }
                action.wieldPrefab = pair.Value.Prefab;
            }
        }

        private static void Drop(int itemId, Entry entry)
        {
            List<Item> list = ItemDatabase.database;
            Item item = list == null ? null : ItemRegistry.Find(list, itemId);
            ItemEquipable action = item == null ? null : item.GetActionOfType<ItemEquipable>() as ItemEquipable;
            if (action != null && action.wieldPrefab == entry.Prefab) action.wieldPrefab = null;
            UnityEngine.Object.DestroyImmediate(entry.Prefab);
            entry.Prefab = null;
        }

        private static string Prepare(int itemId, Entry entry, ItemEquipable action)
        {
            if (entry.Prefab != null) return null;
            if (!entry.Model.IsReady) return "the model is not ready: " + entry.Model.Error;
            if (!Core.OnMain()) return "the held model is built on the Unity main thread only";
            if (action.wieldPrefab == null) action.LoadPrefab();
            GameObject template = action.wieldPrefab;
            if (template == null) return "the item has no held template";
            if (template.GetComponent<PlayerHandItems.IPlayerHandItem>() == null) return "the held template has no IPlayerHandItem";
            GameObject clone = UnityEngine.Object.Instantiate(template, ModelBuild.Holder().transform);
            string refusal = Swap(clone.GetComponent<PlayerHandItems.IPlayerHandItem>(), entry.Model);
            if (refusal != null)
            {
                UnityEngine.Object.DestroyImmediate(clone);
                return refusal;
            }
            clone.name = "obxh_" + itemId;
            entry.Prefab = clone;
            return null;
        }

        private static string Swap(PlayerHandItems.IPlayerHandItem hand, ModelHandle model)
        {
            MeshRenderer first = hand.objectFirstPerson == null ? null : ModelBuild.Primary(hand.objectFirstPerson);
            MeshRenderer third = hand.objectThirdPerson == null ? null : ModelBuild.Primary(hand.objectThirdPerson);
            MeshRenderer sized = first ?? third;
            if (sized == null) return "the held template has no mesh in its first or third person object";
            float templateEdge = ModelBuild.Largest(sized.GetComponent<MeshFilter>().sharedMesh.bounds.size);
            float modelEdge = ModelBuild.Largest(model.Bounds.size);
            if (templateEdge <= 0f || modelEdge <= 0f) return "the template mesh or the model has no size";
            float fit = model.Options.FitToTemplate ? templateEdge / modelEdge : 1f;
            if (float.IsNaN(fit) || float.IsInfinity(fit)) return "the fitted size is not finite";
            Mesh mesh = ModelBuild.BuildMesh(model.Data, model.Options, fit, model.Key);
            Material[] materials = ModelBuild.BuildMaterials(model, sized.sharedMaterial);
            Replace(hand.objectFirstPerson, mesh, materials);
            Replace(hand.objectThirdPerson, mesh, materials);
            return null;
        }

        private static void Replace(GameObject root, Mesh mesh, Material[] materials)
        {
            if (root == null) return;
            MeshRenderer primary = ModelBuild.Primary(root);
            if (primary == null) return;
            primary.GetComponent<MeshFilter>().sharedMesh = mesh;
            primary.sharedMaterials = materials;
            foreach (MeshRenderer other in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (other != primary) other.enabled = false;
            }
        }

        private sealed class Entry
        {
            internal string Mod;
            internal ModelHandle Model;
            internal GameObject Prefab;
        }
    }
}
