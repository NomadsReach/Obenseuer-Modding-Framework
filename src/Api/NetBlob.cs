using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace ObenseuerCore
{
    public static class NetBlob
    {
        private const string Channel = "blob";
        private const int ChunkChars = 960;
        private const int MaxChunks = 8192;
        private const int MaxBytes = 16 * 1024 * 1024;
        private const int MaxTransfers = 16;
        private const int MaxFinished = 64;
        private const int MaxBufferedBytes = 16 * 1024 * 1024;
        private const int Window = 32;
        private const int AckEvery = 8;
        private const float PacketsPerSecond = 120f;
        private const float Burst = 12f;
        private const float ResendSeconds = 2f;
        private const float TotalSeconds = 60f;
        private static readonly uint[] crcTable = CrcTable();
        private static readonly List<Outgoing> outgoing = new List<Outgoing>();
        private static readonly List<Incoming> incoming = new List<Incoming>();
        private static readonly Dictionary<ulong, Budget> budgets = new Dictionary<ulong, Budget>();
        private static int lastId;
        internal static Func<ulong, bool> PeerCheck = Lobby.IsPeer;
        internal static Func<ulong, string, bool> Transmit = (peer, payload) => Net.Send(peer, Channel, payload, true);

        public static event Action<ulong, string, string, byte[]> Received;
        public static event Action<int, bool> Finished;

        public static int Send(ulong peer, string modId, string name, byte[] data)
        {
            return Send(peer, modId, name, data, Time.unscaledTime);
        }

        internal static int Send(ulong peer, string modId, string name, byte[] data, float now)
        {
            ModIds.Require(modId, "modId");
            NetChannels.CheckIdentifier(name, "name");
            if (data == null) throw new ArgumentNullException("data");
            if (!PeerCheck(peer)) return -1;
            if (data.Length > MaxBytes) return Refuse("a blob over " + MaxBytes + " bytes");
            string[] chunks = Cut(Convert.ToBase64String(Gzip(data)));
            if (chunks.Length > MaxChunks) return Refuse("a blob over " + MaxChunks + " chunks");
            if (outgoing.Count >= MaxTransfers) return Refuse("more than " + MaxTransfers + " transfers in progress");
            Outgoing transfer = new Outgoing { Id = ++lastId, Peer = peer, ModId = modId, Name = name, Chunks = chunks, Crc = Crc32(data), LastProgress = now, LastAck = now };
            outgoing.Add(transfer);
            return transfer.Id;
        }

        public static float Progress(int id)
        {
            Outgoing transfer = outgoing.Find(t => t.Id == id);
            return transfer == null ? -1f : transfer.Acked / (float)transfer.Chunks.Length;
        }

        public static float Progress(ulong peer, int id)
        {
            Incoming transfer = FindIncoming(peer, id);
            return transfer == null || transfer.Chunks == null ? -1f : transfer.Prefix / (float)transfer.Count;
        }

        public static bool Cancel(int id)
        {
            Outgoing transfer = outgoing.Find(t => t.Id == id);
            if (transfer == null) return false;
            Net.Send(transfer.Peer, Channel, "C|" + id, true);
            Finish(transfer, false, "cancelled");
            return true;
        }

        internal static void Tick()
        {
            Tick(Time.unscaledTime);
        }

        internal static void Tick(float now)
        {
            if (outgoing.Count > 0) PumpOutgoing(now);
            if (incoming.Count > 0) ExpireIncoming(now);
        }

        internal static void OnPacket(ulong sender, string text)
        {
            OnPacket(sender, text, Time.unscaledTime);
        }

        internal static void OnPacket(ulong sender, string text, float now)
        {
            string[] parts = text.Split(new[] { '|' });
            switch (parts[0])
            {
                case "O":
                    if (parts.Length == 6) OnOpen(sender, parts, now);
                    return;
                case "A":
                    if (parts.Length == 3) OnAck(sender, parts, now);
                    return;
                case "R":
                    if (parts.Length == 2) OnRefused(sender, parts[1]);
                    return;
                case "C":
                    if (parts.Length == 2) OnCancel(sender, parts[1]);
                    return;
            }
            if (parts.Length == 5) OnData(sender, parts, now);
        }

        internal static void OnMemberLeft(ulong peer)
        {
            FinishPeer(peer, "the peer left");
            incoming.RemoveAll(t => t.Peer == peer);
            budgets.Remove(peer);
        }

        internal static string Packet(int id, int index, int count, uint crc, string chunk)
        {
            return id + "|" + index + "|" + count + "|" + crc + "|" + chunk;
        }

        internal static string[] Cut(string encoded)
        {
            string[] chunks = new string[(encoded.Length + ChunkChars - 1) / ChunkChars];
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i] = encoded.Substring(i * ChunkChars, Math.Min(ChunkChars, encoded.Length - i * ChunkChars));
            }
            return chunks;
        }

        internal static byte[] Assemble(string[] chunks, uint crc)
        {
            byte[] data = Gunzip(Convert.FromBase64String(string.Concat(chunks)));
            if (Crc32(data) != crc) throw new InvalidDataException("CRC32 mismatch");
            return data;
        }

        internal static byte[] Gzip(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        internal static byte[] Gunzip(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                int read;
                while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    if (output.Length > MaxBytes) throw new InvalidDataException("larger than " + MaxBytes + " bytes");
                }
                return output.ToArray();
            }
        }

        internal static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFFu;
            foreach (byte b in data)
            {
                crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        internal static bool Stalled(float lastProgress, float now)
        {
            return now - lastProgress > TotalSeconds;
        }

        private static uint[] CrcTable()
        {
            uint[] table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        private static int Refuse(string reason)
        {
            NetLog.Info("blob refused: " + reason);
            return -1;
        }

        private static void Reject(Incoming transfer, string reason)
        {
            incoming.Remove(transfer);
            transfer.Chunks = null;
            Net.Send(transfer.Peer, Channel, "R|" + transfer.Id, true);
            NetLog.Info("blob " + transfer.Id + " " + transfer.ModId + "/" + transfer.Name + " from " + transfer.Peer + " rejected: " + reason);
        }

        private static void PumpOutgoing(float now)
        {
            foreach (Outgoing transfer in outgoing.ToArray())
            {
                if (!outgoing.Contains(transfer)) continue;
                if (!PeerCheck(transfer.Peer)) Finish(transfer, false, "the peer left");
                else if (Stalled(transfer.LastProgress, now))
                {
                    Finish(transfer, false, "no progress for " + TotalSeconds + " s, at chunk " + transfer.Acked + " of " + transfer.Chunks.Length);
                    FinishPeer(transfer.Peer, "the peer went silent");
                }
                else
                {
                    if (transfer.Sent > transfer.Acked && now - transfer.LastAck > ResendSeconds) Rewind(transfer, now);
                    Pump(transfer, now);
                }
            }
        }

        private static void Pump(Outgoing transfer, float now)
        {
            Budget budget = BudgetFor(transfer.Peer, now);
            while (budget.Packets >= 1f && (!transfer.Opened || transfer.Sent < transfer.Chunks.Length) && InFlight(transfer.Peer) < Window)
            {
                if (!SendNext(transfer)) return;
                budget.Packets -= 1f;
            }
            if (!transfer.Resending && transfer.Sent == transfer.Acked && InFlight(transfer.Peer) > 0) transfer.LastProgress = now;
        }

        private static Budget BudgetFor(ulong peer, float now)
        {
            Budget budget;
            if (!budgets.TryGetValue(peer, out budget))
            {
                budget = new Budget { At = now };
                budgets[peer] = budget;
            }
            budget.Packets = Math.Min(budget.Packets + PacketsPerSecond * (now - budget.At), Burst);
            budget.At = now;
            return budget;
        }

        private static int InFlight(ulong peer)
        {
            int count = 0;
            foreach (Outgoing transfer in outgoing)
            {
                if (transfer.Peer == peer) count += transfer.Sent - transfer.Acked;
            }
            return count;
        }

        private static bool SendNext(Outgoing transfer)
        {
            string payload = transfer.Opened
                ? Packet(transfer.Id, transfer.Sent, transfer.Chunks.Length, transfer.Crc, transfer.Chunks[transfer.Sent])
                : "O|" + transfer.Id + "|" + transfer.ModId + "|" + transfer.Name + "|" + transfer.Chunks.Length + "|" + transfer.Crc;
            if (!Transmit(transfer.Peer, payload)) return false;
            if (transfer.Opened) transfer.Sent++;
            else transfer.Opened = true;
            return true;
        }

        private static void Rewind(Outgoing transfer, float now)
        {
            transfer.Sent = transfer.Acked;
            transfer.Opened = transfer.Acked > 0;
            transfer.LastAck = now;
            transfer.Resending = true;
        }

        private static void Finish(Outgoing transfer, bool ok, string reason)
        {
            outgoing.Remove(transfer);
            if (!outgoing.Exists(t => t.Peer == transfer.Peer)) budgets.Remove(transfer.Peer);
            NetLog.Info("blob " + transfer.Id + " " + transfer.ModId + "/" + transfer.Name + " to " + transfer.Peer + (ok ? " delivered" : " failed: " + reason));
            Events.Raise(Finished, "NetBlob.Finished", h => h(transfer.Id, ok));
        }

        private static void FinishPeer(ulong peer, string reason)
        {
            foreach (Outgoing transfer in outgoing.ToArray())
            {
                if (transfer.Peer == peer) Finish(transfer, false, reason);
            }
        }

        private static void OnOpen(ulong sender, string[] parts, float now)
        {
            int id, count;
            uint crc;
            if (!Number(parts[1], out id) || !Number(parts[4], out count) || !UNumber(parts[5], out crc)) return;
            if (count < 1 || count > MaxChunks || !ModIds.IsValid(parts[2]) || !ModIds.IsValid(parts[3])) return;
            Incoming transfer = FindIncoming(sender, id);
            if (transfer != null)
            {
                SendAck(transfer);
                return;
            }
            if (incoming.FindAll(t => t.Peer == sender && t.Chunks != null).Count >= MaxTransfers)
            {
                Net.Send(sender, Channel, "R|" + id, true);
                return;
            }
            incoming.Add(new Incoming { Peer = sender, Id = id, ModId = parts[2], Name = parts[3], Count = count, Crc = crc, Chunks = new string[count], LastSeen = now });
        }

        private static void OnData(ulong sender, string[] parts, float now)
        {
            int id, index, count;
            uint crc;
            if (!Number(parts[0], out id) || !Number(parts[1], out index) || !Number(parts[2], out count) || !UNumber(parts[3], out crc)) return;
            Incoming transfer = FindIncoming(sender, id);
            if (transfer == null || transfer.Count != count || transfer.Crc != crc || index >= count) return;
            if (transfer.Chunks != null && parts[4].Length > ChunkChars)
            {
                Reject(transfer, "a chunk over " + ChunkChars + " characters");
                return;
            }
            if (transfer.Chunks == null || transfer.Chunks[index] != null)
            {
                SendAck(transfer);
                return;
            }
            int bytes = parts[4].Length * sizeof(char);
            if (BufferedFor(sender) + bytes > MaxBufferedBytes)
            {
                Reject(transfer, "more than " + MaxBufferedBytes + " bytes buffered from this peer");
                return;
            }
            transfer.Chunks[index] = parts[4];
            transfer.Bytes += bytes;
            transfer.LastSeen = now;
            while (transfer.Prefix < transfer.Count && transfer.Chunks[transfer.Prefix] != null) transfer.Prefix++;
            if (transfer.Prefix == transfer.Count) Complete(transfer);
            else if (transfer.Prefix - transfer.Acked >= AckEvery) SendAck(transfer);
        }

        private static void Complete(Incoming transfer)
        {
            byte[] data;
            try
            {
                data = Assemble(transfer.Chunks, transfer.Crc);
            }
            catch (Exception e)
            {
                Reject(transfer, e.Message);
                return;
            }
            TrimFinished(transfer.Peer);
            transfer.Chunks = null;
            SendAck(transfer);
            NetLog.Info("blob " + transfer.Id + " " + transfer.ModId + "/" + transfer.Name + " from " + transfer.Peer + " received, " + data.Length + " bytes");
            Events.Raise(Received, "NetBlob.Received", h => h(transfer.Peer, transfer.ModId, transfer.Name, data));
        }

        private static void TrimFinished(ulong peer)
        {
            if (incoming.FindAll(t => t.Peer == peer && t.Chunks == null).Count >= MaxFinished) incoming.Remove(incoming.Find(t => t.Peer == peer && t.Chunks == null));
        }

        private static void SendAck(Incoming transfer)
        {
            transfer.Acked = transfer.Prefix;
            Net.Send(transfer.Peer, Channel, "A|" + transfer.Id + "|" + transfer.Prefix, true);
        }

        private static void OnAck(ulong sender, string[] parts, float now)
        {
            int id, prefix;
            if (!Number(parts[1], out id) || !Number(parts[2], out prefix)) return;
            Outgoing transfer = outgoing.Find(t => t.Id == id);
            if (transfer == null || transfer.Peer != sender || prefix > transfer.Chunks.Length || prefix <= transfer.Acked) return;
            transfer.Acked = prefix;
            transfer.Opened = true;
            if (transfer.Sent < prefix) transfer.Sent = prefix;
            transfer.LastAck = now;
            transfer.LastProgress = now;
            transfer.Resending = false;
            if (transfer.Acked == transfer.Chunks.Length) Finish(transfer, true, "");
        }

        private static void OnRefused(ulong sender, string text)
        {
            int id;
            if (!Number(text, out id)) return;
            Outgoing transfer = outgoing.Find(t => t.Id == id);
            if (transfer != null && transfer.Peer == sender) Finish(transfer, false, "the receiver rejected it");
        }

        private static void OnCancel(ulong sender, string text)
        {
            int id;
            if (!Number(text, out id)) return;
            Incoming transfer = FindIncoming(sender, id);
            if (transfer != null) incoming.Remove(transfer);
        }

        internal static void ExpireIncoming(float now)
        {
            for (int i = incoming.Count - 1; i >= 0; i--)
            {
                if (!Stalled(incoming[i].LastSeen, now)) continue;
                if (incoming[i].Chunks != null) NetLog.Info("blob " + incoming[i].Id + " from " + incoming[i].Peer + " dropped: no data for " + TotalSeconds + " s");
                incoming.RemoveAt(i);
            }
        }

        private static Incoming FindIncoming(ulong peer, int id)
        {
            return incoming.Find(t => t.Peer == peer && t.Id == id);
        }

        internal static int BufferedFor(ulong peer)
        {
            int total = 0;
            foreach (Incoming transfer in incoming)
            {
                if (transfer.Peer == peer && transfer.Chunks != null) total += transfer.Bytes;
            }
            return total;
        }

        private static bool Number(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private static bool UNumber(string text, out uint value)
        {
            return uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private sealed class Outgoing
        {
            public int Id;
            public ulong Peer;
            public string ModId;
            public string Name;
            public string[] Chunks;
            public uint Crc;
            public int Sent;
            public int Acked;
            public bool Opened;
            public bool Resending;
            public float LastProgress;
            public float LastAck;
        }

        private sealed class Incoming
        {
            public ulong Peer;
            public int Id;
            public string ModId;
            public string Name;
            public int Count;
            public uint Crc;
            public string[] Chunks;
            public int Prefix;
            public int Acked;
            public int Bytes;
            public float LastSeen;
        }

        private sealed class Budget
        {
            public float Packets;
            public float At;
        }
    }
}
