using System;
using System.IO;
using System.Reflection;
using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;
using UnityEngine;

[BepInPlugin("example.webuispike", "WebUiSpike", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.9.0")]
[BepInDependency("obenseuer.moddingframework.webui", "0.9.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.9.0")]
[BepInDependency("obenseuer.moddingframework.webui", ">=0.9.0")]
#endif
public class WebUiSpike : BaseUnityPlugin
{
    private const string ModId = "example.webuispike";
    private const string WalletJson = "{\"type\":\"wallet\",\"cash\":1975}";
    private WebView view;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F10)) return;
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
            Logger.LogWarning("WebUiSpike: the view did not open: " + WebUi.LastError);
            return;
        }
        view.Message += OnMessage;
    }

    private void OnMessage(string json)
    {
        if (json.Contains("\"type\":\"ready\"")) view.Post(WalletJson);
        else if (json.Contains("\"type\":\"close\"")) view.Hide();
        else if (json.Contains("\"type\":\"order\"") || json.Contains("\"type\":\"error\"")) Logger.LogInfo(json);
    }
}
