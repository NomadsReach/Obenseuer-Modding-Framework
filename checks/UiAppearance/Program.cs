using System;
using System.IO;
using BepInEx.Configuration;
using ObenseuerCore.Ui;

internal static class Program
{
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); }

    private static void Main()
    {
        const string core = "[Other]\nWebTheme = system\n[Appearance]\nWebTheme = light\n";
        const string web = "[Appearance]\nWebTheme = dark\n";
        Check(UiAppearanceRules.Initial(core, web) == "dark", "web setting takes priority over legacy core");
        Check(UiAppearanceRules.Initial("[Appearance]\nWebTheme = dark\n", "[Appearance]\nWebTheme = light\n") == "light", "web light overrides legacy dark");
        Check(UiAppearanceRules.Initial(core + "Theme = system\n", web) == "system", "canonical setting takes priority");
        Check(UiAppearanceRules.Initial(core, "[Other]\nWebTheme = dark\n") == "light", "ignore unrelated sections");
        Check(UiAppearanceRules.Initial("[Appearance]\nWebTheme = bad\n", null) == "light", "invalid mode falls back");
        Check(UiAppearanceRules.Resolve("system", true) == "dark" && UiAppearanceRules.Resolve("system", false) == "light", "system preference");
        string dir = Path.Combine(Path.GetTempPath(), "obx-appearance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "obenseuer.moddingframework.cfg");
            File.WriteAllText(path, core);
            File.WriteAllText(Path.Combine(dir, "obenseuer.moddingframework.webui.cfg"), web);
            UiAppearance.Init(new ConfigFile { ConfigFilePath = path });
            Check(UiAppearance.IsAvailable && UiAppearance.Mode == "dark" && UiAppearance.Theme == "dark", "migrated setting initializes");
            int events = 0;
            UiAppearance.Changed += value => { events++; Check(value == "light", "event resolved value"); };
            UiAppearance.SetMode("light");
            Check(events == 0 && UiAppearance.Theme == "dark", "changes wait for update");
            UiAppearance.Tick();
            Check(events == 1 && UiAppearance.Theme == "light", "update publishes theme");
            UiAppearance.SetMode("system"); UiAppearance.Tick();
            Check(events == 2 && UiAppearance.Mode == "system" && UiAppearance.Theme == "light", "mode changes publish even when resolved theme is unchanged");
            UiAppearance.SetMode("invalid"); UiAppearance.Tick();
            Check(events == 2 && UiAppearance.Mode == "system", "invalid mode refused");
        }
        finally { Directory.Delete(dir, true); }
        Console.WriteLine("UI appearance checks passed.");
    }
}
