using System;
using System.Collections.Generic;
using System.Globalization;

namespace ObenseuerCore
{
    public enum RenovationState
    {
        Locked,
        Available,
        InProgress,
        AwaitingFinalPayment,
        Installed
    }

    public sealed class RenovationInfo
    {
        public string Id { get; private set; }

        public string Name { get; private set; }

        public int ApartmentId { get; private set; }

        public int Cost { get; private set; }

        public int FinalCost { get; private set; }

        public RenovationState State { get; private set; }

        internal RenovationInfo(string id, string name, int apartmentId, int cost, int finalCost, RenovationState state)
        {
            Id = id;
            Name = name;
            ApartmentId = apartmentId;
            Cost = cost;
            FinalCost = finalCost;
            State = state;
        }
    }

    internal sealed class RenovationEntry
    {
        internal TenementUpgrade Upgrade;

        internal TenementApartment Apartment;

        internal TenementUpgrade[] Line;

        internal RenovationInfo Info;
    }

    public static class Renovations
    {
        internal const string Owner = "obenseuer.moddingframework.renovations";
        private static bool installed;

        public static event Action<RenovationInfo> Finished;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<RenovationInfo> All()
        {
            List<RenovationInfo> list = new List<RenovationInfo>();
            try
            {
                foreach (RenovationEntry entry in Catalog())
                {
                    list.Add(entry.Info);
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "All", e);
                return new List<RenovationInfo>();
            }
        }

        public static RenovationInfo Get(string id)
        {
            try
            {
                RenovationEntry entry = Find(id);
                return entry == null ? null : entry.Info;
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "Get", e);
                return null;
            }
        }

