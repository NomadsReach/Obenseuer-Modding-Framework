using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum TrapCatch
    {
        Empty = 0,
        Rat = 1,
        GlowingRat = 2
    }

    [Flags]
    internal enum BreedingGroup
    {
        None = 0,
        Breed = 1,
        Planting = 2,
        Trap = 4,
        All = Breed | Planting | Trap
    }

    public sealed class BreederInfo
    {
        public string Id { get; private set; }

        public int Count { get; private set; }

        internal BreederInfo(string id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    public sealed class BredArgs
    {
        public string BreederId { get; private set; }

        public int SpeciesId { get; private set; }

        public bool OfflineCatchUp { get; private set; }

        internal BredArgs(string breederId, int speciesId, bool offlineCatchUp)
        {
            BreederId = breederId;
            SpeciesId = speciesId;
            OfflineCatchUp = offlineCatchUp;
        }
    }

    public sealed class TrapSprungArgs
    {
        public string TrapId { get; private set; }

        public TrapCatch Catch { get; private set; }

        internal TrapSprungArgs(string trapId, TrapCatch trapCatch)
        {
            TrapId = trapId;
            Catch = trapCatch;
        }
    }

    public static class Breeding
    {
        internal const string Owner = "obenseuer.moddingframework.breeding";
        internal const string PlantingOwner = Owner + ".planting";
        internal const string TrapOwner = Owner + ".trap";
        private static BreedingGroup installed;

        public static event Action<BredArgs> Bred;
        public static event Action<TrapSprungArgs> TrapSprung;

        internal static bool HasBredHandlers
        {
            get
            {
                return Bred != null;
            }
        }

        internal static bool HasTrapHandlers
        {
            get
            {
                return TrapSprung != null;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return Installed(BreedingGroup.Breed) && Installed(BreedingGroup.Planting);
            }
        }

        internal static bool Installed(BreedingGroup group)
        {
            if ((installed & group) != group) return false;
            foreach (BreedingGroup bit in new[] { BreedingGroup.Breed, BreedingGroup.Planting, BreedingGroup.Trap })
            {
                if ((group & bit) == bit && CorePatcher.IsDisabled(GroupOwner(bit))) return false;
            }
            return true;
        }

        private static string GroupOwner(BreedingGroup group)
        {
            if (group == BreedingGroup.Planting) return PlantingOwner;
            return group == BreedingGroup.Trap ? TrapOwner : Owner;
        }

        public static List<BreederInfo> All()
        {
            List<BreederInfo> list = new List<BreederInfo>();
            try
            {
                foreach (AnimalBreeder breeder in SceneIds.Active<AnimalBreeder>())
                {
                    list.Add(ToInfo(breeder));
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Breeding.Owner, "All", e);
                return new List<BreederInfo>();
            }
        }

        internal static void Init()
        {
            installed = BreedingPatches.Install();
            if (installed == BreedingGroup.All) return;
            CoreLog.For(Owner).Warn("breeding hooks incomplete, missing " + (BreedingGroup.All & ~installed) + ", each member and event whose hooks are missing is disabled");
        }

        internal static BreederInfo ToInfo(AnimalBreeder breeder)
        {
            int count = 0;
            Growing growing = breeder.GetComponent<Growing>();
            if (growing != null)
            {
                foreach (GrowingSpot spot in growing.GrowingSlots)
                {
                    if (spot != null && spot.Plant != null) count++;
                }
            }
            return new BreederInfo(SceneIds.Of(breeder), count);
        }

        internal static void RaiseBred(BredArgs args)
        {
            Events.Raise(Bred, "Breeding.Bred", h => h(args));
        }

        internal static void RaiseTrapSprung(TrapSprungArgs args)
        {
            Events.Raise(TrapSprung, "Breeding.TrapSprung", h => h(args));
        }
    }
}
