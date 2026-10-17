using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class HudHintsSelfTest
    {
        internal static string Run()
        {
            try
            {
                string error = Queue();
                if (error != null) return error;
                error = Prompts();
                if (error != null) return error;
                error = Refusals();
                if (error != null) return error;
                error = Targets();
                if (error != null) return error;
                error = Tokens();
                if (error != null) return error;
                error = Tutorials();
                if (error != null) return error;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "hud hints self-check ok";
        }

        private static string Queue()
        {
            HudHintQueue queue = new HudHintQueue();
            string[] lines = new string[] { "one" };
            if (!queue.Add("mod.a", "A", lines, HintStyle.Framework)) return "the queue rejected an entry";
            if (!queue.Add("mod.b", "B", lines, HintStyle.GameHintBox)) return "the queue rejected an entry";
            if (!queue.Add("mod.c", "C", lines, HintStyle.Framework)) return "the queue rejected an entry";
            if (queue.Top() == null || queue.Top().ModId != "mod.c") return "the newest hint is not drawn";
            int newest = queue.Top().Seq;
            if (!queue.Remove("mod.b")) return "a hint behind the top one was not removed";
            if (queue.Count != 2) return "removing a hint behind the top one left the count wrong";
            if (queue.Top() == null || queue.Top().ModId != "mod.c" || queue.Top().Seq != newest) return "removing a hint behind the top one changed the drawn hint";
            if (!queue.Add("mod.a", "A2", lines, HintStyle.Framework)) return "the queue rejected a replacement";
            if (queue.Top() == null || queue.Top().ModId != "mod.a" || queue.Top().Title != "A2") return "the replacement is not drawn";
            int seq = queue.Top().Seq;
            if (!queue.Add("mod.a", "A2", new string[] { "one" }, HintStyle.Framework)) return "the queue rejected an identical replacement";
            if (queue.Top().Seq != seq) return "an identical replacement grew the queue sequence";
            if (!queue.Add("mod.a", "A2", lines, HintStyle.GameHintBox)) return "the queue rejected a style change";
            if (queue.Top().Seq == seq) return "a style change kept the queue sequence";
            if (!queue.Add("mod.a", "A3", lines, HintStyle.GameHintBox)) return "the queue rejected a title change";
            if (queue.Top().Seq == seq) return "a title change kept the queue sequence";
            string[] mutable = new string[] { "start" };
            if (!queue.Add("mod.d", "D", mutable, HintStyle.Framework)) return "the queue rejected an entry with a live array";
            mutable[0] = "changed";
            int held = queue.Top().Seq;
            if (!queue.Add("mod.d", "D", new string[] { "start" }, HintStyle.Framework)) return "the queue rejected the unchanged lines";
            if (queue.Top().Seq != held) return "a line array changed after it was stored";
            if (!queue.Remove("mod.a")) return "a hint below the top one was not removed";
            if (queue.Top() == null || queue.Top().ModId != "mod.d") return "removing a hint below the top one changed the drawn hint";
            if (!queue.Remove("mod.d")) return "the newest hint was not removed";
            if (queue.Top() == null || queue.Top().ModId != "mod.c") return "hiding the newest hint did not bring the previous one back";
            if (!queue.Remove("mod.c")) return "the last hint was not removed";
            if (queue.Top() != null || queue.Count != 0) return "a hint stayed after the queue emptied";
            if (queue.Remove("mod.zz")) return "an unknown mod id was reported as removed";
            if (!queue.Add("mod.b", "B", lines, HintStyle.GameHintBox)) return "the queue rejected an entry after it emptied";
            if (!queue.Showing("mod.b") || queue.Showing("mod.a")) return "the wrong mod id is reported as showing";
            queue.Clear();
            if (queue.Count != 0 || queue.Top() != null) return "the scene load clear left hints behind";
            return null;
        }

        private static string Prompts()
        {
            HudPromptSlot slot = new HudPromptSlot();
            if (!slot.Set("mod.a", "one", "Jump", null, 5f, PromptStyle.Framework)) return "the prompt slot rejected an entry";
            if (!slot.Showing("mod.a") || slot.Showing("mod.b")) return "the wrong prompt mod id is reported as showing";
            if (!slot.Set("mod.b", "two", null, null, 5f, PromptStyle.GameTutorial)) return "the prompt slot rejected a replacement";
            if (slot.Showing("mod.a")) return "the replaced prompt is still showing";
            if (slot.Remove("mod.a")) return "another mod id removed the prompt";
            if (!slot.Remove("mod.b") || slot.Current != null) return "the owning mod id did not remove the prompt";
            slot.Set("mod.a", "three", "Jump", null, 2f, PromptStyle.Framework);
            if (slot.TickVisible(1f)) return "the prompt expired before its time ran out";
            if (slot.Current == null) return "the visible prompt left the slot early";
            if (!slot.TickVisible(1f)) return "the prompt did not expire when its time ran out";
            if (slot.Current != null) return "the expired prompt is still in the slot";
            slot.Set("mod.a", "four", "Jump", null, 0.5f, PromptStyle.Framework);
            if (slot.Current == null) return "the second prompt did not take the slot";
            if (!slot.Set("mod.a", "sticky", "Jump", null, HudHintLogic.PersistentSeconds, PromptStyle.Framework)) return "the prompt slot rejected a prompt with no timeout";
            if (slot.Current == null) return "a prompt with no timeout did not take the slot";
            if (slot.TickVisible(600f)) return "a prompt with no timeout expired";
            if (slot.Current == null || slot.Current.ModId != "mod.a") return "a prompt with no timeout left the slot";
            if (!slot.Remove("mod.a") || slot.Current != null) return "removing a prompt with no timeout failed";
            if (!slot.Set("mod.a", "five", "Jump", null, 5f, PromptStyle.Framework)) return "the prompt slot rejected an entry";
            int same = slot.Current.Seq;
            if (!slot.Set("mod.a", "five", "Jump", null, 5f, PromptStyle.Framework)) return "the prompt slot rejected an identical entry";
            if (slot.Current.Seq != same) return "an identical prompt grew the prompt sequence";
            if (slot.TickVisible(4f)) return "an identical prompt expired too early";
            if (!slot.Set("mod.a", "five", "Jump", null, 5f, PromptStyle.Framework)) return "the prompt slot rejected a refresh";
            if (slot.TickVisible(4f)) return "the refreshed prompt did not get its time back";
            if (slot.Current == null || slot.Current.ModId != "mod.a") return "the refreshed prompt left the slot";
            if (!slot.Set("mod.a", "six", "Jump", null, 5f, PromptStyle.Framework)) return "the prompt slot rejected a change";
            if (slot.Current.Seq == same) return "a changed prompt kept the prompt sequence";
            if (!slot.Remove("mod.a") || slot.Current != null) return "the refreshed prompt was not removed";
            slot.Clear();
            if (slot.Current != null) return "the scene load clear left the prompt behind";
            return null;
        }

        private static string Refusals()
        {
            if (HudHintLogic.Refuse(false, true) != HudRefusal.SettingOff) return "an off setting did not refuse";
            if (HudHintLogic.Refuse(true, false) != HudRefusal.NoUi) return "a missing game UI did not refuse";
            if (HudHintLogic.Refuse(false, false) != HudRefusal.SettingOff) return "the off setting did not win over the missing game UI";
            if (HudHintLogic.Refuse(true, true) != HudRefusal.None) return "a ready hint was refused";
            if (HudHintLogic.ValidSeconds(0f) || HudHintLogic.ValidSeconds(-1f)) return "a non positive timeout was accepted";
            if (HudHintLogic.ValidSeconds(float.NaN) || HudHintLogic.ValidSeconds(float.PositiveInfinity)) return "a non finite timeout was accepted";
            if (!HudHintLogic.ValidSeconds(8f)) return "a positive timeout was refused";
            HashSet<string> known = new HashSet<string>();
            known.Add("Jump");
            if (!HudHintLogic.KnownAction("Jump", known)) return "a known action was not found";
            if (HudHintLogic.KnownAction("Fly", known)) return "an unknown action was found";
            if (HudHintLogic.KnownAction("Jump", null) || HudHintLogic.KnownAction("Jump", new HashSet<string>())) return "an empty action list reported a known action";
            return null;
        }

        private static string Targets()
        {
            if (HudHintLogic.HintTarget(false, false, false, true, HintStyle.GameHintBox) != HudHintTarget.GameBox) return "a game box hint was not drawn while the setting is off";
            if (!HudHintLogic.BoxHidden(false)) return "a game box hint is not hidden while the setting is off";
            if (HudHintLogic.BoxHidden(true)) return "a game box hint stays hidden while the setting is on";
            if (HudHintLogic.HintTarget(true, false, false, true, HintStyle.GameHintBox) != HudHintTarget.GameBox) return "a game box hint was not drawn while the setting is on";
            if (HudHintLogic.HintTarget(false, false, false, false, HintStyle.GameHintBox) != HudHintTarget.None) return "a game box hint with no box and the setting off was not dropped";
            if (HudHintLogic.HintTarget(true, false, false, false, HintStyle.GameHintBox) != HudHintTarget.Framework) return "a game box hint with no box did not fall back";
            if (HudHintLogic.HintTarget(false, false, false, true, HintStyle.Framework) != HudHintTarget.None) return "a framework hint was drawn with the setting off";
            if (HudHintLogic.HintTarget(true, true, false, true, HintStyle.Framework) != HudHintTarget.None) return "the manual hide did not hide a framework hint";
            if (HudHintLogic.HintTarget(true, false, false, true, HintStyle.Framework) != HudHintTarget.Framework) return "a ready framework hint was not drawn";
            if (HudHintLogic.HintTarget(true, false, true, true, HintStyle.GameHintBox) != HudHintTarget.None) return "a hidden frame still drew a hint";
            if (HudHintLogic.PromptTarget(false, false, PromptStyle.GameTutorial, false, true, false) != HudPromptTarget.None) return "a tutorial prompt was drawn with the setting off";
            if (HudHintLogic.PromptTarget(true, false, PromptStyle.GameTutorial, false, true, false) != HudPromptTarget.Tutorial) return "a tutorial prompt did not use the game tutorial";
            if (HudHintLogic.PromptTarget(true, false, PromptStyle.GameTutorial, true, true, false) != HudPromptTarget.Framework) return "a keybind prompt did not use the framework panel";
            if (HudHintLogic.PromptTarget(true, false, PromptStyle.GameTutorial, false, false, false) != HudPromptTarget.Framework) return "an unavailable tutorial did not fall back";
            if (HudHintLogic.PromptTarget(true, false, PromptStyle.GameTutorial, false, true, true) != HudPromptTarget.None) return "a blocked tutorial still drew a prompt";
            return null;
        }

        private static string Tutorials()
        {
            if (!HudHintLogic.TutorialClearAllowed("Press E", "Press E", false)) return "the framework did not clear its own tutorial prompt";
            if (HudHintLogic.TutorialClearAllowed("Reload", "Press E", false)) return "the framework cleared the game tutorial prompt";
            if (HudHintLogic.TutorialClearAllowed("Press E", "Press E", true)) return "the framework cleared a prompt while the guide owns the tutorial";
            if (HudHintLogic.TutorialClearAllowed("Press E", null, false)) return "the framework cleared a prompt it never drew";
            if (HudHintLogic.TutorialClearAllowed(null, "Press E", false)) return "the framework cleared a prompt with no game text";
            if (HudHintLogic.TutorialClearAllowed(null, null, false)) return "the framework cleared with no text at all";
            return null;
        }

        private static string Tokens()
        {
            int unknown = 0;
            string text = HudHintLogic.Resolve("press {key:Jump} now", name => name == "Jump" ? "Space" : null, name => unknown++);
            if (text != "press Space now") return "a known key token was not resolved: " + text;
            string kept = HudHintLogic.Resolve("{key:Fly}", name => null, name => unknown++);
            if (kept != "{key:Fly}") return "an unknown key token was not kept literal: " + kept;
            if (unknown != 1) return "the unknown key token callback ran " + unknown + " times";
            if (HudHintLogic.Resolve("plain text", name => "Space", null) != "plain text") return "text without a token changed";
            if (HudHintLogic.Resolve("{key:Jump}", null, null) != "{key:Jump}") return "a missing lookup changed the text";
            if (HudHintLogic.Resolve(null, name => "Space", null) != null) return "a null text changed";
            return null;
        }
    }
}
