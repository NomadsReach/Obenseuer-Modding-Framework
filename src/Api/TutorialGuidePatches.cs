using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class TutorialGuidePatches
    {

        internal static bool Install()
        {
            try
            {
                return CorePatcher.PatchCore(typeof(Tutorial), "Update", Type.EmptyTypes, null, CorePatcher.Handler(typeof(TutorialGuidePatches), "UpdatePostfix"), null, true, TutorialGuide.Owner);
            }
            catch (Exception e)
            {
                CoreLog.For(TutorialGuide.Owner).Error("tutorial patch install failed: " + e.ToString());
                return false;
            }
        }

        private static void UpdatePostfix(bool ___LookAroundTutorial, bool ___MoveTutorial, bool ___UseTutorial, bool ___MenuTutorial)
        {
            if (!TutorialGuide.HasHandlers) return;
            try
            {
                TutorialGuide.Observe(___LookAroundTutorial, ___MoveTutorial, ___UseTutorial, ___MenuTutorial);
            }
            catch (Exception e)
            {
                ApiLog.Once(TutorialGuide.Owner, "Update postfix", e);
            }
        }
    }
}