        public static float Progress(string id)
        {
            try
            {
                RenovationEntry entry = Find(id);
                if (entry == null) return 0f;
                if (entry.Info.State == RenovationState.Installed || entry.Info.State == RenovationState.AwaitingFinalPayment) return 1f;
                if (entry.Info.State != RenovationState.InProgress) return 0f;
                TenementUpgradeProgress progress = ProgressOf(entry);
                return progress == null ? 0f : Math.Min(1f, Math.Max(0f, progress.GetCurrentProgress()));
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "Progress", e);
                return 0f;
            }
        }

        public static int Cost(string id)
        {
            try
            {
                RenovationEntry entry = Find(id);
                return entry == null ? 0 : entry.Upgrade.GetPriceStartOC();
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "Cost", e);
                return 0;
            }
        }

        public static bool Start(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ApiLog.Once(Owner, "Start.id", "id must be a non-empty renovation id");
                return false;
            }
            try
            {
                TenementController controller = TenementController.instance;
                RenovationEntry entry = Find(id);
                if (SaveGate.Busy || controller == null || entry == null || !controller.contractorOpen || controller.currentContractor == null) return false;
                if (entry.Info.State != RenovationState.Available) return false;
                TenementContractor contractor = controller.currentContractor;
                ApartmentType size = Housing.SizeOf(entry.Apartment);
                if (size == ApartmentType.Unknown || !contractor.CanUpgrade(entry.Upgrade, (UpgradeType.ApartmentType)size)) return false;
                if (!controller.ignoreUpgradePricesAndItems && !CanPay(entry, contractor)) return false;
                TenementUpgradeProgress progress = entry.Apartment == null ? controller.general.StartUpdate(entry.Upgrade) : entry.Apartment.StartUpdate(entry.Upgrade);
                if (progress == null) return false;
                Refresh(controller, entry);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "Start", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = RenovationsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("renovation hooks incomplete, renovation events are disabled");
        }

        internal static RenovationInfo Describe(TenementUpgradeProgress progress)
        {
            int number = progress.apartmentUpgrade ? progress.apartmentNumber : 0;
            return new RenovationInfo(IdOf(number, progress.upgrade), progress.upgrade.GetName(number), number, progress.upgrade.GetPriceStartOC(), progress.upgrade.GetPriceFinalRM(), RenovationState.Installed);
        }

        internal static void RaiseFinished(RenovationInfo info)
        {
            Events.Raise(Finished, "Renovations.Finished", h => h(info));
        }

        private static void Refresh(TenementController controller, RenovationEntry entry)
        {
            try
            {
                controller.RemoveSupersededMarkedUpgrades(entry.Info.ApartmentId, entry.Upgrade, entry.Line);
                if (controller.upgradePanel != null) controller.upgradePanel.CloseUpgradePanel();
                if (controller.blueprintManager != null) controller.blueprintManager.UpdateAll();
            }
            catch (Exception e)
            {
                ApiLog.Once(Renovations.Owner, "Refresh", e);
            }
        }

        private static List<RenovationEntry> Catalog()
        {
            List<RenovationEntry> entries = new List<RenovationEntry>();
            TenementController controller = TenementController.instance;
            if (controller == null) return entries;
            HashSet<string> seen = new HashSet<string>();
            if (controller.general != null) AddGroups(entries, seen, null, GeneralGroups(controller.general));
            if (controller.apartments == null) return entries;
            foreach (TenementApartment apartment in controller.apartments)
            {
                if (apartment != null && apartment.type != null) AddGroups(entries, seen, apartment, ApartmentGroups(apartment.type));
            }
            return entries;
        }

        private static void AddGroups(List<RenovationEntry> entries, HashSet<string> seen, TenementApartment apartment, List<TenementUpgradeGroup> groups)
        {
            int number = apartment == null ? 0 : apartment.apartmentNumber;
            foreach (TenementUpgradeGroup group in groups)
            {
                if (group == null || group.updateGroup == null) continue;
                foreach (TenementUpgrade upgrade in group.updateGroup)
                {
                    try
                    {
                        if (upgrade == null || string.IsNullOrEmpty(upgrade.Id) || !seen.Add(IdOf(number, upgrade))) continue;
                        RenovationEntry entry = new RenovationEntry();
                        entry.Upgrade = upgrade;
                        entry.Apartment = apartment;
                        entry.Line = group.updateGroup;
                        entry.Info = new RenovationInfo(IdOf(number, upgrade), upgrade.GetName(number), number, upgrade.GetPriceStartOC(), upgrade.GetPriceFinalRM(), StateOf(entry));
                        entries.Add(entry);
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once(Renovations.Owner, "Entry", e);
                    }
                }
            }
        }

        private static List<TenementUpgradeGroup> GeneralGroups(TenementGeneral general)
        {
            return new List<TenementUpgradeGroup>
            {
                general.electricityUpgrades, general.waterUpgrades, general.heatingUpgrades, general.stairsUpgrades, general.elevatorUpgrades, general.facadeUpgrades, general.roofUpgrades, general.basementUpgrades,
                general.saunaUpgrades, general.shroomsUpgrades, general.workshopUpgrades, general.mineUpgrades, general.courtyardUpgrades, general.fenceUpgrades, general.saunaAndPierUpgrades, general.greenhouseUpgrades
            };
        }

        private static List<TenementUpgradeGroup> ApartmentGroups(TenementApartmentType type)
        {
            return new List<TenementUpgradeGroup> { type.electricity, type.water, type.heating, type.bathroom, type.walls, type.balcony, type.balcony2, type.kitchen };
        }

        private static List<TenementUpgradeProgress> GeneralProgresses(TenementGeneral general)
        {
            return new List<TenementUpgradeProgress>
            {
                general.electricityProgress, general.waterProgress, general.heatingProgress, general.stairsProgress, general.elevatorProgress, general.facadeProgress, general.roofProgress, general.basementProgress,
                general.saunaProgress, general.shroomsProgress, general.workshopProgress, general.mineProgress, general.courtyardProgress, general.fenceProgress, general.saunaAndPierProgress, general.greenhouseProgress
            };
        }

        private static RenovationState StateOf(RenovationEntry entry)
        {
            if (entry.Upgrade.IsUpgraded(entry.Apartment)) return RenovationState.Installed;
            TenementUpgradeProgress progress = ProgressOf(entry);
            if (progress != null) return progress.upgradeReady ? RenovationState.AwaitingFinalPayment : RenovationState.InProgress;
            if (LineBusy(entry)) return RenovationState.Locked;
            return entry.Upgrade.CanUpgrade(entry.Apartment) ? RenovationState.Available : RenovationState.Locked;
        }

        private static bool LineBusy(RenovationEntry entry)
        {
            foreach (TenementUpgrade sibling in entry.Line)
            {
                if (sibling != null && ProgressFor(sibling, entry.Apartment) != null) return true;
            }
            return false;
        }

        private static TenementUpgradeProgress ProgressOf(RenovationEntry entry)
        {
            return ProgressFor(entry.Upgrade, entry.Apartment);
        }

        private static TenementUpgradeProgress ProgressFor(TenementUpgrade upgrade, TenementApartment apartment)
        {
            TenementController controller = TenementController.instance;
            foreach (TenementUpgradeProgress progress in controller.currentUpgradeProgresses)
            {
                if (progress != null && Matches(progress, upgrade, apartment)) return progress;
            }
            List<TenementUpgradeProgress> slots = apartment == null ? GeneralProgresses(controller.general) : apartment.GetApartmentProgresses();
            foreach (TenementUpgradeProgress progress in slots)
            {
                if (progress != null && progress.upgradeReady && Matches(progress, upgrade, apartment)) return progress;
            }
            return null;
        }

        private static bool Matches(TenementUpgradeProgress progress, TenementUpgrade upgrade, TenementApartment apartment)
        {
            if (!TenementUpgrade.IsSameUpgrade(progress.upgrade, upgrade)) return false;
            if (apartment == null) return progress.generalUpgrade;
            return progress.apartmentUpgrade && progress.apartmentNumber == apartment.apartmentNumber;
        }

        private static bool CanPay(RenovationEntry entry, TenementContractor contractor)
        {
            return Money.instance != null && Money.instance.CheckMoney(entry.Upgrade.GetPriceStartOC(), Money.Currency.OC) && entry.Upgrade.CanUpgrade(entry.Apartment, true, contractor);
        }

        private static RenovationEntry Find(string id)
        {
            return id == null ? null : Find(Catalog(), id);
        }

        private static RenovationEntry Find(List<RenovationEntry> entries, string id)
        {
            foreach (RenovationEntry entry in entries)
            {
                if (entry.Info.Id == id) return entry;
            }
            return null;
        }

        private static string IdOf(int number, TenementUpgrade upgrade)
        {
            return number.ToString(CultureInfo.InvariantCulture) + "/" + upgrade.Id;
        }
    }
}
