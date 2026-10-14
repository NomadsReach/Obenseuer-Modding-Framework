using System;

namespace ObenseuerCore
{
    internal static class MailboxesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Mailbox), "Interact", Type.EmptyTypes, CorePatcher.Handler(typeof(MailboxesPatches), "InteractPrefix"), CorePatcher.Handler(typeof(MailboxesPatches), "InteractPostfix"), null, true, Mailboxes.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Mailboxes.Owner).Error("mailbox hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void InteractPrefix(Mailbox __instance, out bool __state)
        {
            __state = false;
            if (!Mailboxes.HasHandlers) return;
            try
            {
                __state = Mailboxes.CountOf(__instance) > 0;
            }
            catch (Exception e)
            {
                ApiLog.Once(Mailboxes.Owner, "Mailboxes.Used", e);
            }
        }

        private static void InteractPostfix(Mailbox __instance, bool __state)
        {
            if (!__state) return;
            try
            {
                Mailboxes.RaiseUsed(__instance);
            }
            catch (Exception e)
            {
                ApiLog.Once(Mailboxes.Owner, "Mailboxes.Used", e);
            }
        }
    }
}
