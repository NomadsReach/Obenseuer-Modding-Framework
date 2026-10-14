using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class StartChoiceInfo
    {
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public int Oc { get; private set; }
        public int Rm { get; private set; }

        internal StartChoiceInfo(string id, string title, string description, int oc, int rm)
        {
            Id = id;
            Title = title;
            Description = description;
            Oc = oc;
            Rm = rm;
        }
    }

    public sealed class StartChoiceArgs
    {
        public string Id { get; private set; }
        public string Title { get; private set; }

        internal StartChoiceArgs(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }

    public static class StartChoices
    {
        internal const string Owner = "obenseuer.moddingframework.startchoices";
        private static bool installed;

        public static event Action<StartChoiceArgs> Chosen;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static string Relationship
        {
            get
            {
                if (StartManager.instance == null) return null;
                try
                {
                    foreach (StartSelection.Selection selection in Picks())
                    {
                        if (selection == null) continue;
                        if (selection.id == "Family") return "Married";
                        if (selection.id == "Girlfriend" || selection.id == "Boyfriend") return "In a relationship";
                    }
                    return "Single";
                }
                catch (Exception e)
                {
                    ApiLog.Once(Owner, "Relationship", e);
                    return "Single";
                }
            }
        }

        public static List<StartChoiceInfo> Selected()
        {
            List<StartChoiceInfo> choices = new List<StartChoiceInfo>();
            try
            {
                foreach (StartSelection.Selection selection in Picks())
                {
                    if (selection == null) continue;
                    choices.Add(new StartChoiceInfo(selection.id, selection.title, selection.description, selection.oc, selection.rm));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Selected", e);
            }
            return choices;
        }

        public static bool Has(string id)
        {
            if (id == null)
            {
                ApiLog.Once(Owner, "Has", "id must not be null");
                return false;
            }
            try
            {
                foreach (StartSelection.Selection selection in Picks())
                {
                    if (selection != null && selection.id == id) return true;
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Has", e);
            }
            return false;
        }

        internal static void Init()
        {
            installed = StartChoicesPatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("start choice events not installed, Chosen is disabled");
        }

        internal static void OnChosen(StartSelection.Selection selection)
        {
            if (selection == null) return;
            StartChoiceArgs args = new StartChoiceArgs(selection.id, selection.title);
            Events.Raise(Chosen, "StartChoices.Chosen", h => h(args));
        }

        private static List<StartSelection.Selection> Picks()
        {
            StartManager manager = StartManager.instance;
            return manager == null || manager.selectedSelections == null ? new List<StartSelection.Selection>() : manager.selectedSelections;
        }
    }
}
