using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public enum LobbyStatus
    {
        Offline,
        Pending,
        Hosting,
        Joined
    }

    public static class Lobby
    {
        public const int MaxMembers = 32;
        private const float RequestTimeout = 20f;
        private const float RefreshInterval = 1f;
        private const float VerifyWait = 3f;
        private static readonly List<ulong> members = new List<ulong>();
        private static readonly List<ulong> peers = new List<ulong>();
        private static readonly List<ulong> waiting = new List<ulong>();
        private static Action<ulong> joinRequested;
        private static ulong lobbyId;
        private static ulong verifying;
        private static float verifyUntil;
        private static bool hosting;
        private static bool pending;
        private static float pendingSince;
        private static float nextRefresh;
        private static int generation;
        private static int requestGeneration;
        private static int refusedCount;
        private static readonly List<ulong> refusedMembers = new List<ulong>();
        private static ulong lastOwner;
        private static string sessionTag = "";
        private static string sessionVersion = "";
        private static int hostLimit;
        private static string error = "";

        public static event Action<ulong> MemberJoined;
        public static event Action<ulong> MemberLeft;
        public static event Action<ulong, ulong> OwnerChanged;
        public static event Action<ulong> JoinRequested
        {
            add
            {
                joinRequested += value;
                NetPump.Activate();
            }
            remove { joinRequested -= value; }
        }

        public static LobbyStatus Status
        {
            get
            {
                if (pending) return LobbyStatus.Pending;
                if (lobbyId == 0) return LobbyStatus.Offline;
                return hosting ? LobbyStatus.Hosting : LobbyStatus.Joined;
            }
        }

        public static string Error
        {
            get { return error; }
        }

        public static List<ulong> Members
        {
            get { return new List<ulong>(members); }
        }

        public static ulong Owner
        {
            get
            {
                if (lobbyId == 0 || !Steam.IsReady) return 0UL;
                return SteamBridge.LobbyOwner(lobbyId);
            }
        }

        public static bool IsHost
        {
            get
            {
                ulong owner = Owner;
                return owner != 0 && owner == Steam.LocalId;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return Steam.IsReady;
            }
        }

        public static bool Host(string gameTag, string version, int maxMembers, bool openInvite)
        {
            NetChannels.CheckIdentifier(gameTag, "gameTag");
            NetChannels.CheckIdentifier(version, "version");
            if (maxMembers < 1 || maxMembers > MaxMembers) throw new ArgumentException("1 to " + MaxMembers, "maxMembers");
            NetPump.Activate();
            try
            {
                if (!Steam.IsReady) return Refuse("Steam is not ready.");
                if (lobbyId != 0)
                {
                    if (!hosting) return Refuse("Leave the lobby first.");
                    if (gameTag != sessionTag || version != sessionVersion || maxMembers != hostLimit)
                    {
                        Warn("Lobby.Host for game tag " + gameTag + " refused: already hosting lobby " + lobbyId + " for game tag " + sessionTag);
                        return Refuse("The hosted lobby has other settings. Leave the lobby first.");
                    }
                    if (openInvite) Invite();
                    return true;
                }
                if (pending) return Refuse("A lobby request is already running.");
                Begin(gameTag, version);
                hostLimit = maxMembers;
                int gen = generation;
                SteamBridge.CreateLobby(maxMembers, (ok, id) => OnCreated(gen, ok, id, openInvite));
                return true;
            }
            catch (Exception e)
            {
                return Fail("Lobby.Host", e);
            }
        }

        public static bool Join(ulong lobby, string gameTag, string version)
        {
            NetChannels.CheckIdentifier(gameTag, "gameTag");
            NetChannels.CheckIdentifier(version, "version");
            NetPump.Activate();
            try
            {
                if (!Steam.IsReady) return Refuse("Steam is not ready.");
                if (lobby == 0 || pending) return false;
                if (lobbyId == lobby) return true;
                if (lobbyId != 0)
                {
                    Warn("Lobby.Join for game tag " + gameTag + " leaves lobby " + lobbyId + " of game tag " + sessionTag);
                    Leave();
                }
                Begin(gameTag, version);
                int gen = generation;
                SteamBridge.JoinLobby(lobby, ok => OnJoined(gen, ok, lobby));
                return true;
            }
            catch (Exception e)
            {
                return Fail("Lobby.Join", e);
            }
        }

        public static bool Leave(string gameTag)
        {
            NetChannels.CheckIdentifier(gameTag, "gameTag");
            if (gameTag != sessionTag && (lobbyId != 0 || pending)) return false;
            return Leave();
        }

        public static bool Leave()
        {
            try
            {
                generation++;
                if (verifying != 0)
                {
                    SteamBridge.LeaveLobby(verifying);
                    verifying = 0;
                    pending = false;
                }
                if (lobbyId != 0)
                {
                    SteamBridge.LeaveLobby(lobbyId);
                    NetLog.Info("left lobby " + lobbyId);
                    Clear();
                }
                return true;
            }
            catch (Exception e)
            {
                return Fail("Lobby.Leave", e);
            }
        }

        public static bool Invite()
        {
            if (Status != LobbyStatus.Hosting) return Refuse("Host a lobby first.");
            try
            {
                SteamBridge.OpenInvite(lobbyId);
                return true;
            }
            catch (Exception e)
            {
                return Fail("Lobby.Invite", e);
            }
        }

        public static string MemberName(ulong member)
        {
            if (lobbyId == 0 || member == 0) return "";
            try
            {
                string name = Net.Plain(SteamBridge.MemberTag(lobbyId, member, "name"), Net.MaxTextLength);
                if (name.Length > 0) return name;
                return Steam.Name(member);
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal static void Tick()
        {
            try
            {
                if (verifying != 0) Verify();
                if (pending && Time.unscaledTime - pendingSince > RequestTimeout)
                {
                    generation++;
                    pending = false;
                    error = "The lobby request timed out.";
                    NetLog.Info(error);
                }
                if (lobbyId != 0 && Time.unscaledTime >= nextRefresh)
                {
                    nextRefresh = Time.unscaledTime + RefreshInterval;
                    Refresh();
                }
            }
            catch (Exception e)
            {
                Fail("Lobby.Tick", e);
            }
        }

        internal static bool InLobby
        {
            get { return lobbyId != 0; }
        }

        internal static bool IsPeer(ulong id)
        {
            return peers.Contains(id);
        }

        internal static bool DeferSession(ulong id)
        {
            if (lobbyId == 0 || !members.Contains(id)) return false;
            if (!waiting.Contains(id)) waiting.Add(id);
            return true;
        }

        internal static List<ulong> PeerIds()
        {
            return new List<ulong>(peers);
        }

        public static bool IsRefused(ulong member)
        {
            return refusedMembers.Contains(member);
        }

        internal static void OnMemberChanged(ulong id)
        {
            try
            {
                if (lobbyId != 0 && id == lobbyId) Refresh();
            }
            catch (Exception e)
            {
                Fail("Lobby.OnMemberChanged", e);
            }
        }

        internal static void RaiseJoinRequested(ulong id)
        {
            Events.Raise(joinRequested, "Lobby.JoinRequested", h => h(id));
        }

        private static void OnCreated(int gen, bool ok, ulong id, bool openInvite)
        {
            try
            {
                if (gen == requestGeneration) pending = false;
                if (gen != generation)
                {
                    if (ok) SteamBridge.LeaveLobby(id);
                    return;
                }
                if (!ok)
                {
                    error = "Could not create the lobby.";
                    return;
                }
                lobbyId = id;
                hosting = true;
                SteamBridge.SetLobbyTag(id, "game", sessionTag);
                SteamBridge.SetLobbyTag(id, "v", sessionVersion);
                SteamBridge.SetLobbyTag(id, "core", Net.CoreVersion);
                SteamBridge.SetLobbyTag(id, "gv", Application.version);
                SteamBridge.SetLobbyTag(id, "gb", Core.SteamBuildId ?? "");
                AnnounceMember();
                SteamBridge.SetConnect(id);
                Refresh();
                NetLog.Info("hosting lobby " + id + " with " + members.Count + " member(s)");
                if (openInvite) Invite();
            }
            catch (Exception e)
            {
                Fail("Lobby.OnCreated", e);
            }
        }

        private static void OnJoined(int gen, bool ok, ulong id)
        {
            try
            {
                if (gen == requestGeneration) pending = false;
                if (gen != generation)
                {
                    if (ok) SteamBridge.LeaveLobby(id);
                    return;
                }
                if (!ok)
                {
                    error = "Could not join the lobby.";
                    return;
                }
                verifying = id;
                verifyUntil = Time.unscaledTime + VerifyWait;
                pending = true;
                pendingSince = Time.unscaledTime;
                Verify();
            }
            catch (Exception e)
            {
                Fail("Lobby.OnJoined", e);
            }
        }

        private static void Verify()
        {
            ulong id = verifying;
            string tag = SteamBridge.LobbyTag(id, "game");
            string hostVersion = SteamBridge.LobbyTag(id, "v");
            string hostCore = SteamBridge.LobbyTag(id, "core");
            string hostGame = SteamBridge.LobbyTag(id, "gv");
            string hostBuild = SteamBridge.LobbyTag(id, "gb");
            bool foreign = tag.Length > 0 && tag != sessionTag;
            bool incomplete = tag.Length == 0 || hostVersion.Length == 0 || hostCore.Length == 0 || hostGame.Length == 0;
            if (incomplete && !foreign && Time.unscaledTime < verifyUntil) return;
            verifying = 0;
            pending = false;
            if (tag != sessionTag)
            {
                SteamBridge.LeaveLobby(id);
                error = "Not a " + sessionTag + " lobby.";
                return;
            }
            if (hostVersion != sessionVersion || hostCore != Net.CoreVersion)
            {
                SteamBridge.LeaveLobby(id);
                error = "Version mismatch: the lobby runs " + Shown(hostVersion) + " (framework " + Shown(hostCore) + "), you run " + sessionVersion + " (framework " + Net.CoreVersion + ").";
                return;
            }
            if (hostGame != Application.version)
            {
                SteamBridge.LeaveLobby(id);
                error = "The host runs game version " + Shown(hostGame) + ", you run " + Application.version + ".";
                return;
            }
            if (!BuildMatches(hostBuild))
            {
                SteamBridge.LeaveLobby(id);
                error = "The host runs game build " + Shown(hostBuild) + ", you run " + Core.SteamBuildId + ".";
                return;
            }
            lobbyId = id;
            hosting = false;
            AnnounceMember();
            Refresh();
            NetLog.Info("joined lobby " + id + " with " + members.Count + " member(s)");
        }

        private static string Shown(string text)
        {
            return Net.Plain(text, Net.MaxIdentifierLength);
        }

        private static bool BuildMatches(string build)
        {
            string local = Core.SteamBuildId ?? "";
            return build.Length == 0 || local.Length == 0 || build == local;
        }

        private static void AnnounceMember()
        {
            SteamBridge.SetMemberTag(lobbyId, "v", sessionVersion);
            SteamBridge.SetMemberTag(lobbyId, "core", Net.CoreVersion);
            SteamBridge.SetMemberTag(lobbyId, "gv", Application.version);
            SteamBridge.SetMemberTag(lobbyId, "gb", Core.SteamBuildId ?? "");
            SteamBridge.SetMemberTag(lobbyId, "name", Net.Plain(SteamBridge.LocalName(), Net.MaxTextLength));
        }

        private static void Refresh()
        {
            members.Clear();
            members.AddRange(SteamBridge.Members(lobbyId));
            if (members.Count > 0 && !members.Contains(Steam.LocalId))
            {
                error = "You are no longer in the lobby.";
                NetLog.Info("no longer a member of lobby " + lobbyId);
                Clear();
                return;
            }
            List<ulong> fresh = new List<ulong>();
            refusedMembers.Clear();
            foreach (ulong id in members)
            {
                if (id == Steam.LocalId) continue;
                if (SteamBridge.MemberTag(lobbyId, id, "v") == sessionVersion && SteamBridge.MemberTag(lobbyId, id, "core") == Net.CoreVersion && SteamBridge.MemberTag(lobbyId, id, "gv") == Application.version && BuildMatches(SteamBridge.MemberTag(lobbyId, id, "gb"))) fresh.Add(id);
                else refusedMembers.Add(id);
            }
            if (refusedMembers.Count != refusedCount)
            {
                refusedCount = refusedMembers.Count;
                if (refusedCount > 0) NetLog.Info(refusedCount + " member(s) refused: mod version is not " + sessionVersion + ", Core is not " + Net.CoreVersion + ", game version is not " + Application.version + " or game build differs");
            }
            List<ulong> old = new List<ulong>(peers);
            peers.Clear();
            peers.AddRange(fresh);
            waiting.RemoveAll(w => !members.Contains(w));
            foreach (ulong id in fresh)
            {
                if (old.Contains(id)) continue;
                if (waiting.Remove(id)) SteamBridge.AcceptSession(id);
                Events.Raise(MemberJoined, "Lobby.MemberJoined", h => h(id));
            }
            foreach (ulong id in old)
            {
                if (fresh.Contains(id)) continue;
                SteamBridge.CloseSession(id);
                Events.Raise(MemberLeft, "Lobby.MemberLeft", h => h(id));
            }
            ulong owner = Owner;
            if (owner != lastOwner)
            {
                ulong previous = lastOwner;
                lastOwner = owner;
                if (owner != 0 && owner == Steam.LocalId && !hosting)
                {
                    hosting = true;
                    hostLimit = SteamBridge.MemberLimit(lobbyId);
                    SteamBridge.SetConnect(lobbyId);
                }
                Events.Raise(OwnerChanged, "Lobby.OwnerChanged", h => h(previous, owner));
            }
        }

        private static void Clear()
        {
            ulong[] gone = peers.ToArray();
            lobbyId = 0;
            hosting = false;
            members.Clear();
            peers.Clear();
            waiting.Clear();
            refusedMembers.Clear();
            refusedCount = 0;
            lastOwner = 0;
            SteamBridge.ClearConnect();
            foreach (ulong id in gone)
            {
                SteamBridge.CloseSession(id);
                Events.Raise(MemberLeft, "Lobby.MemberLeft", h => h(id));
            }
        }

        private static void Begin(string tag, string version)
        {
            pending = true;
            pendingSince = Time.unscaledTime;
            requestGeneration = generation;
            sessionTag = tag;
            sessionVersion = version;
            error = "";
        }

        private static void Warn(string text)
        {
            CoreLog.For("obenseuer.moddingframework.net").Warn(text);
        }

        private static bool Refuse(string text)
        {
            error = text;
            return false;
        }

        private static bool Fail(string site, Exception e)
        {
            pending = false;
            verifying = 0;
            error = "Lobby error, see obenseuer.moddingframework.net.log.";
            NetLog.Once(site, e);
            return false;
        }
    }
}
