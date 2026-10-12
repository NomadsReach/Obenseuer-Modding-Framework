using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class NetChannel
    {
        public string Owner;
        public Action<ulong, string> Handler;
    }

    internal static class NetChannels
    {
        private static readonly Dictionary<string, NetChannel> channels = new Dictionary<string, NetChannel>();

        internal static void CheckIdentifier(string text, string argument)
        {
            if (!IsIdentifier(text)) throw new ArgumentException("1 to " + Net.MaxIdentifierLength + " characters of letters, digits, '.', '_' or '-'", argument);
        }

        internal static bool Add(string modId, string channel, Action<ulong, string> handler)
        {
            ModIds.Require(modId, "modId");
            CheckIdentifier(channel, "channel");
            if (handler == null) throw new ArgumentNullException("handler");
            NetChannel existing;
            if (channels.TryGetValue(channel, out existing) && existing.Owner != modId) return false;
            channels[channel] = new NetChannel { Owner = modId, Handler = handler };
            return true;
        }

        internal static void Dispatch(ulong sender, string text)
        {
            int bar = text.IndexOf('|');
            if (bar <= 0) return;
            string channel = text.Substring(0, bar);
            NetChannel target;
            if (!channels.TryGetValue(channel, out target)) return;
            Events.Raise(target.Handler, "Net " + channel, h => h(sender, text.Substring(bar + 1)));
        }

        private static bool IsIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > Net.MaxIdentifierLength) return false;
            foreach (char c in text)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';
                if (!ok) return false;
            }
            return true;
        }
    }
}
