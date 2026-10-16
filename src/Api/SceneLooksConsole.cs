using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    internal static class SceneLooksConsole
    {
        internal const string Name = "scenelooks";
        private const string Help = "list <scene> | listall <scene> | find <scene> <name> | findchar <id> | here; prints scene look entries";
        private const string Usage = "usage: scenelooks list|listall \"<scene>\" | find \"<scene>\" <name> | findchar <id> | here";
        private const string CheckWhat = "menu scene looks checks";

        internal static bool Register()
        {
            return ConsoleCommands.Register(ModIds.CoreId, Name, Help, Run);
        }

        internal static void Run(string[] args)
        {
            string text;
            try
            {
                text = Text(args);
            }
            catch (Exception e)
            {
                ApiLog.Once(SceneLooks.Owner, "console " + Name, e);
                text = Name + " failed: " + e.Message;
            }
            ConsoleCommands.Print(text);
            CoreLog.For(SceneLooks.Owner).Info(text);
        }

        internal static string Text(string[] args)
        {
            if (args == null || args.Length == 0) return Usage;
            string mode = args[0];
            if (mode == "list" && args.Length >= 2) return ListText(args[1], true);
            if (mode == "listall" && args.Length >= 2) return ListText(args[1], false);
            if (mode == "find" && args.Length >= 3) return One(SceneLooks.FindByName(args[1], args[2]), "no look named " + args[2] + " in " + args[1]);
            if (mode == "findchar" && args.Length >= 2)
            {
                int id;
                if (!int.TryParse(args[1], out id)) return "id must be a number";
                return One(SceneLooks.FindByCharacter(id), "no look linked to character " + args[1]);
            }
            if (mode == "here") return ListText(SceneManager.GetActiveScene().name, true);
            return Usage;
        }

        internal static string RunChecks()
        {
            string scene = SceneManager.GetActiveScene().name;
            string reason = Refusal(scene);
            if (reason != null)
            {
                ApiLog.Once(SceneLooks.Owner, CheckWhat, reason);
                return reason;
            }
            SceneLookInfo[] characters = SceneLooks.List(scene, true);
            SceneLookInfo[] roots = SceneLooks.List(scene, false);
            CoreLog.For(SceneLooks.Owner).Info(Checks(scene, characters, roots));
            return scene + ": " + characters.Length + " characters, " + roots.Length + " roots";
        }

        private static string Refusal(string scene)
        {
            string gate = SceneLooks.Gate();
            if (gate != null) return gate;
            if (string.IsNullOrEmpty(scene)) return "there is no active scene";
            if (!SceneLooks.KnownScene(scene)) return "scene not in build settings: " + scene;
            return null;
        }

        private static string Checks(string scene, SceneLookInfo[] characters, SceneLookInfo[] roots)
        {
            List<string> parts = new List<string>();
            parts.Add(ListText(scene, true, characters));
            parts.Add(ListText(scene, false, roots));
            parts.Add(NamesText(roots));
            return string.Join("\n\n", parts.ToArray());
        }

        private static string NamesText(SceneLookInfo[] looks)
        {
            List<string> npcs = new List<string>();
            List<string> characters = new List<string>();
            for (int i = 0; i < looks.Length; i++)
            {
                if (looks[i].IsNpc) npcs.Add(looks[i].Name);
                if (looks[i].HasCharacter) characters.Add(looks[i].CharacterId + " " + looks[i].CharacterName);
            }
            return "npcs: " + Listed(npcs) + "\ncharacters: " + Listed(characters);
        }

        private static string Listed(List<string> names)
        {
            return names.Count == 0 ? "none" : string.Join(", ", names.ToArray());
        }

        private static string ListText(string scene, bool charactersOnly)
        {
            return ListText(scene, charactersOnly, SceneLooks.List(scene, charactersOnly));
        }

        private static string ListText(string scene, bool charactersOnly, SceneLookInfo[] looks)
        {
            List<string> lines = new List<string>();
            lines.Add("scene=" + scene + " mode=" + (charactersOnly ? "characters only" : "all roots"));
            for (int i = 0; i < looks.Length; i++) lines.Add(Line(looks[i]));
            lines.Add("looks: " + looks.Length);
            if (looks.Length == 0) lines.Add("no entries, see the framework log for the reason");
            return string.Join("\n", lines.ToArray());
        }

        private static string One(SceneLookInfo look, string missing)
        {
            return look == null ? missing : Line(look);
        }

        private static string Line(SceneLookInfo look)
        {
            return string.Format("name={0} path={1} renderers={2} bones={3} rig={4} avatar={5} animator={6} npc={7} clean={8} character={9} scene={10} level={11}", look.Name, look.Path, look.Renderers, look.Bones, look.RigKind, look.AvatarKind, look.HasAnimator ? "yes" : "no", look.IsNpc ? "yes" : "no", look.Clean ? "yes" : "no", look.HasCharacter ? look.CharacterId + " " + look.CharacterName : "-", look.Scene, look.Level);
        }
    }
}
