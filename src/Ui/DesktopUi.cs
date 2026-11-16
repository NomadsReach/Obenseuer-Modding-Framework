using System;
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

        public static void MakeSelectable(RectTransform tile, Action activate)
        {
            DesktopSelection selection = tile.gameObject.AddComponent<DesktopSelection>();
            selection.Activate = activate;
        }
    }

    internal sealed class DesktopSelection : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        internal Action Activate;
        private bool hovered;
        private bool selected;
        private Image background;
        private Outline border;
        private Color normal;

        private void Awake()
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
                background.color = Color.clear;
            }
            normal = background.color;
            border = gameObject.AddComponent<Outline>();
            border.effectColor = new Color32(120, 170, 230, 178);
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = false;
            border.enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }

        public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }

        public void OnSelect(BaseEventData eventData) { selected = true; Refresh(); }

        public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(); }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
            if (eventData.clickCount >= 2 && Activate != null) Activate();
        }

        private void Refresh()
        {
            background.color = selected ? (Color)new Color32(120, 170, 230, 77) : hovered ? (Color)new Color32(255, 255, 255, 20) : normal;
            border.enabled = selected;
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
