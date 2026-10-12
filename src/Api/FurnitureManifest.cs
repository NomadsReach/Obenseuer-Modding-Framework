using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    [Serializable]
    public sealed class FurnitureShopRecord
    {
        public string Guid;
        public List<string> Offered = new List<string>();
    }

    [Serializable]
    public sealed class FurnitureManifest
    {
        public List<FurnitureShopRecord> Shops = new List<FurnitureShopRecord>();
    }

    internal static class FurnitureManifestStore
    {
        private static readonly Dictionary<string, List<string>> offered = new Dictionary<string, List<string>>();

        internal static bool Ready;

        internal static void BeginLoad()
        {
            Ready = false;
        }

        internal static FurnitureManifest Build()
        {
            if (!Ready) return null;
            FurnitureManifest manifest = new FurnitureManifest();
            foreach (KeyValuePair<string, List<string>> shop in offered)
            {
                manifest.Shops.Add(new FurnitureShopRecord { Guid = shop.Key, Offered = new List<string>(shop.Value) });
            }
            return manifest;
        }

        internal static void Load(FurnitureManifest manifest)
        {
            offered.Clear();
            if (manifest != null && manifest.Shops != null)
            {
                foreach (FurnitureShopRecord shop in manifest.Shops)
                {
                    if (shop != null && shop.Guid != null) offered[shop.Guid] = shop.Offered ?? new List<string>();
                }
            }
            Ready = true;
            FurnitureShops.Flush();
            FurnitureRegistry.RaiseLoaded();
        }

        internal static List<string> Offered(string shopGuid)
        {
            List<string> ids;
            if (!offered.TryGetValue(shopGuid, out ids))
            {
                ids = new List<string>();
                offered[shopGuid] = ids;
            }
            return ids;
        }

        internal static void Offer(string shopGuid, string id)
        {
            Offered(shopGuid).Add(id);
        }

        internal static void ForgetOffers(string shopGuid)
        {
            List<string> ids;
            if (shopGuid != null && offered.TryGetValue(shopGuid, out ids)) ids.Clear();
        }
    }
}
