using System;
using System.Reflection;
using HarmonyLib;

namespace ObenseuerCore
{
    public enum TutorialStep
    {
        None = 0,
        LookAround = 1,
        Move = 2,
        Interact = 3,
        Menu = 4
    }

    public sealed class TutorialStepArgs
    {
        public TutorialStep Previous { get; private set; }
        public TutorialStep Step { get; private set; }

        internal TutorialStepArgs(TutorialStep previous, TutorialStep step)
        {
            Previous = previous;
            Step = step;
        }
    }

    public static class TutorialGuide
    {
        internal const string Owner = "obenseuer.moddingframework.tutorial";
        private static readonly FieldInfo activeField = AccessTools.Field(typeof(Tutorial), "TutorialActive");
        private static readonly FieldInfo lookField = AccessTools.Field(typeof(Tutorial), "LookAroundTutorial");
        private static readonly FieldInfo moveField = AccessTools.Field(typeof(Tutorial), "MoveTutorial");
        private static readonly FieldInfo interactField = AccessTools.Field(typeof(Tutorial), "UseTutorial");
        private static readonly FieldInfo menuField = AccessTools.Field(typeof(Tutorial), "MenuTutorial");
        private static bool installed;
        private static TutorialStep lastStep = TutorialStep.None;
        private static Action<TutorialStepArgs> stepChanged;

        public static event Action<TutorialStepArgs> StepChanged
        {
            add
            {
                if (stepChanged == null) lastStep = Step;
                stepChanged += value;
            }
            remove
            {
                stepChanged -= value;
            }
        }

        internal static bool HasHandlers
        {
            get
            {
                return stepChanged != null;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static bool Active
        {
            get
            {
                return Guard(() => FlagOn(activeField), false, "Active");
            }
        }

        public static TutorialStep Step
        {
            get
            {
                return Guard(() => StepNow(), TutorialStep.None, "Step");
            }
        }

        internal static void Init()
        {
            installed = activeField != null && lookField != null && moveField != null && interactField != null && menuField != null && TutorialGuidePatches.Install();
            if (installed) return;
            CorePatcher.DisableOwner(Owner);
            CoreLog.For(Owner).Warn("tutorial events not installed, Step and StepChanged are disabled");
        }

        internal static void Observe(bool look, bool move, bool interact, bool menu)
        {
            TutorialStep now = StepOf(look, move, interact, menu);
            if (now == lastStep) return;
            TutorialStepArgs args = new TutorialStepArgs(lastStep, now);
            lastStep = now;
            Events.Raise(stepChanged, "TutorialGuide.StepChanged", h => h(args));
        }

        private static TutorialStep StepNow()
        {
            if (Tutorial.instance == null) return TutorialStep.None;
            return StepOf(FlagOn(lookField), FlagOn(moveField), FlagOn(interactField), FlagOn(menuField));
        }

        private static TutorialStep StepOf(bool look, bool move, bool interact, bool menu)
        {
            if (look) return TutorialStep.LookAround;
            if (move) return TutorialStep.Move;
            if (interact) return TutorialStep.Interact;
            if (menu) return TutorialStep.Menu;
            return TutorialStep.None;
        }

        private static bool FlagOn(FieldInfo field)
        {
            Tutorial tutorial = Tutorial.instance;
            return tutorial != null && (bool)field.GetValue(tutorial);
        }

        private static T Guard<T>(Func<T> read, T fallback, string name)
        {
            try
            {
                return read();
            }
            catch (Exception e)
            {
                CoreLog.For(Owner).Debug(name, name + " failed: " + e.ToString());
                return fallback;
            }
        }
    }
}
