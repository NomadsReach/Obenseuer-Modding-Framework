using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialogueLuaBridge
    {
        private static readonly List<Registration> modFunctions = new List<Registration>();

        internal static void RegisterCore()
        {
            try
            {
                BindCore("Core_QuestState", "QuestStateName");
                BindCore("Core_ObjectiveState", "ObjectiveStateName");
                BindCore("Core_CrimeTotal", "CrimeTotal");
                BindCore("Core_CrimeAgainst", "CrimeAgainst");
                BindCore("Core_ResponsePicked", "ResponsePicked");
                BindCore("Core_BranchPicked", "BranchPicked");
                BindCore("Core_LineText", "LineText");
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "core Lua functions", e);
            }
        }

        internal static bool IsIdentifier(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*\z");
        }

        internal static string LuaName(string modId, string name)
        {
            if (!IsIdentifier(name)) throw new ArgumentException("name must be a Lua identifier", "name");
            string prefix = Regex.Replace(modId, "[^A-Za-z0-9]", "_");
            if (prefix != modId) prefix += "_" + Hash(modId);
            return "Mod_" + prefix + "_" + name;
        }

        internal static void Check(string modId, string luaName, object target, MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (!method.IsStatic && (target == null || !method.DeclaringType.IsInstanceOfType(target))) throw new ArgumentException("target must be an instance of " + method.DeclaringType.FullName, "target");
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (!IsLuaType(parameter.ParameterType)) throw new ArgumentException("parameter " + parameter.Name + " must be float, string or bool", "method");
            }
            Registration owner = modFunctions.Find(r => r.LuaName == luaName);
            if (owner != null && owner.ModId != modId) throw new ArgumentException("Lua name " + luaName + " belongs to mod " + owner.ModId, "modId");
        }

        internal static void Bind(string modId, string luaName, object target, MethodInfo method)
        {
            Replace(luaName, target, method);
            modFunctions.RemoveAll(r => r.LuaName == luaName);
            modFunctions.Add(new Registration { ModId = modId, LuaName = luaName });
        }

        internal static void RemoveAll(string modId)
        {
            foreach (Registration registration in modFunctions.FindAll(r => r.ModId == modId))
            {
                try
                {
                    PCDS.Lua.UnregisterFunction(registration.LuaName);
                }
                catch (Exception e)
                {
                    ApiLog.Once(Dialogue.Owner, "UnregisterFunction", e);
                }
            }
            modFunctions.RemoveAll(r => r.ModId == modId);
        }

        public static string QuestStateName(string taskId)
        {
            QuestInfo quest = Quests.Get(taskId);
            return quest == null ? "" : quest.State.ToString();
        }

        public static string ObjectiveStateName(string taskId, string objectiveId)
        {
            ObjectiveStatusInfo objective = Quests.FindObjective(taskId, objectiveId);
            return objective == null ? "" : ((ObjectiveState)(int)objective.status).ToString();
        }

        public static float CrimeTotal()
        {
            return Law.Total;
        }

        public static float CrimeAgainst(float npcId)
        {
            return Law.GetAgainst((int)npcId);
        }

        public static void ResponsePicked(string key)
        {
            DialogueResponses.Picked(key);
        }

        public static void BranchPicked(string key)
        {
            DialogueBranches.Picked(key);
        }

        public static string LineText(string id)
        {
            return DialogueAuthoring.LineText(id);
        }

        private static string Hash(string text)
        {
            uint hash = 2166136261u;
            foreach (char c in text) hash = (hash ^ (uint)c) * 16777619u;
            return hash.ToString("x8");
        }

        private static void BindCore(string luaName, string method)
        {
            Replace(luaName, null, CorePatcher.Handler(typeof(DialogueLuaBridge), method));
        }

        private static void Replace(string luaName, object target, MethodInfo method)
        {
            PCDS.Lua.UnregisterFunction(luaName);
            PCDS.Lua.RegisterFunction(luaName, target, method);
        }

        private static bool IsLuaType(Type type)
        {
            return type == typeof(float) || type == typeof(string) || type == typeof(bool);
        }

        private sealed class Registration
        {
            internal string ModId;
            internal string LuaName;
        }
    }
}
