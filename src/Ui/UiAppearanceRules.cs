using System;

namespace ObenseuerCore.Ui
{
    internal static class UiAppearanceRules
    {
        internal static string Mode(string value)
        {
            string mode = (value ?? "").Trim().ToLowerInvariant();
            return mode == "light" || mode == "dark" || mode == "system" ? mode : null;
        }

        internal static string Read(string text, string key)
        {
            bool appearance = false;
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    appearance = string.Equals(line, "[Appearance]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                int separator = line.IndexOf('=');
                if (appearance && separator >= 0 && string.Equals(line.Substring(0, separator).Trim(), key, StringComparison.OrdinalIgnoreCase))
                    return Mode(line.Substring(separator + 1));
            }
            return null;
        }

        internal static string Initial(string core, string web)
        {
            return Read(core, "Theme") ?? Read(web, "WebTheme") ?? Read(core, "WebTheme") ?? "light";
        }

        internal static string Resolve(string mode, bool systemDark)
        {
            return Mode(mode) == "dark" || (Mode(mode) == "system" && systemDark) ? "dark" : "light";
        }
    }
}
