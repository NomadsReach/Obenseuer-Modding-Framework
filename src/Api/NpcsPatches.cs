using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class NpcsPatches
    {
        internal static bool Install()
        {
            bool ok = false;
            try
            {
                ok = true;
                ok &= Hook(typeof(NPCManager), "AddNpcToList", new[] { typeof(NPCInfo) }, "ListedPrefix", "SpawnPostfix");
                ok &= Hook(typeof(NPCManager), "RemoveNpcFromList", new[] { typeof(NPCInfo) }, "ListedPrefix", "DespawnPostfix");
                ok &= Hook(typeof(NPCInfo), "ChangeState", new[] { typeof(NPCInfo.State) }, "DiedPrefix", "DiedPostfix");
                ok &= Hook(typeof(Relationship), "CheckDateAndTrigger", new[] { typeof(TimeAndDay), typeof(RelationshipController.RelationshipInfo) }, "RelationshipPrefix", "RelationshipPostfix");
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "install", e);
                ok = false;
            }
            if (ok) return true;
            CorePatcher.DisableOwner(Npcs.Owner);
            CoreLog.For(Npcs.Owner).Warn("npc events disabled, a game method was not patched");
            return false;
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(NpcsPatches), prefix), CorePatcher.Handler(typeof(NpcsPatches), postfix), null, false, Npcs.Owner);
        }

        private static void ListedPrefix(NPCInfo npc, out bool __state)
        {
            __state = false;
            try
            {
                __state = npc != null && NPCManager.instance != null && NPCManager.instance.GetAllNPCs().Contains(npc);
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "npc list prefix", e);
            }
        }

        private static void SpawnPostfix(NPCInfo npc, bool __state)
        {
            if (__state || npc == null) return;
            try
            {
                Npcs.RaiseSpawned(npc);
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "AddNpcToList postfix", e);
            }
        }

        private static void DespawnPostfix(NPCInfo npc, bool __state)
        {
            if (!__state) return;
            try
            {
                Npcs.RaiseDespawned(npc);
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "RemoveNpcFromList postfix", e);
            }
        }

        private static void DiedPrefix(NPCInfo __instance, out bool __state)
        {
            __state = __instance.IsDead();
        }

        private static void DiedPostfix(NPCInfo __instance, bool __state)
        {
            if (__state || !__instance.IsDead()) return;
            try
            {
                Npcs.RaiseDied(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "ChangeState postfix", e);
            }
        }

        private static void RelationshipPrefix(RelationshipController.RelationshipInfo info, out float __state)
        {
            __state = info == null ? 0f : info.disposition;
        }

        private static void RelationshipPostfix(Relationship __instance, RelationshipController.RelationshipInfo info, float __state)
        {
            if (info == null || info.disposition == __state) return;
            try
            {
                Relationships.RaiseChanged(__instance, __state, info.disposition);
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "CheckDateAndTrigger postfix", e);
            }
        }
    }
}
