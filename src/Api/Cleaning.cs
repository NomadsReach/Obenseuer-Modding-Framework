using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum CleaningTimerState
    {
        NotStarted,
        Running,
        Done
    }

    public sealed class CleaningTimerInfo
    {
        public int ApartmentId { get; private set; }

        public CleaningTimerState State { get; private set; }

        public int CompleteDay { get; private set; }

        internal CleaningTimerInfo(int apartmentId, CleaningTimerState state, int completeDay)
        {
            ApartmentId = apartmentId;
            State = state;
            CompleteDay = completeDay;
        }
    }

    public sealed class TrashInfo
    {
        public string Id { get; private set; }

        public Vector3 Position { get; private set; }

        public List<ItemStackInfo> Items { get; private set; }

        internal TrashInfo(string id, Vector3 position, List<ItemStackInfo> items)
        {
            Id = id;
            Position = position;
            Items = items;
        }
    }

    public static class Cleaning
    {
        public static CleaningTimerInfo Timer(int apartmentId)
        {
            try
            {
                TenementApartment apartment = Find(apartmentId);
                if (apartment == null) return null;
                int day = apartment.cleaningTimerCompleteDay;
                if (day < 0) return new CleaningTimerInfo(apartmentId, CleaningTimerState.NotStarted, 0);
                if (day == 0) return new CleaningTimerInfo(apartmentId, CleaningTimerState.Done, 0);
                return new CleaningTimerInfo(apartmentId, CleaningTimerState.Running, day);
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Cleaning.Timer", e);
                return null;
            }
        }

        public static List<TrashInfo> TrashItems()
        {
            List<TrashInfo> list = new List<TrashInfo>();
            try
            {
                foreach (FurnitureTrash trash in SceneIds.Active<FurnitureTrash>())
                {
                    try
                    {
                        if (trash != null) list.Add(ToInfo(trash));
                    }
                    catch (Exception e)
                    {
                        ApiLog.Once("ObenseuerCore", "Cleaning.Entry", e);
                    }
                }
                return list;
            }
            catch (Exception e)
            {
                ApiLog.Once("ObenseuerCore", "Cleaning.TrashItems", e);
                return new List<TrashInfo>();
            }
        }

        private static TenementApartment Find(int apartmentId)
        {
            return TenementController.instance == null ? null : TenementController.instance.GetApartmentFromNumber(apartmentId);
        }

        private static TrashInfo ToInfo(FurnitureTrash trash)
        {
            List<ItemStackInfo> items = new List<ItemStackInfo>();
            if (trash.items != null)
            {
                foreach (global::ItemStack stack in trash.items)
                {
                    if (stack != null && stack.itemId != -1 && stack.itemAmount > 0) items.Add(ItemStackInfo.From(stack));
                }
            }
            return new TrashInfo(trash.GUID, trash.transform.position, items);
        }
    }
}
