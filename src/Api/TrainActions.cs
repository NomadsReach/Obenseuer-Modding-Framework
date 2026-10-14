using System;
using UnityEngine;

namespace ObenseuerCore
{
    public enum TrainEnd
    {
        Front = 0,
        Back = 1
    }

    public static class TrainActions
    {
        internal const string Owner = "obenseuer.moddingframework.trainactions";

        public static bool SetEngine(string modId, string trainId, bool on)
        {
            try
            {
                TrackTrain train = Target(modId, trainId);
                if (train == null) return false;
                Minitrain minitrain = MinitrainOf(train);
                if (minitrain != null)
                {
                    if (on) minitrain.StartMinitrain();
                    else minitrain.StopMinitrain();
                }
                else train.Toggle(on);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TrainActions.SetEngine", e);
                return false;
            }
        }

        public static bool SetTargetSpeed(string modId, string trainId, float speed)
        {
            try
            {
                TrackTrain train = Target(modId, trainId);
                if (train == null || float.IsNaN(speed) || float.IsInfinity(speed)) return false;
                train.SetSpeed(speed);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TrainActions.SetTargetSpeed", e);
                return false;
            }
        }

        public static bool Brake(string modId, string trainId, bool on)
        {
            try
            {
                TrackTrain train = Target(modId, trainId);
                if (train == null) return false;
                train.ApplyBrakes(on);
                return train.braking == on;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TrainActions.Brake", e);
                return false;
            }
        }

        public static bool Couple(string modId, string trainId, TrainEnd end, string otherTrainId, TrainEnd otherEnd)
        {
            try
            {
                if (!ValidMod(modId)) return false;
                TrackTrain train = Target(modId, trainId);
                TrackTrain other = Target(modId, otherTrainId);
                if (train == null || other == null || !CanCouple(train, end, other, otherEnd)) return false;
                TrackTrain.TrainCoupler coupler = CouplerOf(train, end);
                coupler.Couple(CouplerOf(other, otherEnd));
                return coupler.coupledTo != null;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TrainActions.Couple", e);
                return false;
            }
        }

        public static bool Release(string modId, string trainId, TrainEnd end)
        {
            try
            {
                if (!ValidMod(modId)) return false;
                TrackTrain train = Target(modId, trainId);
                TrackTrain.TrainCoupler coupler = train == null ? null : CouplerOf(train, end);
                if (coupler == null || coupler.coupledTo == null) return false;
                coupler.Release();
                return coupler.coupledTo == null;
            }
            catch (Exception e)
            {
                ApiLog.Once(modId ?? Owner, "TrainActions.Release", e);
                return false;
            }
        }

        private static bool ValidMod(string modId)
        {
            if (ModIds.IsValid(modId)) return true;
            ApiLog.Once(Owner, "modId", "modId must be 1 to 64 characters of letters, digits, '.', '_' or '-'");
            return false;
        }

        private static bool CanCouple(TrackTrain train, TrainEnd end, TrackTrain other, TrainEnd otherEnd)
        {
            TrackTrain.TrainCoupler coupler = CouplerOf(train, end);
            TrackTrain.TrainCoupler otherCoupler = CouplerOf(other, otherEnd);
            if (train == other || train.train == other.train || coupler == null || otherCoupler == null) return false;
            if (coupler.parent != train || otherCoupler.parent != other || coupler.coupledTo != null || otherCoupler.coupledTo != null) return false;
            if (SharesWagon(train.train, other.train)) return false;
            return Ready(coupler) && Ready(otherCoupler) && Touching(coupler, otherCoupler) && Slow(train, coupler) && Slow(other, otherCoupler);
        }

        private static bool Ready(TrackTrain.TrainCoupler coupler)
        {
            TrackTrainCouplerTrigger trigger = coupler.couplingTrigger;
            return trigger != null && trigger.active && trigger.cooldown == null;
        }

        private static bool Touching(TrackTrain.TrainCoupler coupler, TrackTrain.TrainCoupler otherCoupler)
        {
            Collider collider = coupler.couplingTrigger.GetComponent<Collider>();
            Collider otherCollider = otherCoupler.couplingTrigger.GetComponent<Collider>();
            return collider != null && otherCollider != null && collider.bounds.Intersects(otherCollider.bounds);
        }

        private static bool Slow(TrackTrain train, TrackTrain.TrainCoupler coupler)
        {
            float max = coupler.couplingTrigger.maxSpeed;
            return max <= 0f || Mathf.Abs(train.currentSpeed) <= max;
        }

        private static bool SharesWagon(TrackTrain.TrainData a, TrackTrain.TrainData b)
        {
            foreach (TrackTrain.TrainData.WagonData wagon in a.wagons)
            {
                if (b.wagons.Contains(wagon)) return true;
            }
            return false;
        }

        private static TrackTrain.TrainCoupler CouplerOf(TrackTrain train, TrainEnd end)
        {
            if (end == TrainEnd.Front) return train.frontCoupler;
            return end == TrainEnd.Back ? train.backCoupler : null;
        }

        private static TrackTrain Target(string modId, string trainId)
        {
            if (string.IsNullOrEmpty(modId) || SaveGate.Busy || string.IsNullOrEmpty(trainId)) return null;
            TrainController controller = TrainController.instance;
            if (controller == null || controller.TrackTrains == null) return null;
            TrackTrain train;
            if (!controller.TrackTrains.TryGetValue(trainId, out train) || train == null) return null;
            return train;
        }

        private static Minitrain MinitrainOf(TrackTrain train)
        {
            foreach (Minitrain minitrain in UnityEngine.Object.FindObjectsByType<Minitrain>(FindObjectsSortMode.None))
            {
                if (minitrain != null && minitrain.GetTrain() == train) return minitrain;
            }
            return null;
        }
    }
}
