using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObenseuerCore
{
    public static class Hud
    {
        private const string NotifyType = "Notice";
        private const float SubtitleCharactersPerSecond = 20f;
        private static readonly Dictionary<string, HintOwner> hintOwners = new Dictionary<string, HintOwner>();

        public static bool Notify(string text)
        {
            RequireText(text, "text");
            if (Notifications.instance == null) return false;
            try
            {
                Notifications.instance.CreateNotification(NotifyType, text);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "Notify", e);
                return false;
            }
        }

        public static bool ShowSubtitle(string text, float fadeSeconds)
        {
            RequireText(text, "text");
            if (fadeSeconds < 0f) throw new ArgumentException("fadeSeconds must not be negative", "fadeSeconds");
            if (Subtitles.instance == null) return false;
            try
            {
                Subtitles.instance.ShowLine(text, fadeSeconds, SubtitleCharactersPerSecond);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowSubtitle", e);
                return false;
            }
        }

        public static bool ShowHint(string modId, string title, string[] lines)
        {
            ModIds.Require(modId, "modId");
            RequireText(title, "title");
            if (lines == null) throw new ArgumentNullException("lines");
            foreach (string line in lines)
            {
                if (line == null) throw new ArgumentException("lines must not contain null", "lines");
            }
            if (HintOverlay.instance == null) return false;
            try
            {
                HintOverlay.Show(OwnerFor(modId), new HintContent(title).AddText(null, lines));
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowHint", e);
                return false;
            }
        }

        public static bool HideHint(string modId)
        {
            ModIds.Require(modId, "modId");
            HintOwner owner;
            if (!hintOwners.TryGetValue(modId, out owner)) return false;
            HintOverlay.Hide(owner);
            return true;
        }

        public static bool ShowNote(string noteId)
        {
            RequireText(noteId, "noteId");
            NoteController controller = NoteController.instance;
            if (controller == null) return false;
            try
            {
                NoteInfo found = null;
                int matches = 0;
                foreach (CollectedNoteInfo entry in controller.notes)
                {
                    List<NoteInfo> pages = entry.book == null ? new List<NoteInfo> { entry.note } : entry.collectedPages;
                    foreach (NoteInfo page in pages)
                    {
                        if (page == null || page.Id != noteId) continue;
                        found = page;
                        matches++;
                    }
                }
                if (matches != 1) return false;
                controller.ReadNote(found);
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ShowNote", e);
                return false;
            }
        }

        public static bool ShowCrosshair()
        {
            return SetCrosshair(true);
        }

        public static bool HideCrosshair()
        {
            return SetCrosshair(false);
        }

        private static bool SetCrosshair(bool show)
        {
            if (Crosshair.instance == null) return false;
            try
            {
                if (show) Crosshair.instance.ShowCrosshair();
                else Crosshair.instance.HideCrosshair();
                return true;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "SetCrosshair", e);
                return false;
            }
        }

        private static HintOwner OwnerFor(string modId)
        {
            HintOwner owner;
            if (!hintOwners.TryGetValue(modId, out owner) || owner == null)
            {
                owner = ScriptableObject.CreateInstance<HintOwner>();
                hintOwners[modId] = owner;
            }
            return owner;
        }

        private static void RequireText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " must be non-empty", name);
        }
    }

    internal sealed class HintOwner : ScriptableObject
    {
    }
}
