using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PCDS = PixelCrushers.DialogueSystem;

namespace ObenseuerCore
{
    internal static class DialoguePatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(PCDS.DialogueSystemController), "StartConversation", new[] { typeof(string), typeof(Transform), typeof(Transform), typeof(int), typeof(PCDS.IDialogueUI), typeof(List<PCDS.ActorOverride>) }, "StartPrefix", null, true);
                Hook(typeof(PCDS.DialogueSystemController), "OnConversationStart", new[] { typeof(Transform) }, null, "StartedPostfix", false);
                Hook(typeof(PCDS.DialogueSystemController), "OnConversationEnd", new[] { typeof(Transform) }, null, "EndedPostfix", false);
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix, bool core)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(DialoguePatches), prefix), CorePatcher.Handler(typeof(DialoguePatches), postfix), null, core, Dialogue.Owner);
        }

        private static bool StartPrefix(ref string title, Transform actor, Transform conversant)
        {
            try
            {
                return Dialogue.RunInterceptors(ref title, actor, conversant);
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "StartConversation prefix", e);
                return true;
            }
        }

        private static void StartedPostfix()
        {
            try
            {
                Dialogue.OnStarted();
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "started postfix", e);
            }
        }

        private static void EndedPostfix()
        {
            try
            {
                Dialogue.OnEnded();
            }
            catch (Exception e)
            {
                ApiLog.Once(Dialogue.Owner, "ended postfix", e);
            }
        }
    }
}
