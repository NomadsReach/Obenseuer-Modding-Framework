using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class SceneLookIndex
    {
        private static readonly Dictionary<string, List<SceneLookInfo>> lists = new Dictionary<string, List<SceneLookInfo>>();
        private static readonly Dictionary<string, string> failures = new Dictionary<string, string>();

        internal static List<SceneLookInfo> List(SceneLookFiles files, IList<string> scenes, string scene, out string reason)
        {
            string level = LevelOf(scenes, scene, out reason);
            if (level == null) return null;
            List<SceneLookInfo> looks;
            if (lists.TryGetValue(level, out looks)) return looks;
            string failure;
            if (!failures.TryGetValue(level, out failure))
            {
                SceneLookFileScan scan = SceneLookScan.ScanFile(files, level);
                if (scan.Error == null)
                {
                    looks = Entries(scene, level, scan);
                    lists[level] = looks;
                    return looks;
                }
                failure = level + ": " + scan.Error;
                failures[level] = failure;
            }
            reason = failure;
            return null;
        }

        internal static SceneLookInfo FindByName(SceneLookFiles files, IList<string> scenes, string scene, string name, out string reason)
        {
            List<SceneLookInfo> looks = List(files, scenes, scene, out reason);
            if (looks == null) return null;
            foreach (SceneLookInfo look in looks)
            {
                if (string.Equals(look.Name, name, StringComparison.OrdinalIgnoreCase)) return look;
                if (look.HasCharacter && string.Equals(look.CharacterName, name, StringComparison.OrdinalIgnoreCase)) return look;
            }
            reason = "no look named " + name + " in " + scene;
            return null;
        }

        internal static SceneLookInfo FindByCharacter(SceneLookFiles files, IList<string> scenes, int characterId, out string reason)
        {
            string firstFailure = null;
            foreach (string scene in scenes)
            {
                string failure;
                List<SceneLookInfo> looks = List(files, scenes, scene, out failure);
                if (looks == null)
                {
                    if (firstFailure == null) firstFailure = failure;
                    continue;
                }
                foreach (SceneLookInfo look in looks)
                {
                    if (look.HasCharacter && look.CharacterId == characterId)
                    {
                        reason = null;
                        return look;
                    }
                }
            }
            reason = "no look linked to character " + characterId + (firstFailure == null ? string.Empty : "; " + firstFailure);
            return null;
        }

        internal static SceneLookInfo FindByCharacter(SceneLookFiles files, IList<string> scenes, string scene, int characterId, out string reason)
        {
            List<SceneLookInfo> looks = List(files, scenes, scene, out reason);
            if (looks == null) return null;
            foreach (SceneLookInfo look in looks)
            {
                if (look.HasCharacter && look.CharacterId == characterId)
                {
                    reason = null;
                    return look;
                }
            }
            reason = "no look linked to character " + characterId + " in " + scene;
            return null;
        }

        internal static SceneLookInfo Root(SceneLookFiles files, IList<string> scenes, string scene, string root, out string reason)
        {
            List<SceneLookInfo> looks = List(files, scenes, scene, out reason);
            if (looks == null) return null;
            SceneLookInfo named = null;
            int namedCount = 0;
            foreach (SceneLookInfo look in looks)
            {
                if (look.Path == root) return look;
                if (look.Name != root) continue;
                named = look;
                namedCount++;
            }
            if (namedCount == 1) return named;
            reason = namedCount == 0 ? "no look " + root + " in " + scene : "more than one look named " + root + " in " + scene + ", use its path";
            return null;
        }

        internal static string LevelOf(IList<string> scenes, string scene, out string reason)
        {
            reason = null;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(scenes[i], scene, StringComparison.OrdinalIgnoreCase)) return "level" + i;
            }
            reason = "scene not in build settings: " + scene;
            return null;
        }

        internal static List<SceneLookInfo> Characters(List<SceneLookInfo> looks)
        {
            List<SceneLookInfo> found = new List<SceneLookInfo>();
            foreach (SceneLookInfo look in looks)
            {
                if (IsCharacter(look)) found.Add(look);
            }
            return found;
        }

        internal static bool IsCharacter(SceneLookInfo look)
        {
            if (look.IsNpc) return true;
            if (look.RigKind == "Bip001" || look.RigKind == "Blender-style") return true;
            return look.HasAnimator && look.Bones >= 30;
        }

        private static List<SceneLookInfo> Entries(string scene, string level, SceneLookFileScan scan)
        {
            List<SceneLookInfo> looks = new List<SceneLookInfo>();
            foreach (SceneLookRoot root in scan.Roots)
            {
                SceneLookInfo info = new SceneLookInfo { Scene = scene, Level = level, Name = root.Name, Path = root.Path, Root = root.GameObject };
                info.Renderers = root.Renderers;
                info.Bones = root.BoneNames.Count;
                info.RigKind = root.Rig;
                info.AvatarKind = root.Avatar;
                info.HasAnimator = root.HasAnimator;
                info.IsNpc = root.IsNpc;
                info.Clean = root.Clean;
                if (root.Character != null) SetCharacter(info, root.Character);
                looks.Add(info);
            }
            foreach (SceneLookNpc npc in scan.Npcs)
            {
                if (npc.Body != 0) continue;
                SceneLookInfo info = new SceneLookInfo { Scene = scene, Level = level, Name = npc.Name, Path = npc.Path, Root = npc.GameObject, RigKind = "no bones", AvatarKind = "none", IsNpc = true };
                if (npc.Character != null) SetCharacter(info, npc.Character);
                looks.Add(info);
            }
            return looks;
        }

        private static void SetCharacter(SceneLookInfo info, SceneLookCharacter character)
        {
            info.HasCharacter = true;
            info.CharacterId = character.Id;
            info.CharacterName = character.Name;
        }
    }
}
