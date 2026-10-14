using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using Steamworks;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Net
    {
        public const int MaxPacketBytes = 1024;
        public const int MaxIdentifierLength = 32;
        internal const int MaxTextLength = 64;
        private const int Transport = 7;
        private const int MaxPerPeer = 4;
        private const int MaxQueuedPerPeer = 1024;
        private const double DrainSeconds = 0.002;
        private const double DropLogSeconds = 60;
        private const int RecvBytes = 4096;
        private static readonly byte[] recvBuffer = new byte[RecvBytes];
        private static readonly Dictionary<ulong, int> perPeer = new Dictionary<ulong, int>();
        private static readonly Dictionary<ulong, Queue<string>> queued = new Dictionary<ulong, Queue<string>>();
        private static long queueDrops;
        private static readonly Dictionary<ulong, int> droppedPerPeer = new Dictionary<ulong, int>();
        private static readonly Dictionary<ulong, double> dropLogged = new Dictionary<ulong, double>();
        internal static readonly string CoreVersion = ((BepInPlugin)Attribute.GetCustomAttribute(typeof(Core), typeof(BepInPlugin))).Version.ToString();

        public static ulong Me
        {
            get { return Steam.LocalId; }
        }

        public static List<ulong> Peers
        {
            get { return Lobby.PeerIds(); }
        }

        public static long Dropped
        {
            get { return queueDrops; }
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
            if (modId == ModIds.CoreId) return false;
            bool added = NetChannels.Add(modId, channel, handler);
            if (added) NetPump.Activate();
            return added;
        }

        internal static void Init()
        {
            NetChannels.Add("obenseuer.moddingframework", "presence", Presence.OnPacket);
            NetChannels.Add("obenseuer.moddingframework", "blob", NetBlob.OnPacket);
            Lobby.MemberLeft += NetBlob.OnMemberLeft;
            NetPump.Ensure();
        }

        internal static string Plain(string text, int max)
        {
            StringBuilder plain = new StringBuilder();
            foreach (char c in text ?? "")
            {
                if (!Stripped(c)) plain.Append(c);
            }
            if (plain.Length <= max) return plain.ToString();
            int end = char.IsHighSurrogate(plain[max - 1]) ? max - 1 : max;
            return plain.ToString(0, end);
        }

        private static bool Stripped(char c)
        {
            return c <= 0x1F || (c >= 0x7F && c <= 0x9F) || c == '<' || c == '>' || (c >= 0x200B && c <= 0x200F) || (c >= 0x202A && c <= 0x202E) || (c >= 0x2060 && c <= 0x2064) || (c >= 0x2066 && c <= 0x2069) || c == 0xFEFF;
        }

        internal static void Drain()
        {
            if (!Lobby.InLobby)
            {
                queued.Clear();
                return;
            }
            perPeer.Clear();
            DispatchQueued();
            double until = Time.realtimeSinceStartupAsDouble + DrainSeconds;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                uint size;
                if (!SteamNetworking.IsP2PPacketAvailable(out size, Transport)) return;
                uint read;
                CSteamID sender;
                if (!SteamNetworking.ReadP2PPacket(recvBuffer, RecvBytes, out read, out sender, Transport)) return;
                ulong peer = sender.m_SteamID;
                if (size > MaxPacketBytes || read > MaxPacketBytes)
                {
                    if (Core.Verbose) NetLog.Debug("Net.oversized", "dropped " + size + " bytes from " + peer);
                    continue;
                }
                if (!Lobby.IsPeer(peer))
                {
                    if (Core.Verbose) NetLog.Debug("Net.sender", "dropped packet from non-peer " + peer);
                    continue;
                }
                if (Budget(peer)) NetChannels.Dispatch(peer, Encoding.UTF8.GetString(recvBuffer, 0, (int)read));
                else Enqueue(peer, Encoding.UTF8.GetString(recvBuffer, 0, (int)read));
            }
        }

        private static bool Budget(ulong peer)
        {
            int count;
            perPeer.TryGetValue(peer, out count);
            if (count >= MaxPerPeer) return false;
            perPeer[peer] = count + 1;
            return true;
        }

        private static void Enqueue(ulong peer, string text)
        {
            Queue<string> pending;
            if (!queued.TryGetValue(peer, out pending))
            {
                pending = new Queue<string>();
                queued[peer] = pending;
            }
            if (pending.Count < MaxQueuedPerPeer)
            {
                pending.Enqueue(text);
                return;
            }
            queueDrops++;
            CountDrop(peer);
        }

        private static void CountDrop(ulong peer)
        {
            int count;
            droppedPerPeer.TryGetValue(peer, out count);
            count++;
            double now = Time.realtimeSinceStartupAsDouble;
            double last;
            if (dropLogged.TryGetValue(peer, out last) && now - last < DropLogSeconds)
            {
                droppedPerPeer[peer] = count;
                return;
            }
            dropLogged[peer] = now;
            droppedPerPeer[peer] = 0;
            NetLog.Info("net: dropped " + count + " packets from " + peer);
        }

        internal static bool SelfCheck()
        {
            const ulong fake = 1;
            long before = queueDrops;
            try
            {
                for (int i = 0; i < MaxQueuedPerPeer + 5; i++) Enqueue(fake, "x");
                return queued[fake].Count == MaxQueuedPerPeer && queueDrops - before == 5;
            }
            finally
            {
                queued.Remove(fake);
                droppedPerPeer.Remove(fake);
                dropLogged.Remove(fake);
                queueDrops = before;
            }
        }

        private static void DispatchQueued()
        {
            if (queued.Count == 0) return;
            foreach (ulong peer in new List<ulong>(queued.Keys))
            {
                Queue<string> pending = queued[peer];
                while (pending.Count > 0 && Lobby.IsPeer(peer) && Budget(peer))
                {
                    NetChannels.Dispatch(peer, pending.Dequeue());
                }
                if (pending.Count == 0 || !Lobby.IsPeer(peer)) queued.Remove(peer);
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
