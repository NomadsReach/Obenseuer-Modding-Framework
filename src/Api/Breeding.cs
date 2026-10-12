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
        private static bool installed;

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
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<BreederInfo> All()
        {
            List<BreederInfo> list = new List<BreederInfo>();
            try
            {
                foreach (AnimalBreeder breeder in UnityEngine.Object.FindObjectsByType<AnimalBreeder>(FindObjectsSortMode.None))
                {
                    if (breeder != null) list.Add(ToInfo(breeder));
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
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("breeding hooks incomplete, breeding and trap events are disabled");
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
