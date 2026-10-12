using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class FloorItemsSelfTest
    {
        internal static string Run()
        {
            try
            {
                MethodInfo load = FloorItems.ResourcesLoad();
                FieldInfo path = typeof(InstantiatedPrefab).GetField("PrefabPath");
                FieldInfo other = typeof(InstantiatedPrefab).GetField("ID");
                if (load == null || path == null || other == null) return "reflection lookup failed";
                CodeInstruction ldloc = new CodeInstruction(OpCodes.Ldloc_0);
                CodeInstruction read = new CodeInstruction(OpCodes.Ldfld, path);
                CodeInstruction call = new CodeInstruction(OpCodes.Call, load);
                CodeInstruction isinst = new CodeInstruction(OpCodes.Isinst, typeof(GameObject));
                if (FloorItems.LoadSites(Code(ldloc, read, call, isinst), typeof(InstantiatedPrefab), "PrefabPath", load).Count != 1) return "one site not matched";
                if (FloorItems.LoadSites(Code(ldloc, new CodeInstruction(OpCodes.Ldfld, other), call, isinst), typeof(InstantiatedPrefab), "PrefabPath", load).Count != 0) return "other field matched";
                if (FloorItems.LoadSites(Code(ldloc, call, isinst), typeof(InstantiatedPrefab), "PrefabPath", load).Count != 0) return "call without field matched";
                if (FloorItems.LoadSites(Code(ldloc, read, call, ldloc, read, call), typeof(InstantiatedPrefab), "PrefabPath", load).Count != 2) return "two sites not counted";
                if (FloorItems.LoadSites(Code(ldloc, read, call, isinst), typeof(InstantiatedPrefab), "PrefabPath", null).Count != 0) return "missing Load not refused";

                FieldInfo real = typeof(InstantiatedPrefab).GetField("PrefabPath");
                List<CodeInstruction> rewritten = new List<CodeInstruction>(FloorItems.Transpile(Code(ldloc, new CodeInstruction(OpCodes.Ldfld, real), new CodeInstruction(OpCodes.Call, load), isinst)));
                if (rewritten.Count != 4 || rewritten[1].opcode != OpCodes.Call || !(rewritten[1].operand is MethodInfo) || ((MethodInfo)rewritten[1].operand).Name != "Resolve") return "site not rewritten to Resolve";
                if (rewritten[2].opcode != OpCodes.Nop || rewritten[3].opcode != OpCodes.Isinst) return "load call not replaced by nop";
                return "floor item site match ok";
            }
            catch (Exception e)
            {
                return "floor item self-check threw: " + e.GetType().Name + ": " + e.Message;
            }
        }

        private static List<CodeInstruction> Code(params CodeInstruction[] items)
        {
            return new List<CodeInstruction>(items);
        }
    }
}
