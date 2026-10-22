using System;
using System.Text;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal sealed class DevMenuWatch : MonoBehaviour
    {
        private const float Interval = 0.25f;
        private const float Width = 480f;
        private const float Height = 400f;
        private static DevMenuWatch view;
        private TextMeshProUGUI label;
        private float next;

        internal static void Toggle()
        {
            if (view != null) Hide();
            else Show();
        }

        internal static string Read(DevWatch watch)
        {
            try
            {
                string value = watch.Value();
                return value ?? "";
            }
            catch (Exception e)
            {
                ApiLog.Once(DevMenu.Owner, "watch " + watch.ModId + " " + watch.Name, e);
                return "error";
            }
        }

        private static void Show()
        {
            GameObject go = new GameObject("ObenseuerCore.DevMenu.Watches", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            UnityEngine.Object.DontDestroyOnLoad(go);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            RectTransform panel = ModUi.NewRect("Panel", go.transform);
            ModUi.Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(UiTheme.Padding, -UiTheme.Padding - Height), new Vector2(UiTheme.Padding + Width, -UiTheme.Padding));
            ModUi.Surface(panel, UiTheme.Window, UiTheme.CardRadius);
            view = go.AddComponent<DevMenuWatch>();
            view.label = ModUi.Label(panel, "", UiTheme.MutedSize, UiTheme.Text, TextAlignmentOptions.TopLeft, false);
            ModUi.Place(view.label.rectTransform, Vector2.zero, Vector2.one, new Vector2(UiTheme.Padding, UiTheme.Padding), new Vector2(-UiTheme.Padding, -UiTheme.Padding));
        }

        private static void Hide()
        {
            UnityEngine.Object.Destroy(view.gameObject);
            view = null;
        }

        private static string Compose()
        {
            if (DevMenu.Watches.Count == 0) return "No watches. Call DevMenu.AddWatch.";
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < DevMenu.Watches.Count; i++)
            {
                DevWatch watch = DevMenu.Watches[i];
                if (i > 0) text.Append('\n');
                text.Append(watch.ModId).Append(' ').Append(watch.Name).Append(": ").Append(Read(watch));
            }
            return text.ToString();
        }

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;
            label.text = Compose();
        }

        private void OnDestroy()
        {
            if (view == this) view = null;
        }
    }
}
