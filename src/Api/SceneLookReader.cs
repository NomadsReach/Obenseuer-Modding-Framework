using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    internal static class SceneLookReader
    {
        private static readonly string[] RequiredRoles = { "Hips", "Spine", "Head", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "RightUpperLeg", "RightLowerLeg", "RightFoot", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightUpperArm", "RightLowerArm", "RightHand" };

        internal static SceneLookGameObject GameObject(SceneLookFile f, long pathId)
        {
            SceneLookGameObject cached;
            if (f.Memo.GameObjects.TryGetValue(pathId, out cached)) return cached;
            SceneLookGameObject go = ParseGameObject(pathId, f.ReadObject(pathId));
            f.Memo.GameObjects[pathId] = go;
            return go;
        }

        internal static SceneLookTransform Transform(SceneLookFile f, long pathId)
        {
            SceneLookTransform cached;
            if (f.Memo.Transforms.TryGetValue(pathId, out cached)) return cached;
            SceneLookTransform transform = ParseTransform(pathId, f.ReadObject(pathId));
            f.Memo.Transforms[pathId] = transform;
            return transform;
        }

        internal static long TransformOf(SceneLookFile f, long goPathId)
        {
            foreach (SceneLookRef component in GameObject(f, goPathId).Components)
            {
                if (component.FileId == 0 && f.Contains(component.PathId) && f.ClassId(component.PathId) == SceneLookClass.Transform) return component.PathId;
            }
            return 0;
        }

        internal static List<long> Chain(SceneLookFile f, long goPathId)
        {
            List<long> chain = new List<long>();
            long current = goPathId;
            while (current != 0 && !chain.Contains(current))
            {
                chain.Add(current);
                current = ParentOf(f, current);
            }
            return chain;
        }

        internal static List<long> SubtreeTransforms(SceneLookFile f, long goPathId)
        {
            List<long> found = new List<long>();
            List<long> stack = new List<long>();
            HashSet<long> visited = new HashSet<long>();
            long top = TransformOf(f, goPathId);
            if (top != 0) stack.Add(top);
            while (stack.Count > 0)
            {
                long current = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                if (!visited.Add(current)) continue;
                found.Add(current);
                foreach (SceneLookRef child in Transform(f, current).Children)
                {
                    if (child.FileId == 0 && f.Contains(child.PathId)) stack.Add(child.PathId);
                }
            }
            return found;
        }

        internal static List<long> Subtree(SceneLookFile f, long goPathId)
        {
            List<long> objects = new List<long>();
            foreach (long transform in SubtreeTransforms(f, goPathId)) objects.Add(Transform(f, transform).GameObject.PathId);
            return objects;
        }

        internal static string PathName(SceneLookFile f, long goPathId)
        {
            List<long> chain = Chain(f, goPathId);
            StringBuilder path = new StringBuilder();
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                if (i < chain.Count - 1) path.Append('/');
                path.Append(GameObject(f, chain[i]).Name);
            }
            return path.ToString();
        }

        internal static HashSet<string> Kinds(SceneLookFile f, long goPathId)
        {
            HashSet<string> cached;
            if (f.Memo.Kinds.TryGetValue(goPathId, out cached)) return cached;
            HashSet<string> kinds = new HashSet<string>();
            foreach (SceneLookRef component in GameObject(f, goPathId).Components)
            {
                if (component.FileId != 0 || !f.Contains(component.PathId)) continue;
                int classId = f.ClassId(component.PathId);
                if (classId == SceneLookClass.MonoBehaviour) kinds.Add(BehaviourClass(f, component.PathId));
                else if (classId == SceneLookClass.Animator) kinds.Add("Animator");
                else if (classId == SceneLookClass.SkinnedMeshRenderer) kinds.Add("SMR");
            }
            f.Memo.Kinds[goPathId] = kinds;
            return kinds;
        }

        internal static long BodyRoot(SceneLookFile f, long goPathId)
        {
            List<long> chain = Chain(f, goPathId);
            foreach (long go in chain)
            {
                if (Kinds(f, go).Contains("NPCInfo")) return go;
            }
            foreach (long go in chain)
            {
                if (Kinds(f, go).Contains("Animator")) return go;
            }
            return 0;
        }

        internal static string BehaviourClass(SceneLookFile f, long pathId)
        {
            string cached;
            if (f.Memo.BehaviourClasses.TryGetValue(pathId, out cached)) return cached;
            string cls;
            try
            {
                cls = ScriptClass(f, ReadBehaviour(new SceneLookCursor(f.ReadObject(pathId), 0), pathId).Script);
            }
            catch (Exception)
            {
                cls = "?";
            }
            f.Memo.BehaviourClasses[pathId] = cls;
            return cls;
        }

        internal static string ScriptClass(SceneLookFile f, SceneLookRef script)
        {
            SceneLookTarget target = f.Target(script);
            if (target.Error != null) return "?";
            string cached;
            if (target.File.Memo.ScriptClasses.TryGetValue(target.PathId, out cached)) return cached;
            string cls;
            try
            {
                SceneLookCursor cursor = new SceneLookCursor(target.File.ReadObject(target.PathId), 0);
                cursor.Text();
                cursor.I32();
                cursor.Skip(16);
                cls = cursor.Text();
            }
            catch (Exception)
            {
                cls = "?";
            }
            target.File.Memo.ScriptClasses[target.PathId] = cls;
            return cls;
        }

        internal static SceneLookBehaviour ReadBehaviour(SceneLookCursor c, long pathId)
        {
            SceneLookBehaviour behaviour = new SceneLookBehaviour();
            behaviour.PathId = pathId;
            behaviour.GameObject = c.Pptr();
            c.U8();
            c.Align(4);
            behaviour.Script = c.Pptr();
            behaviour.Name = c.Text();
            return behaviour;
        }

        internal static List<long> NpcBehaviours(SceneLookFile f)
        {
            List<long> list = new List<long>();
            foreach (long pathId in f.Order)
            {
                if (f.ClassId(pathId) != SceneLookClass.MonoBehaviour) continue;
                SceneLookBehaviour header;
                try
                {
                    header = ReadBehaviour(new SceneLookCursor(f.ReadObjectHead(pathId, 512), 0), pathId);
                }
                catch (Exception)
                {
                    continue;
                }
                if (ScriptClass(f, header.Script) == "NPCInfo") list.Add(pathId);
            }
            return list;
        }

        internal static SceneLookNpc ReadNpc(SceneLookFile f, long pathId)
        {
            SceneLookNpcInfo info = ParseNpcInfo(pathId, f.ReadObject(pathId));
            SceneLookNpc npc = new SceneLookNpc();
            npc.PathId = pathId;
            npc.GameObject = info.GameObject.PathId;
            npc.Name = GameObject(f, npc.GameObject).Name;
            npc.Path = PathName(f, npc.GameObject);
            SceneLookTarget character = f.Target(info.Character);
            if (character.Error == null)
            {
                try
                {
                    SceneLookCharacter head = ParseCharacter(character.File.ReadObject(character.PathId));
                    if (head.Addressable.StartsWith("Assets/", StringComparison.Ordinal)) npc.Character = head;
                    else npc.CharacterNote = "not a Character";
                }
                catch (Exception)
                {
                    npc.CharacterNote = "unreadable";
                }
            }
            SceneLookTarget model = f.Target(info.CharacterModel);
            if (model.Error == null && model.File == f && f.ClassId(model.PathId) == SceneLookClass.MonoBehaviour)
            {
                npc.ModelGameObject = ReadBehaviour(new SceneLookCursor(f.ReadObject(model.PathId), 0), model.PathId).GameObject.PathId;
            }
            return npc;
        }

        internal static SceneLookAnimator ParseAnimator(long pathId, byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookAnimator animator = new SceneLookAnimator();
            animator.PathId = pathId;
            c.Pptr();
            c.U8();
            c.Align(4);
            animator.Avatar = c.Pptr();
            animator.Controller = c.Pptr();
            return animator;
        }

        internal static List<SceneLookAnimator> Animators(SceneLookFile f, long body)
        {
            List<SceneLookAnimator> list = new List<SceneLookAnimator>();
            foreach (long go in Subtree(f, body))
            {
                foreach (SceneLookRef component in GameObject(f, go).Components)
                {
                    if (component.FileId == 0 && f.Contains(component.PathId) && f.ClassId(component.PathId) == SceneLookClass.Animator)
                    {
                        list.Add(ParseAnimator(component.PathId, f.ReadObject(component.PathId)));
                    }
                }
            }
            return list;
        }

        internal static SceneLookAvatarInfo AvatarInfo(SceneLookFile f, SceneLookAnimator animator)
        {
            SceneLookAvatarInfo info = new SceneLookAvatarInfo();
            SceneLookTarget avatar = f.Target(animator.Avatar);
            if (avatar.Error == "null")
            {
                info.Kind = "none";
            }
            else if (avatar.Error != null)
            {
                info.Kind = avatar.Error;
            }
            else
            {
                info.Roles = RoleCount(avatar.File.ReadObject(avatar.PathId));
                info.Kind = info.Roles == RequiredRoles.Length ? "humanoid" : "generic";
            }
            SceneLookTarget controller = f.Target(animator.Controller);
            if (controller.Error == null) info.ControllerName = new SceneLookCursor(controller.File.ReadObject(controller.PathId), 0).Text();
            return info;
        }

        internal static string RigKind(ICollection<string> names)
        {
            int count = names.Count;
            if (count == 0) return "no bones";
            int bip = 0;
            int blender = 0;
            foreach (string name in names)
            {
                if (name.StartsWith("Bip001", StringComparison.Ordinal)) bip++;
                if (name.IndexOf('.') >= 0) blender++;
            }
            if (2 * bip >= count) return "Bip001";
            if (2 * blender >= count) return "Blender-style";
            return "other";
        }

        internal static string BoneName(SceneLookTarget bone)
        {
            long go = Transform(bone.File, bone.PathId).GameObject.PathId;
            return GameObject(bone.File, go).Name;
        }

        internal static SceneLookCharacter ParseCharacter(byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            c.Pptr();
            c.U32();
            c.Pptr();
            SceneLookCharacter character = new SceneLookCharacter();
            character.AssetName = c.Text();
            character.Addressable = c.Text();
            character.Id = c.I32();
            character.Name = c.Text();
            character.LastName = c.Text();
            character.GenderString = c.Text();
            character.Age = c.I32();
            character.Relationship = c.Text();
            character.Job = c.Text();
            character.Sprite = c.Pptr();
            character.Prefab = c.Pptr();
            character.PrefabWithAi = c.Pptr();
            return character;
        }

        internal static SceneLookNpcInfo ParseNpcInfo(long pathId, byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookBehaviour header = ReadBehaviour(c, pathId);
            SceneLookNpcInfo info = new SceneLookNpcInfo();
            info.PathId = pathId;
            info.GameObject = header.GameObject;
            info.Guid = c.Text();
            info.Global = c.U8() != 0;
            c.Align(4);
            info.SavedTimeAndDay = c.Raw(32);
            info.Character = c.Pptr();
            info.CharacterModel = c.Pptr();
            return info;
        }

        internal static bool Contains(byte[] haystack, byte[] needle)
        {
            int last = haystack.Length - needle.Length;
            int start = 0;
            while (start <= last)
            {
                int i = Array.IndexOf(haystack, needle[0], start, last - start + 1);
                if (i < 0) return false;
                int j = 1;
                while (j < needle.Length && haystack[i + j] == needle[j]) j++;
                if (j == needle.Length) return true;
                start = i + 1;
            }
            return false;
        }

        private static int RoleCount(byte[] bytes)
        {
            int count = 0;
            foreach (string role in RequiredRoles)
            {
                if (Contains(bytes, LengthPrefixed(role))) count++;
            }
            return count;
        }

        internal static byte[] LengthPrefixed(string text)
        {
            byte[] body = Encoding.UTF8.GetBytes(text);
            byte[] result = new byte[4 + body.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(body.Length), 0, result, 0, 4);
            Buffer.BlockCopy(body, 0, result, 4, body.Length);
            return result;
        }

        private static long ParentOf(SceneLookFile f, long goPathId)
        {
            long transform = TransformOf(f, goPathId);
            if (transform == 0) return 0;
            SceneLookRef father = Transform(f, transform).Father;
            if (father.FileId != 0 || !f.Contains(father.PathId)) return 0;
            return Transform(f, father.PathId).GameObject.PathId;
        }

        private static SceneLookGameObject ParseGameObject(long pathId, byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookGameObject go = new SceneLookGameObject();
            go.PathId = pathId;
            int count = c.Count();
            go.Components = new List<SceneLookRef>(Math.Min(count, 64));
            for (int i = 0; i < count; i++) go.Components.Add(c.Pptr());
            c.I32();
            go.Name = c.Text();
            return go;
        }

        private static SceneLookTransform ParseTransform(long pathId, byte[] bytes)
        {
            SceneLookCursor c = new SceneLookCursor(bytes, 0);
            SceneLookTransform transform = new SceneLookTransform();
            transform.PathId = pathId;
            transform.GameObject = c.Pptr();
            transform.Rotation = c.Floats(4);
            transform.Position = c.Floats(3);
            transform.Scale = c.Floats(3);
            int count = c.Count();
            transform.Children = new List<SceneLookRef>(Math.Min(count, 64));
            for (int i = 0; i < count; i++) transform.Children.Add(c.Pptr());
            transform.Father = c.Pptr();
            return transform;
        }
    }
}
