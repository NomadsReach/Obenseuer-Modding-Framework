using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;

namespace ObenseuerCore
{
    internal sealed class ModMenuWebModel
    {
        private const string CoreGuid = "obenseuer.moddingframework";
        private static int opens;
        internal readonly List<WebMod> Mods = new List<WebMod>();
        private readonly List<WebPage> pages = new List<WebPage>();

        private ModMenuWebModel()
        {
        }

        internal static List<WebPlugin> Plugins()
        {
            List<WebPlugin> plugins = new List<WebPlugin>();
            foreach (PluginInfo info in Loader.All())
            {
                if (info.Instance != null) plugins.Add(new WebPlugin(info.Metadata.GUID, info.Metadata.Name, info.Metadata.Version.ToString(), ((BaseUnityPlugin)info.Instance).Config));
            }
            return plugins;
        }

        internal static ModMenuWebModel Build(List<WebPlugin> plugins, List<ModMenuPage> sorted)
        {
            opens++;
            ModMenuWebModel model = new ModMenuWebModel();
            string prefix = "p" + opens.ToString(CultureInfo.InvariantCulture) + "_";
            for (int p = 0; p < plugins.Count; p++)
            {
                WebPlugin plugin = plugins[p];
                WebMod mod = new WebMod(plugin.Guid, plugin.Name, plugin.Version);
                if (plugin.Config != null && ModMenuConfigList.Visible(plugin.Config).Count > 0)
                {
                    WebPage page = new WebPage("Settings", "config");
                    page.Config = plugin.Config;
                    mod.Pages.Add(model.Add(prefix, page));
                }
                model.Mods.Add(mod);
            }
            for (int i = 0; i < sorted.Count; i++)
            {
                ModMenuPage source = sorted[i];
                if (source.Build == null) continue;
                WebMod mod = Find(model.Mods, source.ModId);
                if (mod == null)
                {
                    mod = new WebMod(source.ModId, source.Title, null);
                    model.Mods.Add(mod);
                }
                WebPage page = new WebPage(source.Title, "classic");
                page.Classic = source;
                mod.Pages.Add(model.Add(prefix, page));
            }
            model.Mods.RemoveAll(IsEmpty);
            model.Mods.Sort(ByCoreThenName);
            return model;
        }

        internal WebPage PageFor(string id)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Id == id) return pages[i];
            }
            return null;
        }

        private WebPage Add(string prefix, WebPage page)
        {
            page.Id = prefix + pages.Count.ToString(CultureInfo.InvariantCulture);
            pages.Add(page);
            return page;
        }

        private static WebMod Find(List<WebMod> mods, string id)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Id == id) return mods[i];
            }
            return null;
        }

        private static bool IsEmpty(WebMod mod)
        {
            return mod.Pages.Count == 0;
        }

        private static int ByCoreThenName(WebMod a, WebMod b)
        {
            bool aCore = a.Id.StartsWith(CoreGuid, StringComparison.Ordinal);
            bool bCore = b.Id.StartsWith(CoreGuid, StringComparison.Ordinal);
            if (aCore != bCore) return aCore ? -1 : 1;
            return string.CompareOrdinal(a.Name, b.Name);
        }
    }

    internal sealed class WebPlugin
    {
        internal readonly string Guid;
        internal readonly string Name;
        internal readonly string Version;
        internal readonly ConfigFile Config;

        internal WebPlugin(string guid, string name, string version, ConfigFile config)
        {
            Guid = guid;
            Name = name;
            Version = version;
            Config = config;
        }
    }

    internal sealed class WebMod
    {
        internal readonly string Id;
        internal readonly string Name;
        internal readonly string Version;
        internal readonly List<WebPage> Pages = new List<WebPage>();

        internal WebMod(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = version;
        }

        internal void ToJson(ModMenuJson json)
        {
            json.Begin('{').Key("id").Str(Id).Key("name").Str(Name).Key("version").Str(Version ?? "").Key("pages").Begin('[');
            for (int i = 0; i < Pages.Count; i++) Pages[i].Summary(json);
            json.End().End();
        }
    }

    internal sealed class WebPage
    {
        internal string Id;
        internal readonly string Title;
        internal readonly string Kind;
        internal List<WebSection> Sections;
        internal ConfigFile Config;
        internal ModMenuPage Classic;

        internal WebPage(string title, string kind)
        {
            Title = title;
            Kind = kind;
        }

        internal void Summary(ModMenuJson json)
        {
            json.Begin('{').Key("id").Str(Id).Key("title").Str(Title).Key("kind").Str(Kind).End();
        }

        internal void ToJson(ModMenuJson json)
        {
            json.Begin('{').Key("v").Num(1.0).Key("type").Str("page").Key("id").Str(Id).Key("sections").Begin('[');
            if (Sections != null)
            {
                for (int i = 0; i < Sections.Count; i++) Sections[i].ToJson(json);
            }
            json.End().End();
        }
    }

    internal sealed class WebSection
    {
        internal readonly string Name;
        internal readonly List<WebRow> Rows = new List<WebRow>();

        internal WebSection(string name)
        {
            Name = name;
        }

        internal void ToJson(ModMenuJson json)
        {
            json.Begin('{').Key("name").Str(Name).Key("rows").Begin('[');
            for (int i = 0; i < Rows.Count; i++) Rows[i].ToJson(json);
            json.End().End();
        }
    }

    internal sealed class WebRow
    {
        internal string Id;
        internal string Kind;
        internal string Label;
        internal string Desc;
        internal string Value;
        internal string Default;
        internal bool Dirty;
        internal float Min;
        internal float Max;
        internal float Step;
        internal int Decimals;
        internal string[] Options;

        internal void ToJson(ModMenuJson json)
        {
            json.Begin('{').Key("id").Str(Id).Key("kind").Str(Kind).Key("label").Str(Label).Key("desc").Str(Desc ?? "");
            json.Key("value").Str(Value).Key("def").Str(Default).Key("dirty").Bool(Dirty);
            if (Kind == "slider") json.Key("min").Num(Min).Key("max").Num(Max).Key("step").Num(Step).Key("dec").Num((double)Decimals);
            if (Options != null)
            {
                json.Key("options").Begin('[');
                for (int i = 0; i < Options.Length; i++) json.Str(Options[i]);
                json.End();
            }
            json.End();
        }
    }
}
