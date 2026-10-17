using BepInEx.Configuration;

namespace ObenseuerCore
{
    internal static class HudHintSettings
    {
        private static ConfigEntry<bool> showModHints;
        private static ConfigEntry<bool> showModPrompts;

        internal static void Bind(ConfigFile config)
        {
            showModHints = config.Bind("Hints", "ShowModHints", true, new ConfigDescription("Show hints from mods. Separate from the game's own Hints key.", (AcceptableValueBase)null, new object[] { new ModMenuTitle("Show mod hints") }));
            showModPrompts = config.Bind("Hints", "ShowModPrompts", true, new ConfigDescription("Show key prompts from mods.", (AcceptableValueBase)null, new object[] { new ModMenuTitle("Show mod key prompts") }));
        }

        internal static bool ShowModHints
        {
            get { return showModHints == null || showModHints.Value; }
        }

        internal static bool ShowModPrompts
        {
            get { return showModPrompts == null || showModPrompts.Value; }
        }
    }
}
