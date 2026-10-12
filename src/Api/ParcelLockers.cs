using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class LockerInfo
    {
        public int Index { get; private set; }

        public bool IsOpen { get; private set; }

        public bool Occupied { get; private set; }

        public string ParcelName { get; private set; }

        internal LockerInfo(int index, bool isOpen, bool occupied, string parcelName)
        {
            Index = index;
            IsOpen = isOpen;
            Occupied = occupied;
            ParcelName = parcelName;
        }
    }

    public sealed class ParcelLockerInfo
    {
        public string Id { get; private set; }

        public List<LockerInfo> Lockers { get; private set; }

        internal ParcelLockerInfo(string id, List<LockerInfo> lockers)
        {
            Id = id;
            Lockers = lockers;
        }
    }

    public static class ParcelLockers
    {
        internal const string Owner = "obenseuer.moddingframework.parcellockers";
        private static readonly FieldInfo isOpenField = AccessTools.Field(typeof(ParcelLocker.Locker), "isOpen");
        private static readonly FieldInfo parcelField = AccessTools.Field(typeof(ParcelLocker.Locker), "currentParcel");

        public static List<ParcelLockerInfo> All()
        {
            List<ParcelLockerInfo> list = new List<ParcelLockerInfo>();
            try
            {
                foreach (ParcelLocker locker in UnityEngine.Object.FindObjectsByType<ParcelLocker>(FindObjectsSortMode.None))
                {
                    if (locker != null) list.Add(ToInfo(locker));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ParcelLockers.All", e);
            }
            return list;
        }

        public static ParcelLockerInfo Get(string id)
        {
            try
            {
                ParcelLocker locker = SceneIds.Find<ParcelLocker>(id);
                return locker == null ? null : ToInfo(locker);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "ParcelLockers.Get", e);
                return null;
            }
        }

        private static ParcelLockerInfo ToInfo(ParcelLocker locker)
        {
            List<LockerInfo> lockers = new List<LockerInfo>();
            if (locker.lockers != null)
            {
                for (int i = 0; i < locker.lockers.Length; i++)
                {
                    ParcelLocker.Locker slot = locker.lockers[i];
                    MailController.Mail parcel = ParcelOf(slot);
                    lockers.Add(new LockerInfo(i, IsOpen(slot), parcel != null, parcel == null || parcel.mailName == null ? "" : parcel.mailName));
                }
            }
            return new ParcelLockerInfo(SceneIds.Of(locker), lockers);
        }

        private static bool IsOpen(ParcelLocker.Locker slot)
        {
            return slot != null && isOpenField != null && (bool)isOpenField.GetValue(slot);
        }

        private static MailController.Mail ParcelOf(ParcelLocker.Locker slot)
        {
            return slot == null || parcelField == null ? null : parcelField.GetValue(slot) as MailController.Mail;
        }
    }
}
