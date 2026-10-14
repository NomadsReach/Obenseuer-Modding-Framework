using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum TenementEventKind
    {
        None,
        SpawnTrash,
        PlayMusic,
        MakeNoise,
        BreakApartment,
        Die,
        SpawnCrazyTeddies,
        Complain
    }

    // Values 0 to 3 must follow UpgradeType.ApartmentType, SizeOf casts between them.
    public enum ApartmentType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Shop = 3,
        Unknown = 4
    }

    public sealed class TenantInfo
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        internal TenantInfo(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public sealed class RentPayment
    {
        public int Oc { get; private set; }

        public float Rm { get; private set; }

        internal RentPayment(int oc, float rm)
        {
            Oc = oc;
            Rm = rm;
        }
    }

    public sealed class ApartmentInfo
    {
        public int Id { get; private set; }

        public int Floor { get; private set; }

        public string Name { get; private set; }

        public TenantInfo Tenant { get; private set; }

        public bool MovingIn { get; private set; }

        public RentPayment Rent { get; private set; }

        public bool EvictionPending { get; private set; }

        public float EvictionHoursLeft { get; private set; }

        public float MoveInHoursLeft { get; private set; }

        public ApartmentType Type { get; private set; }

        public string ManagerGuid { get; private set; }

        internal ApartmentInfo(int id, int floor, string name, TenantInfo tenant, bool movingIn, RentPayment rent, bool evictionPending, float evictionHoursLeft, float moveInHoursLeft, ApartmentType type, string managerGuid)
        {
            Id = id;
            Floor = floor;
            Name = name;
            Tenant = tenant;
            MovingIn = movingIn;
            Rent = rent;
            EvictionPending = evictionPending;
            EvictionHoursLeft = evictionHoursLeft;
            MoveInHoursLeft = moveInHoursLeft;
            Type = type;
            ManagerGuid = managerGuid;
        }
    }

    public sealed class TenantArgs
    {
        public int ApartmentId { get; private set; }

        public TenantInfo Tenant { get; private set; }

        internal TenantArgs(int apartmentId, TenantInfo tenant)
        {
            ApartmentId = apartmentId;
            Tenant = tenant;
        }
    }

    public sealed class TenementEventArgs
    {
        public string Id { get; private set; }

        public TenementEventKind Kind { get; private set; }

        public int TenantId { get; private set; }

        internal TenementEventArgs(string id, TenementEventKind kind, int tenantId)
        {
            Id = id;
            Kind = kind;
            TenantId = tenantId;
        }
    }

    public static class Housing
    {
        internal const string Owner = "obenseuer.moddingframework.housing";
        internal const string RentOwner = Owner + ".rent";
        internal const string FurnitureOwner = Owner + ".furniture";
        internal const string EvictionOwner = Owner + ".eviction";
        public const float DefaultEvictionHours = 18f;
        private const float MinEvictionHours = 1f / 3600f;
        private const float MaxEvictionHours = 24f * 365f;
        private static bool installed;
        private static bool rentInstalled;
        private static bool furnitureInstalled;

        public static event Action<TenantArgs> TenantMovedIn;

        public static event Action<TenantArgs> TenantMovedOut;

        public static event Action<RentPayment> RentPaid;

        public static event Action<TenementEventArgs> TenementEventStarted;

        public static event Action<TenantArgs> EvictionStarted;

        public static event Action<TenantArgs> EvictionCancelled;

        public static List<ApartmentInfo> AllApartments()
        {
            try
            {
                List<ApartmentInfo> list = new List<ApartmentInfo>();
                if (TenementController.instance == null || TenementController.instance.apartments == null) return list;
                foreach (TenementApartment apartment in TenementController.instance.apartments)
                {
                    if (apartment != null) list.Add(ToInfo(apartment));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "AllApartments", e);
                return new List<ApartmentInfo>();
            }
        }

        public static ApartmentInfo Get(int apartmentId)
        {
            try
            {
                TenementApartment apartment = Find(apartmentId);
                return apartment == null ? null : ToInfo(apartment);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "Get", e);
                return null;
            }
        }

        public static TenantInfo GetTenant(int apartmentId)
        {
            try
            {
                TenementApartment apartment = Find(apartmentId);
                return apartment == null ? null : ActiveTenant(apartment);
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "GetTenant", e);
                return null;
            }
        }

        public static bool StartEviction(int apartmentId)
        {
            return StartEviction(apartmentId, DefaultEvictionHours);
        }

        public static bool StartEviction(int apartmentId, float hours)
        {
            try
            {
                if (SaveGate.Busy) return false;
                TenementApartment apartment = Find(apartmentId);
                if (apartment == null || apartment.apartmentNumber == 1 || apartment.resident == null || apartment.resident.disableEvictButton || apartment.onlyFurniture || apartment.evictionStarted || apartment.moveStarted || TenementController.instance.blueprintManager == null) return false;
                TenementController.instance.StartEviction(apartment, ClampEvictionHours(hours));
                return apartment.evictionStarted;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "StartEviction", e);
                return false;
            }
        }

        public static bool CancelEviction(int apartmentId)
        {
            try
            {
                if (SaveGate.Busy) return false;
                TenementApartment apartment = Find(apartmentId);
                if (apartment == null || !apartment.evictionStarted || TenementController.instance.blueprintManager == null) return false;
                TenementController.instance.CancelEviction(apartment);
                return !apartment.evictionStarted;
            }
            catch (Exception e)
            {
                ApiLog.Once(Housing.Owner, "CancelEviction", e);
                return false;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && rentInstalled && !CorePatcher.IsDisabled(Owner) && !CorePatcher.IsDisabled(RentOwner);
            }
        }

        internal static bool FurnitureAvailable
        {
            get
            {
                return furnitureInstalled && !CorePatcher.IsDisabled(FurnitureOwner);
            }
        }

        internal static void Init()
        {
            installed = HousingPatches.Install();
            rentInstalled = HousingPatches.InstallRent();
            furnitureInstalled = HousingPatches.InstallFurniture();
            if (!installed) CoreLog.For(Owner).Warn("housing tenant hooks incomplete, tenant events are disabled");
            if (!rentInstalled) CoreLog.For(Owner).Warn("housing rent hooks incomplete, rent events are disabled");
            if (!furnitureInstalled) CoreLog.For(Owner).Warn("furniture hooks incomplete, furniture placed and removed events are disabled");
            if (!HousingPatches.InstallEviction()) CoreLog.For(Owner).Warn("eviction hooks incomplete, eviction events are disabled");
            if (!BuildingAreasPatches.Install()) CoreLog.For(Owner).Warn("building area hooks incomplete, area events are disabled");
        }

        internal static TenantInfo ActiveTenant(TenementApartment apartment)
        {
            if (apartment == null || apartment.resident == null || apartment.onlyFurniture || apartment.resident.resident == null) return null;
            return new TenantInfo(apartment.resident.resident.ID, apartment.resident.resident.name);
        }

        internal static ApartmentType SizeOf(TenementApartment apartment)
        {
            if (apartment == null) return ApartmentType.Large;
            return apartment.type == null ? ApartmentType.Unknown : (ApartmentType)apartment.type.size;
        }

        internal static string ManagerGuidOf(TenementApartment apartment)
        {
            TenementApartmentPrefabs prefabs = apartment.tenementApartmentPrefabs;
            FurnitureManager manager = prefabs == null ? null : prefabs.furnitureManager;
            return manager == null || string.IsNullOrEmpty(manager.GUID) ? null : manager.GUID;
        }

        internal static void RaiseTenantMovedIn(TenantArgs tenantEvent)
        {
            Events.Raise(TenantMovedIn, "Housing.TenantMovedIn", h => h(tenantEvent));
        }

        internal static void RaiseTenantMovedOut(TenantArgs tenantEvent)
        {
            Events.Raise(TenantMovedOut, "Housing.TenantMovedOut", h => h(tenantEvent));
        }

        internal static void RaiseRentPaid(RentPayment payment)
        {
            Events.Raise(RentPaid, "Housing.RentPaid", h => h(payment));
        }

        internal static void RaiseTenementEventStarted(TenementEventArgs tenementEvent)
        {
            Events.Raise(TenementEventStarted, "Housing.TenementEventStarted", h => h(tenementEvent));
        }

        internal static void RaiseEvictionStarted(TenantArgs tenantEvent)
        {
            Events.Raise(EvictionStarted, "Housing.EvictionStarted", h => h(tenantEvent));
        }

        internal static void RaiseEvictionCancelled(TenantArgs tenantEvent)
        {
            Events.Raise(EvictionCancelled, "Housing.EvictionCancelled", h => h(tenantEvent));
        }

        private static TenementApartment Find(int apartmentId)
        {
            return TenementController.instance == null ? null : TenementController.instance.GetApartmentFromNumber(apartmentId);
        }

        private static float ClampEvictionHours(float hours)
        {
            if (float.IsNaN(hours) || hours < MinEvictionHours) return MinEvictionHours;
            return hours > MaxEvictionHours ? MaxEvictionHours : hours;
        }

        private static ApartmentInfo ToInfo(TenementApartment apartment)
        {
            return new ApartmentInfo(apartment.apartmentNumber, apartment.apartmentFloor, "Apartment: " + apartment.apartmentNumber, ActiveTenant(apartment), apartment.moveStarted, new RentPayment(apartment.GetOcIncome(), apartment.GetRmIncome()), apartment.evictionStarted, apartment.evictionStarted ? apartment.evictionSecondsLeft / 3600f : 0f, apartment.moveStarted ? apartment.moveSecondsLeft / 3600f : 0f, SizeOf(apartment), ManagerGuidOf(apartment));
        }
    }
}
