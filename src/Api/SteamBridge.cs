using System;
using System.Reflection;
using Steamworks;
using UnityEngine;

namespace ObenseuerCore
{
    internal static class SteamBridge
    {
        private const string ConnectPrefix = "+connect_lobby ";
        private static readonly FieldInfo InitializedField = typeof(SteamManager).GetField("m_bInitialized", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static Callback<P2PSessionRequest_t> sessionCallback;
        private static Callback<LobbyChatUpdate_t> chatCallback;
        private static Callback<LobbyDataUpdate_t> dataCallback;
        private static Callback<GameLobbyJoinRequested_t> joinCallback;
        private static Callback<GameRichPresenceJoinRequested_t> connectCallback;
        private static CallResult<LobbyCreated_t> createResult;
        private static CallResult<LobbyEnter_t> enterResult;

        internal static bool GameSteamInitialized()
        {
            if (InitializedField == null)
            {
                NetLog.Once("Steam.probe", "SteamManager.m_bInitialized not found, Steam is never reported ready");
                return false;
            }
            foreach (SteamManager manager in UnityEngine.Object.FindObjectsByType<SteamManager>(FindObjectsSortMode.None))
            {
                if ((bool)InitializedField.GetValue(manager)) return true;
            }
            return false;
        }

        internal static void Start()
        {
            sessionCallback = Callback<P2PSessionRequest_t>.Create(Net.OnSessionRequest);
            chatCallback = Callback<LobbyChatUpdate_t>.Create(OnChatUpdate);
            dataCallback = Callback<LobbyDataUpdate_t>.Create(OnDataUpdate);
            joinCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
            connectCallback = Callback<GameRichPresenceJoinRequested_t>.Create(OnConnectRequested);
            createResult = CallResult<LobbyCreated_t>.Create();
            enterResult = CallResult<LobbyEnter_t>.Create();
        }

        internal static void CreateLobby(int maxMembers, Action<bool, ulong> done)
        {
            SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers);
            createResult.Set(call, (created, failed) => done(!failed && created.m_eResult == EResult.k_EResultOK, created.m_ulSteamIDLobby));
        }

        internal static void JoinLobby(ulong lobby, Action<bool> done)
        {
            SteamAPICall_t call = SteamMatchmaking.JoinLobby(new CSteamID(lobby));
            enterResult.Set(call, (entered, failed) => done(!failed && entered.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess));
        }

        internal static void LeaveLobby(ulong lobby)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(lobby));
        }

        internal static ulong[] Members(ulong lobby)
        {
            CSteamID id = new CSteamID(lobby);
            int count = SteamMatchmaking.GetNumLobbyMembers(id);
            ulong[] members = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                members[i] = SteamMatchmaking.GetLobbyMemberByIndex(id, i).m_SteamID;
            }
            return members;
        }

        internal static ulong LobbyOwner(ulong lobby)
        {
            return SteamMatchmaking.GetLobbyOwner(new CSteamID(lobby)).m_SteamID;
        }

        internal static int MemberLimit(ulong lobby)
        {
            return SteamMatchmaking.GetLobbyMemberLimit(new CSteamID(lobby));
        }

        internal static void SetLobbyTag(ulong lobby, string key, string value)
        {
            SteamMatchmaking.SetLobbyData(new CSteamID(lobby), key, value);
        }

        internal static string LobbyTag(ulong lobby, string key)
        {
            return SteamMatchmaking.GetLobbyData(new CSteamID(lobby), key) ?? "";
        }

        internal static void SetMemberTag(ulong lobby, string key, string value)
        {
            SteamMatchmaking.SetLobbyMemberData(new CSteamID(lobby), key, value);
        }

        internal static string MemberTag(ulong lobby, ulong user, string key)
        {
            return SteamMatchmaking.GetLobbyMemberData(new CSteamID(lobby), new CSteamID(user), key) ?? "";
        }

        internal static string LocalName()
        {
            return SteamFriends.GetPersonaName() ?? "";
        }

        internal static void OpenInvite(ulong lobby)
        {
            SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(lobby));
        }

        internal static void SetConnect(ulong lobby)
        {
            SteamFriends.SetRichPresence("connect", ConnectPrefix + lobby);
        }

        internal static void ClearConnect()
        {
            SteamFriends.ClearRichPresence();
        }

        internal static void AcceptSession(ulong user)
        {
            SteamNetworking.AcceptP2PSessionWithUser(new CSteamID(user));
        }

        internal static void CloseSession(ulong user)
        {
            SteamNetworking.CloseP2PSessionWithUser(new CSteamID(user));
        }

        private static void OnChatUpdate(LobbyChatUpdate_t update)
        {
            Lobby.OnMemberChanged(update.m_ulSteamIDLobby);
        }

        private static void OnDataUpdate(LobbyDataUpdate_t update)
        {
            Lobby.OnMemberChanged(update.m_ulSteamIDLobby);
        }

        private static void OnJoinRequested(GameLobbyJoinRequested_t request)
        {
            Lobby.RaiseJoinRequested(request.m_steamIDLobby.m_SteamID);
        }

        private static void OnConnectRequested(GameRichPresenceJoinRequested_t request)
        {
            string text = request.m_rgchConnect;
            ulong lobby;
            if (text != null && text.StartsWith(ConnectPrefix, StringComparison.Ordinal) && ulong.TryParse(text.Substring(ConnectPrefix.Length).Trim(), out lobby))
            {
                Lobby.RaiseJoinRequested(lobby);
            }
        }
    }
}
