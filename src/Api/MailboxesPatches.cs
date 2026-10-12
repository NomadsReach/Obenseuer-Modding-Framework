using System;

namespace ObenseuerCore
{
    internal static class MailboxesPatches
    {
        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Mailbox), "Interact", Type.EmptyTypes, null, CorePatcher.Handler(typeof(MailboxesPatches), "InteractPostfix"), null, true, Mailboxes.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(Mailboxes.Owner).Error("mailbox hook install failed: " + e.ToString());
                return false;
            }
        }

        private static void InteractPostfix(Mailbox __instance)
        {
            if (!Mailboxes.HasHandlers) return;
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
