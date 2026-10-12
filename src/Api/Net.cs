using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using Steamworks;

namespace ObenseuerCore
{
    public static class Net
    {
        public const int MaxPacketBytes = 1024;
        public const int MaxIdentifierLength = 32;
        private const int Transport = 7;
        private const int MaxPerFrame = 16;
        private const int RecvBytes = 4096;
        private static readonly byte[] recvBuffer = new byte[RecvBytes];
        internal static readonly string CoreVersion = ((BepInPlugin)Attribute.GetCustomAttribute(typeof(Core), typeof(BepInPlugin))).Version.ToString();

        public static ulong Me
        {
            get { return Steam.LocalId; }
        }

        public static List<ulong> Peers
        {
            get { return Lobby.PeerIds(); }
        }

        public static bool Send(ulong peer, string channel, string payload, bool reliable)
        {
            NetChannels.CheckIdentifier(channel, "channel");
            if (payload == null) throw new ArgumentNullException("payload");
            if (!Lobby.IsPeer(peer)) return false;
            byte[] bytes = Encoding.UTF8.GetBytes(channel + "|" + payload);
            if (bytes.Length > MaxPacketBytes) return false;
            EP2PSend type = reliable ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable;
            return SteamNetworking.SendP2PPacket(new CSteamID(peer), bytes, (uint)bytes.Length, type, Transport);
        }

        public static bool Register(string modId, string channel, Action<ulong, string> handler)
        {
            bool added = NetChannels.Add(modId, channel, handler);
            if (added) NetPump.Activate();
            return added;
        }

        internal static void Init()
        {
            NetChannels.Add("obenseuer.moddingframework", "presence", Presence.OnPacket);
            NetPump.Ensure();
        }

        internal static void Drain()
        {
            if (!Lobby.InLobby) return;
            for (int i = 0; i < MaxPerFrame; i++)
            {
                uint size;
                if (!SteamNetworking.IsP2PPacketAvailable(out size, Transport)) return;
                uint read;
                CSteamID sender;
                if (!SteamNetworking.ReadP2PPacket(recvBuffer, RecvBytes, out read, out sender, Transport)) return;
                ulong peer = sender.m_SteamID;
                if (size > MaxPacketBytes || read > MaxPacketBytes)
                {
                    NetLog.Debug("Net.oversized", "dropped " + size + " bytes from " + peer);
                    continue;
                }
                if (!Lobby.IsPeer(peer))
                {
                    NetLog.Debug("Net.sender", "dropped packet from non-peer " + peer);
                    continue;
                }
                NetChannels.Dispatch(peer, Encoding.UTF8.GetString(recvBuffer, 0, (int)read));
            }
        }

        internal static void OnSessionRequest(P2PSessionRequest_t request)
        {
            try
            {
                ulong peer = request.m_steamIDRemote.m_SteamID;
                if (Lobby.IsPeer(peer))
                {
                    SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
                    return;
                }
                bool waiting = Lobby.DeferSession(peer);
                NetLog.Debug("Net.session", "P2P session request from " + peer + (waiting ? " deferred until its version is verified" : " refused: not a lobby member"));
            }
            catch (Exception e)
            {
                NetLog.Once("Net.OnSessionRequest", e);
            }
        }
    }
}
