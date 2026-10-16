using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class Program
    {
        internal const string DefaultDataDir = @"C:\Program Files (x86)\Steam\steamapps\common\Obenseuer\Obenseuer_Data";

        private static int Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = new UTF8Encoding(false);
            string command = args.Length > 0 ? args[0] : string.Empty;
            if (command == "list" || command == "listall" || command == "find" || command == "findchar") return SceneLookHarnessIndex.Run(args);
            string dataDir = args.Length > 1 ? args[1] : DefaultDataDir;
            if (command == "proof") return SceneLookHarnessProof.Run(dataDir);
            if (command == "scan") return SceneLookHarnessScan.Run(dataDir);
            if (command == "match") return SceneLookHarnessMatch.Run(dataDir);
            if (command == "applyplan") return SceneLookHarnessApplyPlan.Run(dataDir, Arg(args, 2), Arg(args, 3), Arg(args, 4));
            if (command == "blendshapes") return SceneLookHarnessBlendShapes.Run(dataDir, Arg(args, 2), Arg(args, 3));
            if (command == "bindposes") return SceneLookHarnessBindPoses.Run(dataDir, Arg(args, 2), Arg(args, 3), Arg(args, 4));
            Console.Error.WriteLine("usage: SceneLookHarness proof|scan|match|list|listall|find|findchar [dataDir]");
            Console.Error.WriteLine("       SceneLookHarness applyplan [dataDir] [scene] [lookRoot] [target]");
            Console.Error.WriteLine("       SceneLookHarness blendshapes [dataDir] [meshName|pathId] [file]");
            Console.Error.WriteLine("       SceneLookHarness bindposes [dataDir] [scene] [lookRoot] [target]");
            return 2;
        }

        private static string Arg(string[] args, int index)
        {
            return index < args.Length ? args[index] : null;
        }

        internal static string FormatName(int format)
        {
            return format + " " + SceneLookReaderMesh.FormatLabel(format) + (SceneLookReaderMesh.IsSupported(format) ? string.Empty : " (not supported)");
        }
    }

    internal static class Py
    {
        internal static string Str(string text)
        {
            return "'" + text + "'";
        }

        internal static string Items(IEnumerable<string> reprs)
        {
            return "[" + string.Join(", ", reprs) + "]";
        }

        internal static string Dict(SceneLookCounter<string> counter)
        {
            List<string> parts = new List<string>();
            foreach (string key in counter.Keys) parts.Add(Str(key) + ": " + counter[key]);
            return "{" + string.Join(", ", parts) + "}";
        }

        internal static string DictRaw(SceneLookCounter<string> counter)
        {
            List<string> parts = new List<string>();
            foreach (string key in counter.Keys) parts.Add(key + ": " + counter[key]);
            return "{" + string.Join(", ", parts) + "}";
        }
    }
}
