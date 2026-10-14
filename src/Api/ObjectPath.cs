using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class ObjectPath
    {
        internal static string[] Split(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string[] names = path.Split('/');
            foreach (string name in names)
            {
                if (name.Length == 0) return null;
            }
            return names;
        }

        internal static T Descend<T>(IEnumerable<T> nodes, string[] names, int depth, Func<T, string> nameOf, Func<T, bool> visible, Func<T, IEnumerable<T>> childrenOf) where T : class
        {
            foreach (T node in nodes)
            {
                if (nameOf(node) != names[depth] || !visible(node)) continue;
                if (depth == names.Length - 1) return node;
                T found = Descend(childrenOf(node), names, depth + 1, nameOf, visible, childrenOf);
                if (found != null) return found;
            }
            return null;
        }

        internal static bool SelfCheck()
        {
            string[] names = Split("Bazaar/Trader/Muscle Max");
            string[] root = Split("Root");
            return names != null && names.Length == 3 && names[0] == "Bazaar" && names[2] == "Muscle Max"
                && root != null && root.Length == 1
                && Split("") == null && Split(null) == null && Split("/Root") == null && Split("Root/") == null && Split("A//B") == null
                && Duplicates();
        }

        private static bool Duplicates()
        {
            Node hidden = new Node("Muscle Max", false);
            Node shown = new Node("Muscle Max", true);
            Node inRoot = new Node("Muscle Max", true);
            Node inChild = new Node("Muscle Max", true);
            List<Node> roots = new List<Node> { new Node("Trader", true), new Node("Trader", true, inRoot) };
            List<Node> nested = new List<Node> { new Node("Shop", true, new Node("Trader", true), new Node("Trader", true, inChild)) };
            List<Node> inactive = new List<Node> { new Node("Trader", true, hidden), new Node("Trader", true, shown) };
            return Find(roots, "Trader/Muscle Max", true) == inRoot
                && Find(nested, "Shop/Trader/Muscle Max", true) == inChild
                && Find(inactive, "Trader/Muscle Max", false) == shown
                && Find(inactive, "Trader/Muscle Max", true) == hidden
                && Find(roots, "Trader/Missing", true) == null;
        }

        private static Node Find(List<Node> roots, string path, bool includeInactive)
        {
            return Descend(roots, Split(path), 0, n => n.Name, n => includeInactive || n.Active, n => n.Children);
        }

        private sealed class Node
        {
            internal readonly string Name;
            internal readonly bool Active;
            internal readonly List<Node> Children;

            internal Node(string name, bool active, params Node[] children)
            {
                Name = name;
                Active = active;
                Children = new List<Node>(children);
            }
        }
    }
}
