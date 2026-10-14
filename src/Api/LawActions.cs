using System;
using System.Reflection;

namespace ObenseuerCore
{
    public static class LawActions
    {
        internal const string Owner = "obenseuer.moddingframework.lawactions";
        private static readonly FieldInfo latestBribeField = typeof(Crime).GetField("latestBribe", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool CanPayFine()
        {
            Crime crime = Ready();
            if (crime == null) return false;
            try
            {
                return crime.CanPayFine();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LawActions.CanPayFine", e);
                return false;
            }
        }

        public static bool CanBribe()
        {
            Crime crime = Ready();
            if (crime == null) return false;
            try
            {
                return crime.CanBribe();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LawActions.CanBribe", e);
                return false;
            }
        }

        public static bool PayFine(string modId)
        {
            ModIds.Require(modId, "modId");
            Crime crime = Ready();
            if (crime == null) return false;
            try
            {
                return crime.CanPayFine() && crime.PayFine();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LawActions.PayFine", e);
                return false;
            }
        }

        public static bool TryBribe(string modId)
        {
            ModIds.Require(modId, "modId");
            Crime crime = Ready();
            if (crime == null) return false;
            try
            {
                if (!crime.CanBribe() || latestBribeField == null) return false;
                crime.CalculateBribe();
                if (!Money.instance.CheckMoney((int)latestBribeField.GetValue(crime))) return false;
                return crime.TryToBribe();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LawActions.TryBribe", e);
                return false;
            }
        }

        public static bool ConfiscateInventory(string modId, string containerGuid, bool clearContainer)
        {
            ModIds.Require(modId, "modId");
            Crime crime = Ready();
            ContainerInfo container = string.IsNullOrEmpty(containerGuid) ? null : Containers.FindByGuid(containerGuid);
            if (crime == null || container == null || container.Source == null || Inventory.instance == null) return false;
            try
            {
                crime.ConfiscateItems(container.Source, clearContainer, null);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "LawActions.ConfiscateInventory", e);
                return false;
            }
        }

        private static Crime Ready()
        {
            if (!Law.IsAvailable || SaveGate.Busy || Changelevel.ChangeLevelActive) return null;
            Crime crime = Crime.instance;
            return crime == null || Inventory.instance == null || crime.ArrestInProgress || crime.CountCrimes() <= 0f ? null : crime;
        }
    }
}
