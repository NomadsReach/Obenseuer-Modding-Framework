using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public static class Subscriptions
    {
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
