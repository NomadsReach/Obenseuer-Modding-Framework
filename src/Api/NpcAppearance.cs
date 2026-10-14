using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class NpcMeshInfo
    {
        public string Mesh { get; private set; }
        public bool Active { get; private set; }
        public List<string> Materials { get; private set; }

        internal NpcMeshInfo(string mesh, bool active, List<string> materials)
        {
            Mesh = mesh;
            Active = active;
            Materials = materials;
        }
    }

    public static partial class NpcAppearance
    {
        internal const string Owner = "obenseuer.moddingframework.npcappearance";
        private const string InstanceSuffix = " (Instance)";
        private static readonly List<Edit> edits = new List<Edit>();

        public static List<NpcMeshInfo> Meshes(int npcId)
        {
            List<NpcMeshInfo> list = new List<NpcMeshInfo>();
            try
            {
                foreach (Renderer renderer in Renderers(Find(npcId)))
                {
                    List<string> names = new List<string>();
                    foreach (Material material in renderer.sharedMaterials) names.Add(material == null ? null : material.name);
                    list.Add(new NpcMeshInfo(renderer.gameObject.name, renderer.gameObject.activeSelf, names));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.Meshes", e);
            }
            return list;
        }

        public static List<string> MaterialNames()
        {
            List<string> names = new List<string>();
            try
            {
                foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if (material != null && !string.IsNullOrEmpty(material.name) && !names.Contains(material.name)) names.Add(material.name);
                }
                names.Sort(StringComparer.Ordinal);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.MaterialNames", e);
            }
            return names;
        }

        public static bool SetMeshActive(string modId, int npcId, string mesh, bool active)
        {
            ModIds.Require(modId, "modId");
            RequireMesh(mesh);
            if (SaveGate.Busy) return false;
            try
            {
                List<Renderer> targets = Named(Find(npcId), mesh);
                if (targets.Count == 0 || Claimed(modId, targets)) return false;
                Character clone = ModNpcs.CloneOf(npcId);
                foreach (Renderer renderer in targets)
                {
                    Capture(modId, npcId, renderer, clone, mesh);
                    renderer.gameObject.SetActive(active);
                }
                if (clone != null) Record(clone, mesh, active, null);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.SetMeshActive", e);
                return false;
            }
        }

        public static bool SetMaterials(string modId, int npcId, string mesh, IList<string> materialNames)
        {
            ModIds.Require(modId, "modId");
            RequireMesh(mesh);
            if (materialNames == null || materialNames.Count == 0) throw new ArgumentException("materialNames must list at least one material", "materialNames");
            foreach (string name in materialNames)
            {
                if (string.IsNullOrEmpty(name)) throw new ArgumentException("materialNames must not hold a null or empty name", "materialNames");
            }
            if (SaveGate.Busy) return false;
            try
            {
                List<Renderer> targets = Named(Find(npcId), mesh);
                if (targets.Count == 0 || Claimed(modId, targets)) return false;
                Material[] materials = Resolve(materialNames);
                if (materials == null) return false;
                foreach (Renderer renderer in targets)
                {
                    if (!renderer.gameObject.activeSelf || renderer.sharedMaterials.Length != materials.Length) return false;
                }
                Character clone = ModNpcs.CloneOf(npcId);
                foreach (Renderer renderer in targets)
                {
                    Capture(modId, npcId, renderer, clone, mesh);
                    renderer.sharedMaterials = materials;
                }
                if (clone != null) Record(clone, mesh, true, materials);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.SetMaterials", e);
                return false;
            }
        }

        public static bool Restore(string modId, int npcId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                Prune();
                bool shaped = RevertLooksAndShapes(modId, npcId);
                List<Edit> mine = edits.FindAll(x => x.ModId == modId && x.NpcId == npcId);
                foreach (Edit edit in mine) Revert(edit);
                bool forgot = ForgetLooks(modId, npcId);
                return mine.Count > 0 || shaped || forgot;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.Restore", e);
                return false;
            }
        }

        public static bool Clear(string modId)
        {
            ModIds.Require(modId, "modId");
            try
            {
                Prune();
                bool shaped = RevertLooksAndShapes(modId, null);
                List<Edit> mine = edits.FindAll(x => x.ModId == modId);
                foreach (Edit edit in mine) Revert(edit);
                bool forgot = ForgetLooks(modId, null);
                return mine.Count > 0 || shaped || forgot;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.Clear", e);
                return false;
            }
        }

        internal static void Init()
        {
            Events.SceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(string scene)
        {
            try
            {
                Prune();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "NpcAppearance.SceneLoaded", e);
            }
        }

        private static void Prune()
        {
            edits.RemoveAll(x => x.Renderer == null);
            PruneLooksAndShapes();
        }

        private static NPCInfo Find(int npcId)
        {
            return NPCManager.instance == null ? null : NPCManager.instance.FindNpcById(npcId);
        }

        private static List<Renderer> Renderers(NPCInfo npc)
        {
            List<Renderer> list = new List<Renderer>();
            if (npc == null) return list;
            Transform root = npc.characterModel != null ? npc.characterModel.transform : npc.transform;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer) list.Add(renderer);
            }
            return list;
        }

        private static List<Renderer> Named(NPCInfo npc, string mesh)
        {
            return Renderers(npc).FindAll(r => r.gameObject.name == mesh);
        }

        private static Material[] Resolve(IList<string> names)
        {
            Material[] all = null;
            Material[] found = new Material[names.Count];
            for (int i = 0; i < found.Length; i++)
            {
                if (all == null) all = Resources.FindObjectsOfTypeAll<Material>();
                foreach (Material material in all)
                {
                    if (material != null && material.name == names[i] && !material.name.EndsWith(InstanceSuffix, StringComparison.Ordinal))
                    {
                        found[i] = material;
                        break;
                    }
                }
                if (found[i] == null)
                {
                    ApiLog.Once(Owner, "unknown material " + names[i], "no loaded material has the name " + names[i]);
                    return null;
                }
            }
            return found;
        }

        private static bool Claimed(string modId, List<Renderer> targets)
        {
            return edits.Exists(x => x.ModId != modId && targets.Contains(x.Renderer));
        }

        private static void Capture(string modId, int npcId, Renderer renderer, Character clone, string mesh)
        {
            if (edits.Exists(x => x.ModId == modId && x.Renderer == renderer)) return;
            Edit edit = new Edit { ModId = modId, NpcId = npcId, Renderer = renderer, Active = renderer.gameObject.activeSelf, Materials = renderer.sharedMaterials };
            if (clone != null)
            {
                Character.AppearanceMesh entry = Entry(clone, mesh);
                edit.Clone = clone;
                edit.Mesh = mesh;
                edit.Listed = entry != null;
                edit.ListedMaterials = entry == null ? null : entry.materials;
            }
            edits.Add(edit);
        }

        internal static Character.AppearanceMesh Entry(Character clone, string mesh)
        {
            if (clone.appearance == null || clone.appearance.appearanceMeshes == null) return null;
            return clone.appearance.appearanceMeshes.Find(m => m != null && m.meshName == mesh);
        }

        private static void Revert(Edit edit)
        {
            edits.Remove(edit);
            if (edit.Renderer == null) return;
            edit.Renderer.sharedMaterials = edit.Materials;
            edit.Renderer.gameObject.SetActive(edit.Active);
            if (edit.Clone == null) return;
            Record(edit.Clone, edit.Mesh, edit.Listed, edit.ListedMaterials);
            Character.AppearanceMesh entry = edit.Listed ? Entry(edit.Clone, edit.Mesh) : null;
            if (entry != null) entry.materials = edit.ListedMaterials;
        }

        private static void Record(Character clone, string mesh, bool listed, Material[] materials)
        {
            if (clone.appearance == null) clone.appearance = new Character.Appearance();
            if (clone.appearance.appearanceMeshes == null) clone.appearance.appearanceMeshes = new List<Character.AppearanceMesh>();
            List<Character.AppearanceMesh> list = clone.appearance.appearanceMeshes;
            if (!listed)
            {
                list.RemoveAll(m => m != null && m.meshName == mesh);
                return;
            }
            Character.AppearanceMesh entry = list.Find(m => m != null && m.meshName == mesh);
            if (entry == null) clone.appearance.AddMesh(mesh, materials);
            else if (materials != null) entry.materials = materials;
        }

        private static void RequireMesh(string mesh)
        {
            if (string.IsNullOrEmpty(mesh)) throw new ArgumentException("mesh must be a non-empty string", "mesh");
        }

        private sealed class Edit
        {
            internal string ModId;
            internal int NpcId;
            internal Renderer Renderer;
            internal bool Active;
            internal Material[] Materials;
            internal Character Clone;
            internal string Mesh;
            internal bool Listed;
            internal Material[] ListedMaterials;
        }
    }
}
