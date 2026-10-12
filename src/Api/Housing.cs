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

        internal ApartmentInfo(int id, int floor, string name, TenantInfo tenant, bool movingIn, RentPayment rent)
        {
            Id = id;
            Floor = floor;
            Name = name;
            Tenant = tenant;
            MovingIn = movingIn;
            Rent = rent;
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
        private static bool installed;

        public static event Action<TenantArgs> TenantMovedIn;

        public static event Action<TenantArgs> TenantMovedOut;

        public static event Action<RentPayment> RentPaid;

        public static event Action<TenementEventArgs> TenementEventStarted;

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

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        internal static void Init()
        {
            installed = HousingPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("housing hooks incomplete, housing and building events are disabled");
        }

        internal static TenantInfo ActiveTenant(TenementApartment apartment)
        {
            if (apartment == null || apartment.resident == null || apartment.onlyFurniture || apartment.resident.resident == null) return null;
            return new TenantInfo(apartment.resident.resident.ID, apartment.resident.resident.name);
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

        private static TenementApartment Find(int apartmentId)
        {
            return TenementController.instance == null ? null : TenementController.instance.GetApartmentFromNumber(apartmentId);
        }

        private static ApartmentInfo ToInfo(TenementApartment apartment)
        {
            return new ApartmentInfo(apartment.apartmentNumber, apartment.apartmentFloor, "Apartment: " + apartment.apartmentNumber, ActiveTenant(apartment), apartment.moveStarted, new RentPayment(apartment.GetOcIncome(), apartment.GetRmIncome()));
        }
    }
}
