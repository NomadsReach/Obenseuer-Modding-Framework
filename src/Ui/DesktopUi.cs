using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public static class DesktopUi
    {
        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            item.GetComponent<Image>().color = color;
            return (RectTransform)item.transform;
        }

        public static Text Label(Transform parent, Font font, string text, int size, Color color, TextAnchor alignment)
        {
            GameObject item = new GameObject("Text", typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            Text label = item.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.text = text;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void TopLeft(RectTransform rect, float x, float y, float width, float height, float scale)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x * scale, -y * scale);
            rect.sizeDelta = new Vector2(width * scale, height * scale);
        }

        public static void MakeDraggable(RectTransform handle, RectTransform window)
        {
            DesktopDrag drag = handle.gameObject.AddComponent<DesktopDrag>();
            drag.Window = window;
        }
    }

    internal sealed class DesktopDrag : MonoBehaviour, IDragHandler
    {
        internal RectTransform Window;

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform parent = Window.parent as RectTransform;
            Vector2 previous;
            Vector2 current;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position - eventData.delta, eventData.pressEventCamera, out previous) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out current))
                Window.anchoredPosition += current - previous;
        }
    }
}
