using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class HotelsPatches
    {
        internal static bool Install()
        {
            try
            {
                bool ok = Hook(typeof(HotelRoom), "RentRoomPlayer", new[] { typeof(int), typeof(float) }, null, "RentPostfix");
                ok &= Hook(typeof(HotelRoom), "CountDays", new[] { typeof(bool).MakeByRefType() }, "ExpiryPrefix", "ExpiryPostfix");
                return ok;
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "install", e);
                return false;
            }
        }

        private static bool Hook(Type type, string name, Type[] args, string prefix, string postfix)
        {
            return CorePatcher.PatchCore(type, name, args, CorePatcher.Handler(typeof(HotelsPatches), prefix), CorePatcher.Handler(typeof(HotelsPatches), postfix), null, true, Hotels.Owner);
        }

        private static void RentPostfix(HotelRoom __instance, int nights, bool __result)
        {
            if (!__result) return;
            try
            {
                Hotels.RaiseRoomRented(new RoomRentedArgs(SceneIds.Of(__instance), __instance.roomName, nights));
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "RentRoomPlayer postfix", e);
            }
        }

        private static void ExpiryPrefix(HotelRoom __instance, out bool __state)
        {
            __state = __instance.rentedForPlayer;
        }

        private static void ExpiryPostfix(HotelRoom __instance, bool __state)
        {
            if (!__state || __instance.rentedForPlayer) return;
            try
            {
                Hotels.RaiseRoomExpired(new RoomExpiredArgs(SceneIds.Of(__instance), __instance.roomName));
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "CountDays postfix", e);
            }
        }
    }
}
