using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Steam
    {
        private const float ProbeInterval = 1f;
        private static bool ready;
        private static float nextProbe;

        public static bool IsReady
        {
            get
            {
                if (ready) return true;
                float now = Time.unscaledTime;
                if (now < nextProbe) return false;
                nextProbe = now + ProbeInterval;
                ready = SteamBridge.GameSteamInitialized();
                return ready;
            }
        }

        public static ulong LocalId
        {
            get { return IsReady ? SteamUser.GetSteamID().m_SteamID : 0UL; }
        }

        public static string Name(ulong steamId)
        {
            if (steamId == 0 || !IsReady) return "";
            return SteamFriends.GetFriendPersonaName(new CSteamID(steamId)) ?? "";
        }

        public static List<ulong> FriendsPlaying(uint appId)
        {
            List<ulong> result = new List<ulong>();
            if (!IsReady) return result;
            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < count; i++)
            {
                CSteamID friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                FriendGameInfo_t info;
                if (!SteamFriends.GetFriendGamePlayed(friend, out info)) continue;
                if (info.m_gameID.AppID().m_AppId == appId) result.Add(friend.m_SteamID);
            }
            return result;
        }
    }
}
