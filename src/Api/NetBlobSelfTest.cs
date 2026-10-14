using System;
using System.Collections.Generic;
using System.Text;

namespace ObenseuerCore
{
    internal static class NetBlobSelfTest
    {
        internal static string Run()
        {
            try
            {
                if (NetBlob.Crc32(Encoding.ASCII.GetBytes("123456789")) != 0xCBF43926u) return "crc32 check value";
                byte[] data = new byte[200000];
                new Random(7).NextBytes(data);
                if (!Same(NetBlob.Gunzip(NetBlob.Gzip(data)), data)) return "gzip round trip";
                uint crc = NetBlob.Crc32(data);
                string[] chunks = NetBlob.Cut(Convert.ToBase64String(NetBlob.Gzip(data)));
                if (chunks.Length < 2) return "the blob did not split into chunks";
                if (!Same(NetBlob.Assemble(chunks, crc), data)) return "reassembled blob differs";
                if (Encoding.UTF8.GetByteCount("blob|" + NetBlob.Packet(int.MaxValue, 9999, 9999, uint.MaxValue, chunks[0])) > Net.MaxPacketBytes) return "packet over the limit";
                if (NetBlob.Packet(7, 3, chunks.Length, crc, chunks[3]).Split(new[] { '|' })[4] != chunks[3]) return "packet fields";
                if (!Rejects(chunks, crc ^ 1u)) return "CRC mismatch accepted";
                string[] damaged = (string[])chunks.Clone();
                char[] changed = damaged[5].ToCharArray();
                changed[100] = changed[100] == 'A' ? 'B' : 'A';
                damaged[5] = new string(changed);
                if (!Rejects(damaged, crc)) return "damaged chunk accepted";
                string limits = Limits();
                if (limits != null) return limits;
                string queued = QueuedBehindLarge();
                if (queued != null) return queued;
                string silent = SilentPeerStalls();
                if (silent != null) return silent;
                string silentQueue = SilentPeerQueue();
                if (silentQueue != null) return silentQueue;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "netblob self-check ok";
        }

        private static string Limits()
        {
            const ulong peer = 9001;
            const ulong oversize = 9002;
            const ulong capped = 9003;
            const ulong slow = 9004;
            const ulong stalled = 9005;
            string chunk = new string('A', 960);
            int delivered = 0;
            Action<ulong, string, string, byte[]> counter = (sender, modId, name, data) =>
            {
                if (sender == peer) delivered++;
            };
            NetBlob.Received += counter;
            try
            {
                for (int i = 0; i < 17; i++) Deliver(peer, 100 + i, Encoding.ASCII.GetBytes("blob " + i), 0f);
                if (delivered != 17) return "17 sequential blobs: " + delivered + " delivered";
                NetBlob.OnPacket(oversize, "O|1|obenseuer.selftest|blob|2|1", 0f);
                NetBlob.OnPacket(oversize, NetBlob.Packet(1, 0, 2, 1u, chunk), 0f);
                if (NetBlob.BufferedFor(oversize) != chunk.Length * sizeof(char)) return "a 960-character chunk was not buffered";
                NetBlob.OnPacket(oversize, NetBlob.Packet(1, 1, 2, 1u, chunk + "A"), 0f);
                if (NetBlob.BufferedFor(oversize) != 0) return "a chunk over 960 characters was not rejected and freed";
                NetBlob.OnPacket(capped, "O|1|obenseuer.selftest|blob|8192|1", 0f);
                for (int i = 0; i < 8191; i++) NetBlob.OnPacket(capped, NetBlob.Packet(1, i, 8192, 1u, chunk), 0f);
                int filled = NetBlob.BufferedFor(capped);
                NetBlob.OnPacket(capped, "O|2|obenseuer.selftest|blob|1000|1", 0f);
                for (int i = 0; i < 1000; i++)
                {
                    NetBlob.OnPacket(capped, NetBlob.Packet(2, i, 1000, 1u, chunk), 0f);
                    if (NetBlob.BufferedFor(capped) > 16 * 1024 * 1024) return "a peer buffered more than 16 MB";
                }
                if (NetBlob.BufferedFor(capped) != filled) return "a transfer past the byte cap was not rejected and freed";
                NetBlob.OnPacket(slow, "O|1|obenseuer.selftest|blob|21|1", 0f);
                for (int i = 0; i < 20; i++)
                {
                    float now = 50f * (i + 1);
                    NetBlob.OnPacket(slow, NetBlob.Packet(1, i, 21, 1u, chunk), now);
                    NetBlob.ExpireIncoming(now);
                    if (NetBlob.BufferedFor(slow) == 0) return "a slow transfer that kept progressing timed out";
                }
                NetBlob.OnPacket(stalled, "O|1|obenseuer.selftest|blob|2|1", 0f);
                NetBlob.OnPacket(stalled, NetBlob.Packet(1, 0, 2, 1u, chunk), 0f);
                NetBlob.OnPacket(stalled, NetBlob.Packet(1, 0, 2, 1u, chunk), 50f);
                NetBlob.ExpireIncoming(61f);
                if (NetBlob.BufferedFor(stalled) != 0) return "a transfer with no new chunk for 60 s was kept";
                float last = 0f;
                for (float t = 50f; t <= 1000f; t += 50f)
                {
                    if (NetBlob.Stalled(last, t)) return "a sender with progress every 50 s timed out";
                    last = t;
                }
                if (!NetBlob.Stalled(0f, 61f)) return "a sender with no progress for 61 s did not time out";
                return null;
            }
            finally
            {
                NetBlob.Received -= counter;
                foreach (ulong used in new[] { peer, oversize, capped, slow, stalled }) NetBlob.OnMemberLeft(used);
            }
        }

        private static string QueuedBehindLarge()
        {
            const ulong peer = 9201;
            const string mod = "obenseuer.selftest";
            Func<ulong, bool> isPeer = NetBlob.PeerCheck;
            Func<ulong, string, bool> transmit = NetBlob.Transmit;
            Dictionary<int, int> count = new Dictionary<int, int>();
            Dictionary<int, int> highest = new Dictionary<int, int>();
            Dictionary<int, bool> outcome = new Dictionary<int, bool>();
            Action<int, bool> finished = (id, ok) => outcome[id] = ok;
            byte[] large = new byte[1500000];
            new Random(11).NextBytes(large);
            byte[] small = new byte[3000];
            new Random(12).NextBytes(small);
            NetBlob.PeerCheck = sender => sender == peer;
            NetBlob.Transmit = (target, payload) =>
            {
                string[] parts = payload.Split(new[] { '|' });
                if (parts[0] == "O") return true;
                int id = int.Parse(parts[0]);
                highest[id] = int.Parse(parts[1]);
                count[id] = int.Parse(parts[2]);
                return true;
            };
            NetBlob.Finished += finished;
            try
            {
                int first = NetBlob.Send(peer, mod, "large", large, 0f);
                int second = NetBlob.Send(peer, mod, "small", small, 0f);
                if (first < 0 || second < 0) return "a blob was refused";
                for (int tick = 1; tick <= 200 * 60 && !outcome.ContainsKey(second); tick++)
                {
                    float now = tick / 60f;
                    NetBlob.Tick(now);
                    if (tick % 60 != 0) continue;
                    foreach (int id in new[] { first, second })
                    {
                        if (!highest.ContainsKey(id)) continue;
                        int prefix = highest[id] + 1 == count[id] ? count[id] : highest[id] + 1;
                        NetBlob.OnPacket(peer, "A|" + id + "|" + prefix, now);
                    }
                }
                if (!outcome.ContainsKey(second)) return "a blob queued behind a large one never finished";
                return outcome[second] ? null : "a blob queued behind a large one failed";
            }
            finally
            {
                NetBlob.Finished -= finished;
                NetBlob.PeerCheck = isPeer;
                NetBlob.Transmit = transmit;
                NetBlob.OnMemberLeft(peer);
            }
        }

        private static string SilentPeerStalls()
        {
            const ulong peer = 9202;
            const string mod = "obenseuer.selftest";
            Func<ulong, bool> isPeer = NetBlob.PeerCheck;
            Func<ulong, string, bool> transmit = NetBlob.Transmit;
            Dictionary<int, bool> outcome = new Dictionary<int, bool>();
            Action<int, bool> finished = (id, ok) => outcome[id] = ok;
            byte[] data = new byte[3000];
            new Random(13).NextBytes(data);
            NetBlob.PeerCheck = sender => sender == peer;
            NetBlob.Transmit = (target, payload) => true;
            NetBlob.Finished += finished;
            try
            {
                int id = NetBlob.Send(peer, mod, "silent", data, 0f);
                if (id < 0) return "a blob was refused";
                for (int tick = 1; tick <= 200 * 60 && !outcome.ContainsKey(id); tick++) NetBlob.Tick(tick / 60f);
                if (!outcome.ContainsKey(id)) return "a transfer to a silent peer never stalled";
                return outcome[id] ? "a transfer to a silent peer finished" : null;
            }
            finally
            {
                NetBlob.Finished -= finished;
                NetBlob.PeerCheck = isPeer;
                NetBlob.Transmit = transmit;
                NetBlob.OnMemberLeft(peer);
            }
        }

        private static string SilentPeerQueue()
        {
            const ulong peer = 9203;
            const string mod = "obenseuer.selftest";
            Func<ulong, bool> isPeer = NetBlob.PeerCheck;
            Func<ulong, string, bool> transmit = NetBlob.Transmit;
            Dictionary<int, bool> outcome = new Dictionary<int, bool>();
            Action<int, bool> finished = (id, ok) => outcome[id] = ok;
            byte[] large = new byte[60000];
            new Random(14).NextBytes(large);
            byte[] small = new byte[3000];
            new Random(15).NextBytes(small);
            NetBlob.PeerCheck = sender => sender == peer;
            NetBlob.Transmit = (target, payload) => true;
            NetBlob.Finished += finished;
            try
            {
                int first = NetBlob.Send(peer, mod, "large", large, 0f);
                int second = NetBlob.Send(peer, mod, "small", small, 0f);
                int third = NetBlob.Send(peer, mod, "small", small, 0f);
                if (first < 0 || second < 0 || third < 0) return "a blob was refused";
                for (int tick = 1; tick <= 200 * 60 && !outcome.ContainsKey(first); tick++) NetBlob.Tick(tick / 60f);
                if (!outcome.ContainsKey(first)) return "a transfer to a silent peer never stalled";
                if (!outcome.ContainsKey(second) || !outcome.ContainsKey(third)) return "transfers queued behind a silent peer's stalled one waited for it to time out";
                return outcome[first] || outcome[second] || outcome[third] ? "a transfer to a silent peer finished" : null;
            }
            finally
            {
                NetBlob.Finished -= finished;
                NetBlob.PeerCheck = isPeer;
                NetBlob.Transmit = transmit;
                NetBlob.OnMemberLeft(peer);
            }
        }

        private static void Deliver(ulong peer, int id, byte[] data, float now)
        {
            string[] chunks = NetBlob.Cut(Convert.ToBase64String(NetBlob.Gzip(data)));
            uint crc = NetBlob.Crc32(data);
            NetBlob.OnPacket(peer, "O|" + id + "|obenseuer.selftest|blob|" + chunks.Length + "|" + crc, now);
            for (int i = 0; i < chunks.Length; i++) NetBlob.OnPacket(peer, NetBlob.Packet(id, i, chunks.Length, crc, chunks[i]), now);
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static bool Rejects(string[] chunks, uint crc)
        {
            try
            {
                NetBlob.Assemble(chunks, crc);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
