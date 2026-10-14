using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class TicketMachineInfo
    {
        public string Id { get; private set; }

        public int Price { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal TicketMachineInfo(string id, int price, MoneyCurrency currency)
        {
            Id = id;
            Price = price;
            Currency = currency;
        }
    }

    public sealed class TicketBoughtArgs
    {
        public string MachineId { get; private set; }

        public int Price { get; private set; }

        public MoneyCurrency Currency { get; private set; }

        internal TicketBoughtArgs(string machineId, int price, MoneyCurrency currency)
        {
            MachineId = machineId;
            Price = price;
            Currency = currency;
        }
    }

    public sealed class TicketTakenArgs
    {
        public string MachineId { get; private set; }

        public int ItemId { get; private set; }

        internal TicketTakenArgs(string machineId, int itemId)
        {
            MachineId = machineId;
            ItemId = itemId;
        }
    }

    public static class TrainTickets
    {
        internal const string Owner = "obenseuer.moddingframework.traintickets";
        private static bool installed;
        private static FieldInfo price;
        private static FieldInfo currency;

        public static event Action<TicketBoughtArgs> Bought;
        public static event Action<TicketTakenArgs> Taken;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<TicketMachineInfo> All()
        {
            List<TicketMachineInfo> list = new List<TicketMachineInfo>();
            try
            {
                if (price == null || currency == null) return list;
                foreach (InteractableTicketMachine machine in SceneIds.Active<InteractableTicketMachine>())
                {
                    if (machine == null) continue;
                    list.Add(new TicketMachineInfo(SceneIds.Of(machine), (int)price.GetValue(machine), Cash.FromGame((Money.Currency)currency.GetValue(machine))));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TrainTickets.All", e);
            }
            return list;
        }

        internal static void Init()
        {
            installed = ResolveFields() && TrainTicketsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("ticket machine hooks incomplete, ticket events are disabled");
        }

        internal static void RaiseBought(TicketBoughtArgs args)
        {
            Events.Raise(Bought, "TrainTickets.Bought", h => h(args));
        }

        internal static void RaiseTaken(TicketTakenArgs args)
        {
            Events.Raise(Taken, "TrainTickets.Taken", h => h(args));
        }

        private static bool ResolveFields()
        {
            try
            {
                price = AccessTools.Field(typeof(InteractableTicketMachine), "ticketPrice");
                currency = AccessTools.Field(typeof(InteractableTicketMachine), "currency");
                return price != null && currency != null;
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Error("field lookup failed: " + e.ToString());
                return false;
            }
        }
    }
}
