using System;
using System.Reflection;
using HarmonyLib;
using NPC;

namespace ObenseuerCore
{
    public static class Tenants
    {
        internal const string Owner = "obenseuer.moddingframework.tenants";
        private const float MaxMoveInHours = 24f * 365f;
        private static readonly FieldInfo menuDisabledField = AccessTools.Field(typeof(GameUIController), "gameMenuDisabled");

        public static bool CanRent(int npcId)
        {
            try
            {
                TenementResident resident = ResidentOf(npcId);
                return resident != null && !TenementController.instance.ResidentRented(resident.resident.ID, true);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CanRent", e);
                return false;
            }
        }

        public static bool Rent(string modId, int npcId, int apartmentId, float moveInHours, bool silent)
        {
            if (!ValidMod(modId) || SaveGate.Busy) return false;
            if (float.IsNaN(moveInHours) || moveInHours < 0f || moveInHours > MaxMoveInHours)
            {
                ApiLog.Once(Owner, "moveInHours", "moveInHours must be 0 to " + MaxMoveInHours);
                return false;
            }
            try
            {
                TenementController tenements = TenementController.instance;
                TenementApartment apartment = Find(apartmentId);
                TenementResident resident = ResidentOf(npcId);
                if (resident == null || apartment == null || apartment.apartmentNumber == 1 || apartment.resident != null || tenements.blueprintManager == null || tenements.residentOpen) return false;
                if (tenements.ResidentRented(resident.resident.ID, true)) return false;
                if (resident.CheckRequirements(apartment) != TenementResident.CantRentReason.None) return false;
                tenements.RentApartment(resident, apartment, moveInHours, silent);
                return apartment.resident == resident;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Rent", e);
                return false;
            }
        }

        public static bool TriggerEvent(string modId, int npcId, TenementEventKind kind)
        {
            if (!ValidMod(modId) || SaveGate.Busy) return false;
            if (!Enum.IsDefined(typeof(TenementEventKind), kind))
            {
                ApiLog.Once(Owner, "TriggerEvent kind", "kind must be a TenementEventKind value");
                return false;
            }
            try
            {
                TenementResident resident = ResidentOf(npcId);
                if (resident == null || resident.tenementEvents == null || TenementEventController.instance == null || !MovedIn(resident)) return false;
                TenementEvent.EventType type = (TenementEvent.EventType)(int)kind;
                foreach (TenementEvent tenementEvent in resident.tenementEvents.tenementEvents)
                {
                    if (tenementEvent == null || tenementEvent.type != type || !tenementEvent.CanTrigger()) continue;
                    tenementEvent.OnTrigger(resident.resident.ID);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TriggerEvent", e);
                return false;
            }
        }

        public static bool OpenRentScreen(string modId, int npcId)
        {
            if (!ValidMod(modId) || SaveGate.Busy) return false;
            TenementController tenements = TenementController.instance;
            try
            {
                GameUIController ui = GameUIController.instance;
                TenementResident resident = ResidentOf(npcId);
                if (resident == null || ui == null || tenements.blueprintManager == null || tenements.residentOpen || tenements.contractorOpen) return false;
                if (tenements.ResidentRented(resident.resident.ID, true) || tenements.tenementPanelUI == null || !tenements.tenementPanelUI.PlayerHasRequiredItem() || ScreenBusy()) return false;
                resident.StartRent();
                if (tenements.currentResident == resident && ui.IsMenuVisible) return true;
                tenements.RemoveResident();
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "OpenRentScreen", e);
                if (tenements != null && tenements.residentOpen && (GameUIController.instance == null || !GameUIController.instance.IsMenuVisible)) tenements.RemoveResident();
                return false;
            }
        }

        public static RentPayment TotalIncome()
        {
            try
            {
                if (TenementController.instance == null) return null;
                Money.money total = TenementController.instance.GetTotalIncome();
                return new RentPayment(total.OC, total.RM);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "TotalIncome", e);
                return null;
            }
        }

        public static RentPayment RawIncome()
        {
            try
            {
                if (TenementController.instance == null) return null;
                Money.money total = TenementController.instance.GetTotalRawIncome();
                return new RentPayment(total.OC, total.RM);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "RawIncome", e);
                return null;
            }
        }

        public static int GeneralUpgrade(TenementController.GeneralUpgradeCategory category)
        {
            try
            {
                return TenementController.instance == null ? -1 : TenementController.instance.GetGeneralUpgradeLevel(category);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "GeneralUpgrade", e);
                return -1;
            }
        }

        public static int ApartmentUpgrade(int apartmentId, TenementController.ApartmentUpgradeCategory category)
        {
            try
            {
                return Find(apartmentId) == null ? -1 : TenementController.instance.GetApartmentUpgradeLevelByNumber(apartmentId, category);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ApartmentUpgrade", e);
                return -1;
            }
        }

        public static int FurnitureClearPercent(int apartmentId)
        {
            try
            {
                TenementApartment apartment = Find(apartmentId);
                return apartment == null ? -1 : apartment.FurnitureClearPercent();
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "FurnitureClearPercent", e);
                return -1;
            }
        }

        private static bool MovedIn(TenementResident resident)
        {
            foreach (TenementApartment apartment in TenementController.instance.apartments)
            {
                if (apartment != null && apartment.resident != null && apartment.resident.Compare(resident) && !apartment.moveStarted && !apartment.onlyFurniture) return true;
            }
            return false;
        }

        private static bool ScreenBusy()
        {
            DialogueController dialogue = DialogueController.instance;
            GameUIController ui = GameUIController.instance;
            return menuDisabledField == null || (ui != null && (bool)menuDisabledField.GetValue(ui)) || UiState.IsMenuOpen || UiState.IsPaused || UiState.IsInputBlocked || UiState.InventoryDisabled || (BuildingSystemMenu.instance != null && BuildingSystemMenu.instance.menuIsOpen) || (dialogue != null && dialogue.DialogueActive);
        }

        private static TenementApartment Find(int apartmentId)
        {
            return TenementController.instance == null ? null : TenementController.instance.GetApartmentFromNumber(apartmentId);
        }

        private static TenementResident ResidentOf(int npcId)
        {
            if (TenementController.instance == null || NPCDirector.instance == null) return null;
            NPCData data = NPCDirector.instance.GetNPCDataByID(npcId);
            if (data == null || data.character == null || data.character.resident == null || data.character.resident.resident == null) return null;
            return data.character.resident;
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }
    }
}
