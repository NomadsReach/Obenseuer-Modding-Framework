using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum NpcShapeKind { None, Sliders, BlendShapes }

    public sealed class NpcBlendShapeInfo
    {
        public string Mesh { get; private set; }
        public string Shape { get; private set; }
        public float Weight { get; private set; }

        internal NpcBlendShapeInfo(string mesh, string shape, float weight)
        {
            Mesh = mesh;
            Shape = shape;
            Weight = weight;
        }
    }

    public static partial class NpcAppearance
    {
        private static readonly List<LookEdit> looks = new List<LookEdit>();
        private static readonly List<ShapeEdit> shapes = new List<ShapeEdit>();

        public static NpcShapeKind ShapeKind(int npcId)
        {
            try
            {
                return KindOf(Find(npcId));
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.ShapeKind", e);
                return NpcShapeKind.None;
            }
        }

        public static PlayerLook.Look GetLook(int npcId)
        {
            try
            {
                Appearance ap = SlidersOf(Find(npcId));
                return ap == null ? null : PlayerLook.Capture(ap);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.GetLook", e);
                return null;
            }
        }

        public static bool SetLook(string modId, int npcId, PlayerLook.Look look)
        {
            ModIds.Require(modId, "modId");
            if (look == null) throw new ArgumentNullException("look");
            try
            {
                Appearance ap = SlidersOf(Find(npcId));
                if (ap == null || looks.Exists(x => x.NpcId == npcId && x.ModId != modId)) return false;
                LookEdit edit = looks.Find(x => x.ModId == modId && x.NpcId == npcId);
                PlayerLook.Look before = edit == null ? PlayerLook.Capture(ap) : edit.Before;
                Character original = edit == null ? ap.createdCharacter : edit.Original;
                if (before == null || !WriteLook(npcId, Merge(look, ap), ap)) return false;
                if (edit == null)
                {
                    edit = new LookEdit { ModId = modId, NpcId = npcId, Appearance = ap, Before = before, Original = original };
                    looks.Add(edit);
                }
                edit.Costume = ap.createdCharacter;
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.SetLook", e);
                return false;
            }
        }

        public static List<NpcBlendShapeInfo> BlendShapes(int npcId)
        {
            List<NpcBlendShapeInfo> list = new List<NpcBlendShapeInfo>();
            try
            {
                foreach (SkinnedMeshRenderer renderer in Skinned(Find(npcId)))
                {
                    if (!renderer.gameObject.activeSelf) continue;
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        list.Add(new NpcBlendShapeInfo(renderer.gameObject.name, renderer.sharedMesh.GetBlendShapeName(i), renderer.GetBlendShapeWeight(i)));
                    }
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.BlendShapes", e);
            }
            return list;
        }

        public static bool SetBlendShape(string modId, int npcId, string mesh, string shape, float weight)
        {
            ModIds.Require(modId, "modId");
            RequireMesh(mesh);
            RequireShape(shape);
            if (float.IsNaN(weight) || float.IsInfinity(weight)) throw new ArgumentException("weight must be a finite number", "weight");
            if (NpcShapeRules.Refused(shape) || SaveGate.Busy) return false;
            try
            {
                NPCInfo npc = Find(npcId);
                if (KindOf(npc) != NpcShapeKind.BlendShapes) return false;
                List<SkinnedMeshRenderer> targets = Skinned(npc).FindAll(r => r.gameObject.name == mesh && r.gameObject.activeSelf && r.sharedMesh.GetBlendShapeIndex(shape) >= 0);
                if (targets.Count == 0 || shapes.Exists(x => x.ModId != modId && x.NpcId == npcId && x.Mesh == mesh && x.Shape == shape)) return false;
                Character clone = ModNpcs.CloneOf(npcId);
                ShapeEdit edit = shapes.Find(x => x.ModId == modId && x.NpcId == npcId && x.Mesh == mesh && x.Shape == shape);
                if (edit == null)
                {
                    edit = NewShapeEdit(modId, npcId, mesh, shape, clone);
                    shapes.Add(edit);
                }
                float value = NpcShapeRules.ClampWeight(weight);
                foreach (SkinnedMeshRenderer renderer in targets)
                {
                    int index = renderer.sharedMesh.GetBlendShapeIndex(shape);
                    if (!edit.Saves.Exists(s => s.Renderer == renderer)) edit.Saves.Add(new ShapeSave { Renderer = renderer, Index = index, Weight = renderer.GetBlendShapeWeight(index) });
                    renderer.SetBlendShapeWeight(index, value);
                }
                if (clone != null) PutShape(clone, mesh, shape, value, true);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.SetBlendShape", e);
                return false;
            }
        }

        private static bool RevertLooksAndShapes(string modId, int? npcId)
        {
            List<LookEdit> lookMine = looks.FindAll(x => x.ModId == modId && (npcId == null || x.NpcId == npcId.Value));
            List<ShapeEdit> shapeMine = shapes.FindAll(x => x.ModId == modId && (npcId == null || x.NpcId == npcId.Value));
            if (SaveGate.Busy) return false;
            foreach (LookEdit edit in lookMine) RevertLook(edit);
            for (int i = shapeMine.Count - 1; i >= 0; i--) RevertShape(shapeMine[i]);
            return lookMine.Count + shapeMine.Count > 0;
        }

        private static bool ForgetLooks(string modId, int? npcId)
        {
            bool forgot = false;
            foreach (ModNpcInfo npc in ModNpcs.List(modId))
            {
                if (npcId == null || npc.CharacterId == npcId.Value) forgot = ModNpcs.ForgetLook(modId, npc.Id) || forgot;
            }
            return forgot;
        }

        internal static void DropNpc(int npcId)
        {
            foreach (LookEdit edit in looks.FindAll(x => x.NpcId == npcId)) PlayerLook.Release(edit.Costume);
            looks.RemoveAll(x => x.NpcId == npcId);
            shapes.RemoveAll(x => x.NpcId == npcId);
        }

        private static void PruneLooksAndShapes()
        {
            looks.RemoveAll(x =>
            {
                if (x.Appearance != null) return false;
                PlayerLook.Release(x.Costume);
                return true;
            });
            shapes.RemoveAll(x =>
            {
                x.Saves.RemoveAll(s => s.Renderer == null);
                return x.Saves.Count == 0;
            });
        }

        private static void RevertLook(LookEdit edit)
        {
            if (edit.Appearance != null)
            {
                if (!WriteLook(edit.NpcId, edit.Before, edit.Appearance)) return;
                Character written = edit.Appearance.createdCharacter;
                edit.Appearance.createdCharacter = edit.Original;
                PlayerLook.Release(written);
            }
            looks.Remove(edit);
        }

        private static void RevertShape(ShapeEdit edit)
        {
            shapes.Remove(edit);
            foreach (ShapeSave save in edit.Saves) save.Renderer.SetBlendShapeWeight(save.Index, save.Weight);
            Character clone = ModNpcs.CloneOf(edit.NpcId);
            if (clone == null) return;
            if (!edit.EntryListed) Record(clone, edit.Mesh, false, null);
            else if (edit.ShapeListed) PutShape(clone, edit.Mesh, edit.Shape, edit.ShapeValue, true);
            else UnrecordShape(clone, edit.Mesh, edit.Shape);
        }

        private static ShapeEdit NewShapeEdit(string modId, int npcId, string mesh, string shape, Character clone)
        {
            ShapeEdit edit = new ShapeEdit { ModId = modId, NpcId = npcId, Mesh = mesh, Shape = shape };
            Character.AppearanceMesh entry = clone == null ? null : Entry(clone, mesh);
            edit.EntryListed = entry != null;
            if (entry == null || entry.blendShapeNames == null || entry.blendShapeValues == null) return edit;
            int index = entry.blendShapeNames.IndexOf(shape);
            edit.ShapeListed = index >= 0;
            if (index >= 0) edit.ShapeValue = entry.blendShapeValues[index];
            return edit;
        }

        internal static void PutShape(Character clone, string mesh, string shape, float weight, bool createEntry)
        {
            if (createEntry) Record(clone, mesh, true, null);
            Character.AppearanceMesh entry = Entry(clone, mesh);
            if (entry == null) return;
            if (entry.blendShapeNames == null || entry.blendShapeValues == null)
            {
                entry.blendShapeNames = new List<string>();
                entry.blendShapeValues = new List<float>();
            }
            int index = entry.blendShapeNames.IndexOf(shape);
            if (index < 0)
            {
                entry.blendShapeNames.Add(shape);
                entry.blendShapeValues.Add(weight);
            }
            else entry.blendShapeValues[index] = weight;
        }

        private static void UnrecordShape(Character clone, string mesh, string shape)
        {
            Character.AppearanceMesh entry = Entry(clone, mesh);
            if (entry == null || entry.blendShapeNames == null || entry.blendShapeValues == null) return;
            int index = entry.blendShapeNames.IndexOf(shape);
            if (index < 0) return;
            entry.blendShapeNames.RemoveAt(index);
            entry.blendShapeValues.RemoveAt(index);
        }

        private static PlayerLook.Look Merge(PlayerLook.Look look, Appearance ap)
        {
            PlayerLook.Look merged = PlayerLook.Capture(ap);
            foreach (LookDial dial in PlayerLookTable.All) dial.SetLook(merged, PlayerLookTable.Clamp(dial, dial.GetLook(look)));
            foreach (LookSlot slot in PlayerLookTable.Slots)
            {
                string name = slot.Get(look);
                if (PlayerLookTable.IndexOf(slot.Choices(ap, merged), name) >= 0) slot.Set(merged, name);
            }
            return merged;
        }

        private static bool WriteLook(int npcId, PlayerLook.Look look, Appearance ap)
        {
            if (SaveGate.Busy) return false;
            Character clone = ModNpcs.CloneOf(npcId);
            if (clone == null) return PlayerLook.Apply(look, ap);
            PlayerLook.WriteCostume(clone, look, ap, true);
            ap.LoadCharacter();
            return true;
        }

        private static Appearance SlidersOf(NPCInfo npc)
        {
            Appearance ap = npc == null ? null : npc.GetComponentInChildren<Appearance>(true);
            if (ap == null || !ap.isActiveAndEnabled || !PlayerLook.IsReady(ap)) return null;
            return ap;
        }

        private static NpcShapeKind KindOf(NPCInfo npc)
        {
            if (SlidersOf(npc) != null) return NpcShapeKind.Sliders;
            CharacterAppearance baked = npc == null ? null : npc.GetComponentInChildren<CharacterAppearance>(true);
            return baked != null && baked.isActiveAndEnabled ? NpcShapeKind.BlendShapes : NpcShapeKind.None;
        }

        private static List<SkinnedMeshRenderer> Skinned(NPCInfo npc)
        {
            List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
            foreach (Renderer renderer in Renderers(npc))
            {
                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null && skinned.sharedMesh != null) list.Add(skinned);
            }
            return list;
        }

        private static void RequireShape(string shape)
        {
            if (string.IsNullOrEmpty(shape)) throw new ArgumentException("shape must be a non-empty string", "shape");
        }

        private sealed class LookEdit
        {
            internal string ModId;
            internal int NpcId;
            internal Appearance Appearance;
            internal PlayerLook.Look Before;
            internal Character Original;
            internal Character Costume;
        }

        private sealed class ShapeEdit
        {
            internal string ModId;
            internal int NpcId;
            internal string Mesh;
            internal string Shape;
            internal bool EntryListed;
            internal bool ShapeListed;
            internal float ShapeValue;
            internal List<ShapeSave> Saves = new List<ShapeSave>();
        }

        private sealed class ShapeSave
        {
            internal SkinnedMeshRenderer Renderer;
            internal int Index;
            internal float Weight;
        }
    }

    internal static class NpcShapeRules
    {
        internal const float MaxWeight = 200f;
        private const string GameLidShape = "_01UpperLidLower";

        internal static bool Refused(string shape)
        {
            return shape == GameLidShape;
        }

        internal static float ClampWeight(float weight)
        {
            return Math.Max(0f, Math.Min(MaxWeight, weight));
        }
    }
}
