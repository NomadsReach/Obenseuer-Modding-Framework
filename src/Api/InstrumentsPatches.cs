using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class InstrumentsPatches
    {
        private const string GuitarPositive = "Generic/Guitar/Guitar Positive";
        private const string GuitarNegative = "Generic/Guitar/Guitar Negative";
        private static bool guitarUpdating;

        internal static bool Install()
        {
            try
            {
                bool played = Hook(typeof(global::WieldableItem.WieldableGuitar), "onPrimaryUse", Type.EmptyTypes, "PlayPrefix", "PlayPostfix");
                bool commented = Hook(typeof(NPCRandomTalk), "OnGuitarPlayed", new[] { typeof(bool) }, null, "CommentPostfix");
                bool updating = Hook(typeof(global::WieldableItem.WieldableGuitar), "Update", Type.EmptyTypes, "UpdatePrefix", "UpdatePostfix", "UpdatePostfix");
                bool bark = Hook(typeof(CharacterBarkStarter), "TryToComment", new[] { typeof(string) }, null, "BareCommentPostfix");
                return played && commented && updating && bark;
            }
            catch (Exception e)
            {
                CorePatcher.DisableOwner(Instruments.Owner);
                ApiLog.Once(Instruments.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, string finalizer = null)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(InstrumentsPatches), prefix), CorePatcher.Handler(typeof(InstrumentsPatches), postfix), CorePatcher.Handler(typeof(InstrumentsPatches), finalizer), true, Instruments.Owner);
        }

        private static void PlayPrefix(global::WieldableItem.WieldableGuitar __instance, out bool __state)
        {
            __state = false;
            try
            {
                __state = Instruments.HasPlayedHandlers && Instruments.CanPlay(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Instruments.Owner, "onPrimaryUse prefix", e);
            }
        }

        private static void PlayPostfix(global::WieldableItem.WieldableGuitar __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                Instruments.RaisePlayed(new GuitarPlayedArgs(SceneIds.Of(__instance)));
            }
            catch (Exception e)
            {
                ApiLog.Once(Instruments.Owner, "onPrimaryUse postfix", e);
            }
        }

        private static void UpdatePrefix()
        {
            guitarUpdating = true;
        }

        private static void UpdatePostfix()
        {
            guitarUpdating = false;
        }

        private static void CommentPostfix(NPCRandomTalk __instance, bool positive)
        {
            if (!Instruments.HasCommentedHandlers) return;
            try
            {
                RaiseComment(__instance, positive);
            }
            catch (Exception e)
            {
                ApiLog.Once(Instruments.Owner, "OnGuitarPlayed postfix", e);
            }
        }

        private static void BareCommentPostfix(CharacterBarkStarter __instance, string _conversation)
        {
            if (!guitarUpdating || !Instruments.HasCommentedHandlers || __instance.GetComponent<NPCRandomTalk>() != null) return;
            bool positive = _conversation == GuitarPositive;
            if (!positive && _conversation != GuitarNegative) return;
            try
            {
                RaiseComment(__instance, positive);
            }
            catch (Exception e)
            {
                ApiLog.Once(Instruments.Owner, "TryToComment postfix", e);
            }
        }

        private static void RaiseComment(Component witness, bool positive)
        {
            NPCController npc = witness.GetComponentInParent<NPCController>();
            Instruments.RaiseCommented(new GuitarCommentArgs(SceneIds.Of(witness), npc == null ? 0 : npc.ID, positive));
        }
    }
}
