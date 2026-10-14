using System;
using System.Reflection;
using ObenseuerCore.Ui;

namespace ObenseuerCore
{
    internal static class ModMenuInput
    {
        private static bool patched;

        internal static bool Ready
        {
            get { return patched && (!UiInput.Locked || UiInput.Engaged); }
        }

        internal static void Init()
        {
            bool update = Patch(typeof(GameController), "Update", Type.EmptyTypes, "SkipWhileEngaged");
            bool switched = Patch(typeof(InventoryNavigationHandler), "SwitchTab", new Type[] { typeof(InputManager.InputType), typeof(int), typeof(int) }, "SkipWhileCustomPage");
            bool navigation = Patch(typeof(InventoryNavigationHandler), "Update", Type.EmptyTypes, "SkipWhileCustomPage");
            patched = update && switched && navigation;
        }

        private static bool SkipWhileEngaged()
        {
            if (!UiInput.Engaged && !ModWindow.EscapeConsumed) return true;
            try
            {
                DialogueController.instance.CheckDialogueStatus();
            }
            catch (Exception e)
            {
                ApiLog.Once(ModMenu.Owner, "dialogue status while engaged", e);
            }
            return false;
        }

        private static bool SkipWhileCustomPage()
        {
            UIPageButtons pages = UIPageButtons.instance;
            return pages == null || !ModMenuTab.OnPage(pages);
        }

        private static bool Patch(Type type, string name, Type[] args, string prefix)
        {
            MethodInfo method = typeof(ModMenuInput).GetMethod(prefix, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return CorePatcher.PatchCore(type, name, args, method, null, null, false, ModMenu.Owner);
        }
    }
}
