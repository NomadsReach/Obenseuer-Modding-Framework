using System;
using System.IO;
using System.Reflection;
using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.webuihello", "WebUiHello", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.10.0")]
[BepInDependency("obenseuer.moddingframework.webui", "0.10.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
[BepInDependency("obenseuer.moddingframework.webui", ">=0.10.0")]
#endif
public class WebUiHello : BaseUnityPlugin
{
    private const string ModId = "example.webuihello";
    private WebView view;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;
        if (view != null && !view.IsClosed)
        {
            if (view.IsVisible) view.Hide();
            else view.Show();
            return;
        }
        string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "web");
        view = WebUi.Open(ModId, folder, "index.html", true);
        if (view == null)
        {
            Logger.LogWarning("WebUiHello: the view did not open: " + WebUi.LastError);
            return;
        }
        view.Message += OnMessage;
    }

    private void OnMessage(string json)
    {
        if (json.Contains("\"ping\"")) view.Post("{\"type\":\"pong\",\"at\":" + Time.frameCount + "}");
    }
}
