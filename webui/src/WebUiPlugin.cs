using System;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Unity.Mono;

namespace ObenseuerCore
{
    [BepInPlugin("obenseuer.moddingframework.webui", "Obenseuer Modding Framework WebUi", "0.12.1")]
#if BEPINEX5
    [BepInDependency("obenseuer.moddingframework", "0.11.0")]
#else
    [BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
#endif
    public class WebUiPlugin : BaseUnityPlugin
    {
        private const string CoreConfig = "obenseuer.moddingframework.cfg";
        private static readonly string[] Themes = { "light", "dark", "system" };
        private static readonly Action tickMenu = () => ModMenuWebHost.Tick();
        private static readonly Action tickWebUi = () => WebUi.Tick();

        private void Awake()
        {
            string carried = CarriedTheme();
            Core.Isolate("webui", () => WebUi.Init(Config, carried ?? "light"));
            Core.Isolate("mod menu web", () =>
            {
                ModMenuWindow.StartWeb = ModMenuWebHost.Start;
                ModMenuWindow.WebBusy = () => ModMenuWebHost.Busy;
            });
            Core.Isolate("web menu check", () =>
            {
                if (Config.Bind("Debug", "WebMenuSelfCheck", false, "Run the web mod menu JSON writer and key-map self-check at start and log the result.").Value)
                {
                    string error = ModMenuJson.SelfCheck() ?? ModMenuActions.SelfCheck() ?? ModMenuWebKeys.SelfCheck() ?? ModMenuWebConfig.SelfCheck() ?? ModMenuWebHost.SelfCheck();
                    CoreLog.For(WebUi.Owner).Info(error == null ? "web menu self-check ok" : "web menu self-check failed: " + error);
                }
            });
        }

        private void Update()
        {
            Core.Guard("ModMenuWebHost.Tick", tickMenu);
            Core.Guard("WebUi.Tick", tickWebUi);
        }

        private void OnApplicationQuit()
        {
            try
            {
                WebUi.Shutdown();
            }
            catch (Exception e)
            {
                CoreLog.For(WebUi.Owner).Error("WebUi shutdown failed: " + e.ToString());
            }
        }

        private string CarriedTheme()
        {
            string path = Config.ConfigFilePath;
            string old = Path.Combine(Path.GetDirectoryName(path), CoreConfig);
            try
            {
                string theme = ValueIn(old);
                if (theme == null || Array.IndexOf(Themes, theme) < 0 || ValueIn(path) != null) return null;
                CoreLog.For(WebUi.Owner).Info("WebTheme " + theme + " carried over from " + CoreConfig);
                return theme;
            }
            catch (Exception e)
            {
                CoreLog.For(WebUi.Owner).Warn("WebTheme not carried over from " + CoreConfig + " (" + e.Message + "). Set [Appearance] WebTheme by hand.");
                return null;
            }
        }

        private static string ValueIn(string file)
        {
            if (!File.Exists(file)) return null;
            Match m = Regex.Match(File.ReadAllText(file), @"(?m)^WebTheme = (\S+)");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
