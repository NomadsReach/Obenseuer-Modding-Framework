using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class HotelRoomInfo
    {
        public string Id { get; private set; }

        public string HotelId { get; private set; }

        public string Name { get; private set; }

        public bool Rented { get; private set; }

        public int DaysLeft { get; private set; }

        public bool Available { get; private set; }

        internal HotelRoomInfo(string id, string hotelId, string name, bool rented, int daysLeft, bool available)
        {
            Id = id;
            HotelId = hotelId;
            Name = name;
            Rented = rented;
            DaysLeft = daysLeft;
            Available = available;
        }
    }

    public sealed class RoomRentedArgs
    {
        public string RoomId { get; private set; }

        public string RoomName { get; private set; }

        public int Days { get; private set; }

        internal RoomRentedArgs(string roomId, string roomName, int days)
        {
            RoomId = roomId;
            RoomName = roomName;
            Days = days;
        }
    }

    public sealed class RoomExpiredArgs
    {
        public string RoomId { get; private set; }

        public string RoomName { get; private set; }

        internal RoomExpiredArgs(string roomId, string roomName)
        {
            RoomId = roomId;
            RoomName = roomName;
        }
    }

    public static class Hotels
    {
        public const int MaxDays = 7;
        internal const string Owner = "obenseuer.moddingframework.hotels";
        private static bool installed;
        private static System.Reflection.FieldInfo priceFactor;

        public static event Action<RoomRentedArgs> RoomRented;

        public static event Action<RoomExpiredArgs> RoomExpired;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<HotelRoomInfo> All()
        {
            try
            {
                List<HotelRoomInfo> list = new List<HotelRoomInfo>();
                foreach (Hotel hotel in SceneIds.Active<Hotel>())
                {
                    string hotelId = SceneIds.Of(hotel);
                    foreach (HotelRoom room in hotel.rooms)
                    {
                        if (room != null) list.Add(ToInfo(hotelId, room));
                    }
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "All", e);
                return new List<HotelRoomInfo>();
            }
        }

        public static HotelRoomInfo Get(string roomId)
        {
            try
            {
                Hotel hotel;
                int index;
                if (!TryFind(roomId, out hotel, out index)) return null;
                return ToInfo(SceneIds.Of(hotel), hotel.rooms[index]);
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "Get", e);
                return null;
            }
        }

        public static RentPayment Price(string roomId, int days)
        {
            ValidateDays(days);
            try
            {
                Hotel hotel;
                int index;
                if (!TryFind(roomId, out hotel, out index)) return null;
                Money.money price = hotel.GetRoomPrice(index, days);
                if (priceFactor != null && price.OC >= 0) price = hotel.rooms[index].rentPrice * (float)priceFactor.GetValue(hotel) * days;
                return price.OC < 0 ? null : new RentPayment(price.OC, price.RM);
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "Price", e);
                return null;
            }
        }

        public static bool Rent(string roomId, int days)
        {
            ValidateDays(days);
            if (SaveGate.Busy) return false;
            if (!Cash.Ready()) return false;
            try
            {
                Hotel hotel;
                int index;
                if (!TryFind(roomId, out hotel, out index) || hotel.PlayerHasRoom()) return false;
                return hotel.RentRoom(index, days);
            }
            catch (Exception e)
            {
                ApiLog.Once(Hotels.Owner, "Rent", e);
                return false;
            }
        }

        internal static void Init()
        {
            LoadPriceFactor();
            installed = HotelsPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("hotel hooks incomplete, hotel events are disabled");
        }

        internal static void RaiseRoomRented(RoomRentedArgs rentedEvent)
        {
            Events.Raise(RoomRented, "Hotels.RoomRented", h => h(rentedEvent));
        }

        internal static void RaiseRoomExpired(RoomExpiredArgs expiredEvent)
        {
            Events.Raise(RoomExpired, "Hotels.RoomExpired", h => h(expiredEvent));
        }


        private static void LoadPriceFactor()
        {
            try
            {
                priceFactor = AccessTools.Field(typeof(Hotel), "dailyPricefactor");
            }
            catch (Exception e)
            {
                priceFactor = null;
                ApiLog.Once(Hotels.Owner, "priceFactor", e);
            }
        }

        private static bool TryFind(string roomId, out Hotel hotel, out int index)
        {
            hotel = null;
            index = -1;
            if (string.IsNullOrEmpty(roomId)) return false;
            foreach (Hotel candidate in SceneIds.Active<Hotel>())
            {
                for (int i = 0; i < candidate.rooms.Length; i++)
                {
                    if (candidate.rooms[i] != null && SceneIds.Of(candidate.rooms[i]) == roomId)
                    {
                        hotel = candidate;
                        index = i;
                        return true;
                    }
                }
            }
            return false;
        }

        private static HotelRoomInfo ToInfo(string hotelId, HotelRoom room)
        {
            return new HotelRoomInfo(SceneIds.Of(room), hotelId, room.roomName, room.rentedForPlayer, room.reservedDays > 0 ? room.reservedDays : 0, room.reservedDays == -1);
        }

        private static void ValidateDays(int days)
        {
            if (days < 1 || days > MaxDays) throw new ArgumentException("days must be from 1 to " + MaxDays, "days");
        }
    }
}
