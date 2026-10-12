using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class TrainTicketsPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook("BuyTicket", "BuyPrefix", "BuyPostfix");
                ok &= Hook("TakeTicket", null, "TakePostfix");
                return ok;
            }
            catch (Exception e)
            {
                CoreLog.For(TrainTickets.Owner).Error("install failed: " + e.ToString());
                return false;
            }
        }

        private static bool Hook(string name, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(typeof(InteractableTicketMachine), name, Type.EmptyTypes, CorePatcher.Handler(typeof(TrainTicketsPatches), prefix), CorePatcher.Handler(typeof(TrainTicketsPatches), postfix), null, false, TrainTickets.Owner);
        }

        private static void BuyPrefix(bool ___hasTicket, out bool __state)
        {
            __state = ___hasTicket;
        }

        private static void BuyPostfix(InteractableTicketMachine __instance, bool ___hasTicket, bool __state, int ___ticketPrice, Money.Currency ___currency)
        {
            if (__state || !___hasTicket) return;
            try
            {
                TrainTickets.RaiseBought(new TicketBoughtArgs(SceneIds.Of(__instance), ___ticketPrice, Cash.FromGame(___currency)));
            }
            catch (Exception e)
            {
                ApiLog.Once(TrainTickets.Owner, "TrainTickets.Bought", e);
            }
        }

        private static void TakePostfix(InteractableTicketMachine __instance, ItemReference ___ticket)
        {
            try
            {
                TrainTickets.RaiseTaken(new TicketTakenArgs(SceneIds.Of(__instance), ___ticket.ID));
            }
            catch (Exception e)
            {
                ApiLog.Once(TrainTickets.Owner, "TrainTickets.Taken", e);
            }
        }
    }
}
