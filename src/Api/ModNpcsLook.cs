using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ModNpcsLook
    {
        internal static SavedNpcLook Capture(Character copy, Character source, bool sliders)
        {
            SavedNpcLook look = new SavedNpcLook { Sliders = sliders };
            if (sliders)
            {
                foreach (LookDial dial in PlayerLookTable.All)
                {
                    float value = dial.GetCostume(copy);
                    if (value == dial.GetCostume(source)) continue;
                    look.DialNames.Add(dial.Label);
                    look.DialValues.Add(value);
                }
                foreach (LookSlot slot in PlayerLookTable.Slots)
                {
                    string name = PlayerLookTable.NameOf(slot.Take(copy));
                    if (name == PlayerLookTable.NameOf(slot.Take(source))) continue;
                    look.SlotNames.Add(slot.Label);
                    look.Wearables.Add(name);
                }
            }
            if (copy.appearance != null && copy.appearance.appearanceMeshes != null)
            {
                foreach (Character.AppearanceMesh mesh in copy.appearance.appearanceMeshes)
                {
                    Character.AppearanceMesh original = NpcAppearance.Entry(source, mesh.meshName);
                    look.Shapes.AddRange(SavedNpcFile.DiffShapes(mesh.blendShapeNames, mesh.blendShapeValues, original == null ? null : original.blendShapeNames, original == null ? null : original.blendShapeValues, mesh.meshName));
                }
            }
            return SavedNpcFile.NonEmptyLook(SavedNpcFile.CleanLook(look));
        }

        internal static void Write(Character copy, SavedNpcLook look)
        {
            try
            {
                if (look.Sliders) WriteSliders(copy, look);
                foreach (SavedNpcShape shape in look.Shapes) NpcAppearance.PutShape(copy, shape.Mesh, shape.Shape, shape.Weight, false);
            }
            catch (Exception e)
            {
                ApiLog.Once(ModNpcs.Owner, "ModNpcs look apply", e);
            }
        }

        internal static void Reset(Character copy, Character source, bool sliders)
        {
            if (sliders)
            {
                foreach (LookDial dial in PlayerLookTable.All) dial.SetCostume(copy, dial.GetCostume(source));
                foreach (LookSlot slot in PlayerLookTable.Slots) slot.Put(copy, slot.Take(source));
            }
            if (copy.appearance == null || copy.appearance.appearanceMeshes == null) return;
            foreach (Character.AppearanceMesh mesh in copy.appearance.appearanceMeshes)
            {
                Character.AppearanceMesh original = NpcAppearance.Entry(source, mesh.meshName);
                mesh.blendShapeNames = original == null || original.blendShapeNames == null ? null : new List<string>(original.blendShapeNames);
                mesh.blendShapeValues = original == null || original.blendShapeValues == null ? null : new List<float>(original.blendShapeValues);
            }
        }

        private static void WriteSliders(Character copy, SavedNpcLook look)
        {
            bool male = copy.GenderString == "Male";
            for (int i = 0; i < Math.Min(look.DialNames.Count, look.DialValues.Count); i++)
            {
                LookDial dial = PlayerLookTable.FindDial(look.DialNames[i]);
                if (dial != null) dial.SetCostume(copy, PlayerLookTable.Clamp(dial, look.DialValues[i]));
            }
            for (int i = 0; i < Math.Min(look.SlotNames.Count, look.Wearables.Count); i++)
            {
                LookSlot slot = PlayerLookTable.Find(look.SlotNames[i]);
                if (slot == null) continue;
                if (look.Wearables[i].Length == 0)
                {
                    slot.Put(copy, null);
                    continue;
                }
                Wearable wearable = FindWearable(slot.Label, male, look.Wearables[i]);
                if (wearable != null) slot.Put(copy, wearable);
            }
        }

        private static Wearable FindWearable(string slot, bool male, string name)
        {
            string folder = PlayerLookTable.FolderOf(slot, male);
            if (folder == null) return null;
            Wearable[] list = Resources.LoadAll<Wearable>(folder);
            int index = PlayerLookTable.IndexOf(list, name);
            return index < 0 ? null : list[index];
        }
    }
}
