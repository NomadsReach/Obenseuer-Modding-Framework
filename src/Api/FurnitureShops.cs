using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class FurnitureShops
    {
        private static readonly List<FurnitureShop> pending = new List<FurnitureShop>();

        internal static void Restocked(FurnitureShop shop)
        {
            FurnitureManifestStore.ForgetOffers(shop.GUID);
            Request(shop);
        }

        internal static void Request(FurnitureShop shop)
        {
            if (!FurnitureManifestStore.Ready || SaveGate.Busy)
            {
                if (!pending.Contains(shop)) pending.Add(shop);
                return;
            }
            Reconcile(shop);
        }

        internal static void Flush()
        {
            if (pending.Count == 0 || !FurnitureManifestStore.Ready || SaveGate.Busy) return;
            List<FurnitureShop> batch = new List<FurnitureShop>(pending);
            pending.Clear();
            foreach (FurnitureShop shop in batch)
            {
                if (shop != null) Reconcile(shop);
            }
        }

        internal static bool Sells(FurnitureShop shop, string furnitureId)
        {
            FurnitureEntry entry = FurnitureRegistry.Find(furnitureId);
            return entry != null && entry.ListedAt(shop.GUID);
        }

        private static void Reconcile(FurnitureShop shop)
        {
            foreach (FurnitureEntry entry in FurnitureRegistry.All())
            {
                if (!entry.ListedAt(shop.GUID) || FurnitureManifestStore.Offered(shop.GUID).Contains(entry.Id)) continue;
                Furniture furniture = FurnitureBuild.Registered(entry);
                if (furniture == null) continue;
                FurnitureManifestStore.Offer(shop.GUID, entry.Id);
                if (Stocked(shop, entry.Id)) continue;
                BuildingSystem.FurnitureInfo.Meta meta = new BuildingSystem.FurnitureInfo.Meta();
                shop.availableFurnitures.Add(new BuildingSystem.FurnitureInfo(furniture, meta, Furniture.CreateTaskItem(furniture, meta), null, entry.Definition.ShopStock));
            }
        }

        private static bool Stocked(FurnitureShop shop, string id)
        {
            foreach (BuildingSystem.FurnitureInfo info in shop.availableFurnitures)
            {
                if (info.furniture != null && info.furniture.Id == id) return true;
            }
            return false;
        }
    }
}
