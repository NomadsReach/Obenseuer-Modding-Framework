using System.Collections.Generic;

namespace ObenseuerCore
{
    internal sealed class HudHintRequest
    {
        internal string ModId;
        internal int Seq;
        internal string Title;
        internal string[] Lines;
        internal HintStyle Style;
    }

    internal sealed class HudHintQueue
    {
        private readonly Dictionary<string, HudHintRequest> items = new Dictionary<string, HudHintRequest>();
        private int seq;

        internal int Count
        {
            get { return items.Count; }
        }

        internal bool Add(string modId, string title, string[] lines, HintStyle style)
        {
            if (modId != null)
            {
                HudHintRequest existing;
                if (items.TryGetValue(modId, out existing) && Same(existing, title, lines, style)) return true;
            }
            seq++;
            HudHintRequest request = new HudHintRequest();
            request.ModId = modId;
            request.Seq = seq;
            request.Title = title;
            request.Lines = Copy(lines);
            request.Style = style;
            items[modId] = request;
            return true;
        }

        private static string[] Copy(string[] lines)
        {
            return lines == null ? null : (string[])lines.Clone();
        }

        private static bool Same(HudHintRequest request, string title, string[] lines, HintStyle style)
        {
            if (request.Style != style || request.Title != title) return false;
            int held = request.Lines == null ? 0 : request.Lines.Length;
            int count = lines == null ? 0 : lines.Length;
            if (held != count) return false;
            for (int i = 0; i < count; i++)
            {
                if (request.Lines[i] != lines[i]) return false;
            }
            return true;
        }

        internal bool Remove(string modId)
        {
            if (modId == null) return false;
            return items.Remove(modId);
        }

        internal bool Showing(string modId)
        {
            return modId != null && items.ContainsKey(modId);
        }

        internal HudHintRequest Top()
        {
            HudHintRequest top = null;
            foreach (HudHintRequest request in items.Values)
            {
                if (top == null || request.Seq > top.Seq) top = request;
            }
            return top;
        }

        internal void Clear()
        {
            items.Clear();
        }
    }

    internal sealed class HudPromptRequest
    {
        internal string ModId;
        internal string Text;
        internal string Action;
        internal ModKeybind Bind;
        internal int Seq;
        internal float Remaining;
        internal bool Timed;
        internal PromptStyle Style;
    }

    internal sealed class HudPromptSlot
    {
        private HudPromptRequest current;
        private int seq;

        internal HudPromptRequest Current
        {
            get { return current; }
        }

        internal bool Set(string modId, string text, string action, ModKeybind bind, float seconds, PromptStyle style)
        {
            if (current != null && current.ModId == modId && Same(current, text, action, bind, style))
            {
                current.Timed = HudHintLogic.ValidSeconds(seconds);
                current.Remaining = current.Timed ? seconds : 0f;
                return true;
            }
            seq++;
            HudPromptRequest request = new HudPromptRequest();
            request.ModId = modId;
            request.Text = text;
            request.Action = action;
            request.Bind = bind;
            request.Seq = seq;
            request.Timed = HudHintLogic.ValidSeconds(seconds);
            request.Remaining = request.Timed ? seconds : 0f;
            request.Style = style;
            current = request;
            return true;
        }

        internal bool Remove(string modId)
        {
            if (current == null || modId == null || current.ModId != modId) return false;
            current = null;
            return true;
        }

        internal bool Showing(string modId)
        {
            return current != null && modId != null && current.ModId == modId;
        }

        internal void Clear()
        {
            current = null;
        }

        private static bool Same(HudPromptRequest request, string text, string action, ModKeybind bind, PromptStyle style)
        {
            if (request.Style != style || request.Text != text || request.Action != action) return false;
            if (request.Bind == null || bind == null) return request.Bind == null && bind == null;
            return request.Bind == bind || request.Bind.Key == bind.Key;
        }

        internal bool TickVisible(float delta)
        {
            if (current == null) return false;
            if (!current.Timed) return false;
            if (delta < 0f) delta = 0f;
            current.Remaining -= delta;
            if (current.Remaining > 0f) return false;
            current = null;
            return true;
        }
    }
}
