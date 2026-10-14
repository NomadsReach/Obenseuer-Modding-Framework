using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ObenseuerCore
{
    public sealed class PresenceRecord
    {
        public ulong Peer;
        public string Name;
        public string Area;
        public Vector3 Position;
        public float Yaw;
        public bool Moving;
        public float LastSeen;
    }

    public static class Presence
    {
        private const string Channel = "presence";
        private const int MaxAreaLength = 64;
        private const float MaxValue = 100000f;
        private const float SendInterval = 0.1f;
        private const float PublishTimeout = 1f;
        private const float StaleAfter = 5f;
        private static readonly Dictionary<ulong, PresenceRecord> records = new Dictionary<ulong, PresenceRecord>();
        private static readonly List<ulong> gone = new List<ulong>();
        private static bool published;
        private static float publishedAt;
        private static float nextSend;
        private static string sampleArea = "";
        private static Vector3 samplePosition;
        private static float sampleYaw;
        private static bool sampleMoving;

        public static List<PresenceRecord> Remotes
        {
            get
            {
                List<PresenceRecord> result = new List<PresenceRecord>(records.Values);
                return result;
            }
        }

        public static bool Publish(string area, Vector3 position, float yaw, bool moving)
        {
            if (area == null || area.Length == 0 || area.Length > MaxAreaLength || area.IndexOf('|') >= 0) throw new ArgumentException("1 to 64 characters, no '|'", "area");
            if (!IsValue(position.x) || !IsValue(position.y) || !IsValue(position.z)) throw new ArgumentException("finite and within 100000", "position");
            if (!IsValue(yaw)) throw new ArgumentException("finite and within 100000", "yaw");
            sampleArea = area;
            samplePosition = position;
            sampleYaw = yaw;
            sampleMoving = moving;
            published = true;
            publishedAt = Time.unscaledTime;
            return Lobby.Status == LobbyStatus.Hosting || Lobby.Status == LobbyStatus.Joined;
        }

        internal static void Tick()
        {
            float now = Time.unscaledTime;
            Prune(now);
            if (!published || now - publishedAt > PublishTimeout || now < nextSend) return;
            nextSend = now + SendInterval;
            string text = Format();
            foreach (ulong peer in Net.Peers)
            {
                Net.Send(peer, Channel, text, false);
            }
        }

        internal static void OnPacket(ulong sender, string text)
        {
            string[] parts = text.Split('|');
            if (parts.Length != 6 || parts[0].Length == 0 || parts[0].Length > MaxAreaLength) return;
            float x, y, z, turn;
            if (!ReadValue(parts[1], out x) || !ReadValue(parts[2], out y) || !ReadValue(parts[3], out z) || !ReadValue(parts[4], out turn)) return;
            if (parts[5] != "0" && parts[5] != "1") return;
            string area = Net.Plain(parts[0], MaxAreaLength);
            PresenceRecord old;
            string name = records.TryGetValue(sender, out old) ? old.Name : "";
            if (string.IsNullOrEmpty(name)) name = Steam.Name(sender);
            records[sender] = new PresenceRecord
            {
                Peer = sender,
                Name = name,
                Area = area.Length > 0 ? area : "?",
                Position = new Vector3(x, y, z),
                Yaw = turn,
                Moving = parts[5] == "1",
                LastSeen = Time.unscaledTime
            };
        }

        private static string Format()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1:F3}|{2:F3}|{3:F3}|{4:F1}|{5}", sampleArea, samplePosition.x, samplePosition.y, samplePosition.z, sampleYaw, sampleMoving ? 1 : 0);
        }

        private static void Prune(float now)
        {
            if (records.Count == 0) return;
            gone.Clear();
            foreach (PresenceRecord record in records.Values)
            {
                if (now - record.LastSeen > StaleAfter || !Lobby.IsPeer(record.Peer)) gone.Add(record.Peer);
            }
            foreach (ulong id in gone)
            {
                records.Remove(id);
            }
        }

        private static bool ReadValue(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && IsValue(value);
        }

        private static bool IsValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= MaxValue;
        }
    }
}
