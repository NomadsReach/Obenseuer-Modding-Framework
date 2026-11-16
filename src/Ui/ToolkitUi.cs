using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ObenseuerCore.Ui
{
    public static class ToolkitUi
    {
        public static VisualElement CreateOverlay(Transform host)
        {
            if (host == null) throw new ArgumentNullException("host");
            GameObject panel = new GameObject("ObenseuerCore UI Toolkit panel");
            panel.SetActive(false);
            panel.transform.SetParent(host, false);
            ToolkitPanel owner = panel.AddComponent<ToolkitPanel>();
            try
            {
                return owner.Create();
            }
            catch
            {
                owner.Release();
                UnityEngine.Object.Destroy(panel);
                throw;
            }
        }
    }

    internal sealed class ToolkitPanel : MonoBehaviour
    {
        private PanelSettings settings;

        internal VisualElement Create()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.sortingOrder = 600;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            gameObject.SetActive(true);
            return document.rootVisualElement;
        }

        private void OnDestroy()
        {
            Release();
        }

        internal void Release()
        {
            if (settings != null) Destroy(settings);
            settings = null;
        }
    }
}
