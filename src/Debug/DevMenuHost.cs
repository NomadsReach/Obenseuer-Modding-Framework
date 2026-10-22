using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class DevMenuHost : MonoBehaviour
    {
        internal static void Create()
        {
            GameObject go = new GameObject("ObenseuerCore.DevMenu");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<DevMenuHost>();
        }

        private void Update()
        {
            ModKeybind key = DevMenu.Key;
            if (key != null && key.Down()) DevMenuWindow.Toggle();
        }
    }
}
