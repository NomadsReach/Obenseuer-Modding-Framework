using UnityEngine.UIElements;

namespace ObenseuerCore.Ui
{
    internal static class ToolkitTextFocus
    {
        internal static bool Contains(VisualElement focused, VisualElement root)
        {
            bool text = false;
            while (focused != null)
            {
                if (focused is TextField) text = true;
                if (focused == root) return text;
                focused = focused.parent;
            }
            return false;
        }
    }
}
