using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessIndex
    {
        private delegate SceneLookInfo Finder(SceneLookFiles files, List<string> scenes, out string reason);

        internal static int Run(string[] args)
        {
            string command = args[0];
            if (command == "list" && args.Length >= 2) return List(args[1], Optional(args, 2), true);
            if (command == "listall" && args.Length >= 2) return List(args[1], Optional(args, 2), false);
            if (command == "find" && args.Length >= 3) return Single(Optional(args, 3), delegate (SceneLookFiles files, List<string> scenes, out string reason) { return SceneLookIndex.FindByName(files, scenes, args[1], args[2], out reason); });
            int id;
            if (command == "findchar" && args.Length >= 2 && int.TryParse(args[1], out id)) return Single(Optional(args, 2), delegate (SceneLookFiles files, List<string> scenes, out string reason) { return SceneLookIndex.FindByCharacter(files, scenes, id, out reason); });
            Console.Error.WriteLine("usage: SceneLookHarness list|listall <scene> [dataDir] | find <scene> <name> [dataDir] | findchar <id> [dataDir]");
            return 2;
        }

        private static string Optional(string[] args, int index)
        {
            return args.Length > index ? args[index] : Program.DefaultDataDir;
        }

        private static int List(string scene, string dataDir, bool charactersOnly)
        {
            List<string> scenes = SceneLookHarnessScan.SceneNames(dataDir);
            bool levels = Enumerable.Range(0, scenes.Count).All(i => File.Exists(Path.Combine(dataDir, "level" + i)));
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                string reason;
                List<SceneLookInfo> looks = SceneLookIndex.List(files, scenes, scene, out reason);
                if (looks != null && charactersOnly) looks = SceneLookIndex.Characters(looks);
                if (looks == null)
                {
                    Console.WriteLine("refused: " + reason);
                    return 1;
                }
                Console.WriteLine("scene {0} -> {1} | mode {2} | build scenes {3}, level file for every build index: {4}", scene, SceneLookIndex.LevelOf(scenes, scene, out reason), charactersOnly ? "characters only" : "all roots", scenes.Count, levels ? "yes" : "no");
                foreach (SceneLookInfo look in looks) Console.WriteLine(Line(look));
                Console.WriteLine("looks: {0}", looks.Count);
                return 0;
            }
            finally
            {
                files.Dispose();
            }
        }

        private static int Single(string dataDir, Finder find)
        {
            Stopwatch watch = Stopwatch.StartNew();
            List<string> scenes = SceneLookHarnessScan.SceneNames(dataDir);
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                string reason;
                SceneLookInfo look = find(files, scenes, out reason);
                Console.WriteLine(look == null ? "not found: " + reason : Line(look));
                Console.WriteLine("wall time: {0:F2} s", watch.Elapsed.TotalSeconds);
                return look == null ? 1 : 0;
            }
            finally
            {
                files.Dispose();
            }
        }

        private static string Line(SceneLookInfo look)
        {
            return string.Format("name={0} path={1} renderers={2} bones={3} rig={4} avatar={5} animator={6} npc={7} clean={8} character={9} scene={10} level={11}", look.Name, look.Path, look.Renderers, look.Bones, look.RigKind, look.AvatarKind, look.HasAnimator ? "yes" : "no", look.IsNpc ? "yes" : "no", look.Clean ? "yes" : "no", look.HasCharacter ? look.CharacterId + " " + look.CharacterName : "-", look.Scene, look.Level);
        }
    }
}
