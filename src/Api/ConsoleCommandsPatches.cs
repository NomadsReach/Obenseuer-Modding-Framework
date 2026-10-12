using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class ConsoleCommandsPatches
    {
        internal static bool Install()
        {
            try
            {
                MethodInfo prefix = CorePatcher.Handler(typeof(ConsoleCommandsPatches), "HandleInputPrefix");
                MethodInfo postfix = CorePatcher.Handler(typeof(ConsoleCommandsPatches), "PrintValidCommandsPostfix");
                return CorePatcher.PatchCore(typeof(DeveloperConsole), "HandleInput", new[] { typeof(string) }, prefix, null, null, true, HudReport.Owner)
                    && CorePatcher.PatchCore(typeof(DeveloperConsole), "PrintValidCommands", new Type[0], null, postfix, null, true, HudReport.Owner);
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "ConsoleCommands install", e);
                return false;
            }
        }

        private static bool HandleInputPrefix(string input)
        {
            try
            {
                string[] tokens = Tokenize(input);
                if (tokens.Length == 0) return true;
                ConsoleCommandEntry command = ConsoleCommands.Find(tokens[0]);
                if (command == null) return true;
                string[] args = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, args, 0, args.Length);
                Run(command, args);
                return false;
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "HandleInput prefix", e);
                return true;
            }
        }

        private static void PrintValidCommandsPostfix()
        {
            try
            {
                ConsoleCommands.PrintModCommands();
            }
            catch (Exception e)
            {
                ApiLog.Once(HudReport.Owner, "PrintValidCommands postfix", e);
            }
        }

        private static void Run(ConsoleCommandEntry command, string[] args)
        {
            try
            {
                command.Run(args);
            }
            catch (Exception e)
            {
                ConsoleCommands.Print(command.Name + " failed: " + e.Message);
                ApiLog.Once(HudReport.Owner, "command " + command.ModId + " " + command.Name, e);
            }
        }

        private static string[] Tokenize(string input)
        {
            string[] parts = input.Split(new[] { '"' });
            List<string> tokens = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 != 0) tokens.Add(parts[i]);
                else tokens.AddRange(parts[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            return tokens.ToArray();
        }
    }
}
