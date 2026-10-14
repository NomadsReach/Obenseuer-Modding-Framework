using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Subscriptions
    {
        private static readonly Dictionary<string, PlayerSubscription> loaded = new Dictionary<string, PlayerSubscription>();
        private static readonly Dictionary<string, float> missUntil = new Dictionary<string, float>();
        private const float MissRetrySeconds = 5f;

        internal static void Init()
        {
            Events.Loaded += missUntil.Clear;
        }

        public static bool IsActive(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            try
            {
                PlayerSubscriptionsController subscriptions = PlayerSubscriptionsController.instance;
                return subscriptions != null && subscriptions.SubscriptionIdActive(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Subscriptions.IsActive", e);
                return false;
            }
        }

        public static bool Subscribe(string id)
        {
            if (string.IsNullOrEmpty(id) || SaveGate.Busy || PlayerSubscriptionsController.instance == null || IsActive(id)) return false;
            try
            {
                PlayerSubscription subscription = LoadedById(id);
                if (subscription != null && !(subscription is PlayerPhonePlanSubscription)) subscription.Subscribe();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Subscriptions.Subscribe", e);
            }
            return IsActive(id);
        }

        public static bool Unsubscribe(string id)
        {
            if (string.IsNullOrEmpty(id) || SaveGate.Busy || PlayerSubscriptionsController.instance == null || !IsActive(id)) return false;
            try
            {
                PlayerSubscription subscription = ActiveById(id);
                if (subscription != null && !(subscription is PlayerPhonePlanSubscription)) subscription.Unsubscribe();
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Subscriptions.Unsubscribe", e);
            }
            return !IsActive(id);
        }

        private static PlayerSubscription LoadedById(string id)
        {
            PlayerSubscription cached;
            if (loaded.TryGetValue(id, out cached) && cached != null) return cached;
            float retryAt;
            if (missUntil.TryGetValue(id, out retryAt) && Time.unscaledTime < retryAt) return null;
            foreach (PlayerSubscription subscription in Resources.FindObjectsOfTypeAll<PlayerSubscription>())
            {
                if (subscription == null || subscription.Id != id) continue;
                loaded[id] = subscription;
                return subscription;
            }
            missUntil[id] = Time.unscaledTime + MissRetrySeconds;
            return null;
        }

        private static PlayerSubscription ActiveById(string id)
        {
            foreach (PlayerSubscriptionsController.PlayerSubscriptionInfo info in PlayerSubscriptionsController.instance.activeSubscriptions)
            {
                if (info != null && info.subscription != null && info.subscription.Id == id) return info.subscription;
            }
            return null;
        }

        public static List<string> ActiveIds()
        {
            List<string> ids = new List<string>();
            try
            {
                PlayerSubscriptionsController subscriptions = PlayerSubscriptionsController.instance;
                if (subscriptions == null) return ids;
                foreach (PlayerSubscriptionsController.PlayerSubscriptionInfo info in subscriptions.activeSubscriptions)
                {
                    if (info != null && info.subscription != null) ids.Add(info.subscription.Id);
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(PlayerState.Owner, "Subscriptions.ActiveIds", e);
            }
            return ids;
        }
    }
}
