using System;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class ShopsPlayerProbe
    {
        internal static void Run()
        {
            try
            {
                CoreLog log = CoreLog.For(PlayerShops.Owner);
                if (TenementController.instance != null && TenementController.instance.apartments != null)
                {
                    foreach (TenementApartment apartment in TenementController.instance.apartments)
                    {
                        string type = apartment.type == null ? "none" : apartment.type.size.ToString();
                        log.Info("probe apartment " + apartment.apartmentNumber + " type " + type + " resident " + ResidentOf(apartment));
                    }
                }
                foreach (PlayerShopManager shop in SceneIds.Active<PlayerShopManager>(FindObjectsInactive.Include))
                {
                    FurnitureManager manager = shop.manager;
                    string area = manager == null ? "none" : manager.area.ToString();
                    string managerGuid = manager == null ? "none" : manager.GUID;
                    log.Info("probe shop scene " + shop.gameObject.scene.name + " area " + area + " manager " + managerGuid + " shop " + shop.GUID + " register " + shop.data.hasCashRegister);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerShops.Owner, "player shop probe", e);
            }
        }

        private static string ResidentOf(TenementApartment apartment)
        {
            return apartment.resident == null || apartment.resident.resident == null ? "none" : apartment.resident.resident.name;
        }
    }
}
