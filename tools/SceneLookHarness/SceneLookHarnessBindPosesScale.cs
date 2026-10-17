using System;
using System.Collections.Generic;
using System.Globalization;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessBindPosesScale
    {
        private const float Tolerance = 0.001f;

        private sealed class Item
        {
            internal SceneLookLook Look;
            internal string Name;
            internal float Root;
            internal float RootWorld;
            internal float Chain;
            internal float Up;
            internal float Body;
            internal float Factor;
            internal bool Negative;
        }

        private static readonly List<Item> Cases = new List<Item>();
        private static readonly List<Item> Mirrors = new List<Item>();
        private static float unitBody = 1f;
        private static string unitBodyLabel = "no target was measured";

        internal static float BodyScale(SceneLookFile file, List<SceneLookRoot> roots, string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return 1f;
            foreach (SceneLookRoot root in roots)
            {
                if (root.Renderers == 0) continue;
                if (!root.Name.StartsWith(targetName, StringComparison.OrdinalIgnoreCase)) continue;
                long transform = SceneLookReader.TransformOf(file, root.GameObject);
                if (transform == 0) continue;
                unitBody = SceneLookFrame.Scale(SceneLookHarnessBindPoses.SceneWorld(file, transform));
                unitBodyLabel = root.Name + " (GameObject pid " + root.GameObject.ToString(CultureInfo.InvariantCulture) + ")";
                return unitBody;
            }
            unitBodyLabel = null;
            Console.WriteLine("  check: FAIL the scale rule asked for body {0} but no root with renderers carries that name", targetName);
            return -1f;
        }

        internal static int Case(SceneLookFile file, SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, SceneLookMatrix frame, float bodyScale, float up)
        {
            SceneLookNode root = SceneLookFrame.Node(look, look.Root);
            if (root == null)
            {
                Console.WriteLine("  check: FAIL the root scale rule has no look root node to read");
                return 1;
            }
            SceneLookMatrix rootTrs = SceneLookFrame.Local(root);
            bool negative = root.Scale[0] < 0f || root.Scale[1] < 0f || root.Scale[2] < 0f;
            float factor;
            string reason;
            bool accepted = SceneLookFrame.HolderRatio(look, bodyScale, out factor, out reason);
            Item item = new Item();
            item.Look = look;
            item.Name = root.Name;
            item.Root = SceneLookFrame.RootScale(look);
            item.RootWorld = SceneLookFrame.Scale(SceneLookHarnessBindPoses.SceneWorld(file, root.Transform));
            item.Chain = SceneLookFrame.Scale(frame);
            item.Up = up;
            item.Body = bodyScale;
            item.Factor = factor;
            item.Negative = negative;
            Cases.Add(item);
            if (negative) Mirrors.Add(item);
            Console.WriteLine("  root scale rule: look root {0} | node scale ({1}, {2}, {3}) | magnitude {4} | file scene world scale {5}",
                root.Name, Format(root.Scale[0]), Format(root.Scale[1]), Format(root.Scale[2]), Format(item.Root), Format(item.RootWorld));
            Console.WriteLine("  target body scale {0} | holder factor {1} | holder frame scale {2} | holder local scale {3} | world scale {4} | world up extent {5}",
                Format(bodyScale), Format(factor), Format(item.Chain), Format(item.Chain * factor), Format(item.Chain * factor * bodyScale), Format(up * factor * bodyScale));
            int failures = 0;
            if (!accepted)
            {
                Console.WriteLine("  check: FAIL the root scale rule refused the look: " + reason);
                failures++;
            }
            else if (!(factor > 0f))
            {
                Console.WriteLine("  check: FAIL the holder factor is not positive: " + Format(factor));
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the holder factor is positive, " + Format(factor));
            }
            if (negative)
            {
                Console.WriteLine("  the look root is mirrored, its node scale has a negative component and the factor takes the magnitude");
            }
            return failures;
        }

        internal static int Summary()
        {
            Console.WriteLine("root scale ratio rule: the holder carries the look root scale divided by the target body scale");
            int failures = 0;
            Console.WriteLine("  target body measured on {0} | scene world scale {1}", unitBodyLabel, Format(unitBody));
            Item unit = First(1f);
            Item shortLook = First(0.9f);
            if (unit == null)
            {
                Console.WriteLine("  check: FAIL no unit root look was measured");
                failures++;
            }
            if (shortLook == null)
            {
                Console.WriteLine("  check: FAIL no 0.9 root look was measured");
                failures++;
            }
            if (unit == null || shortLook == null) return failures;
            float f1 = Factor(unit, 1f);
            failures += Assert("a unit root look on a unit body gives 1, nothing changes for the looks that are already unit", f1, 1f);
            Console.WriteLine("  case 1 holder local scale {0} against the base chain {1}, body 1.00000000, so world up extent {2}", Format(unit.Chain * f1), Format(unit.Chain), Format(unit.Up * f1));
            float f2 = Factor(shortLook, 1f);
            failures += Assert("a " + Format(shortLook.Root) + " root look on a unit body gives its own root scale, so a short character stays short", f2, shortLook.Root);
            Console.WriteLine("  case 2 holder local scale {0} against the base chain {1}, body 1.00000000, so world up extent {2} against the base {3}",
                Format(shortLook.Chain * f2), Format(shortLook.Chain), Format(shortLook.Up * f2), Format(shortLook.Up));
            float f3 = Factor(shortLook, shortLook.Root);
            failures += Assert("a " + Format(shortLook.Root) + " root look on a " + Format(shortLook.Root) + " body gives 1, so the body scale is not counted twice", f3, 1f);
            Console.WriteLine("  case 3 holder local scale {0} (the base chain), body {1}, so world up extent {2}, the same height as case 2", Format(shortLook.Chain * f3), Format(shortLook.Root), Format(shortLook.Up * f3 * shortLook.Root));
            failures += Assert("the world height of a short look is the same whether the body is unit or matches it, so the ratio cancels the double count and does not undo the body scale", shortLook.Up * f3 * shortLook.Root, shortLook.Up * f2);
            failures += RealBody(unit, shortLook, unitBody);
            failures += Refusal(unit);
            failures += Mirrored(unit.Root);
            return failures;
        }

        private static int RealBody(Item unit, Item shortLook, float bodyScale)
        {
            if (Math.Abs(bodyScale - 1f) <= Tolerance)
            {
                Console.WriteLine("  the target body measured in this case is unit, so the unit body case is the measured body case");
                return 0;
            }
            float expectedUnit = unit.Root / bodyScale;
            float expectedShort = shortLook.Root / bodyScale;
            int failures = Assert("the unit root look on the measured body of " + Format(bodyScale) + " carries the body reciprocal", Factor(unit, bodyScale), expectedUnit);
            failures += Assert("the " + Format(shortLook.Root) + " root look on the measured body of " + Format(bodyScale) + " carries its own root scale over the body scale", Factor(shortLook, bodyScale), expectedShort);
            Console.WriteLine("  measured body case holder local scale {0} for the unit look and {1} for the short look, world scale {2} and {3} once the body of {4} is applied", Format(unit.Chain * expectedUnit), Format(shortLook.Chain * expectedShort), Format(unit.Chain * expectedUnit * bodyScale), Format(shortLook.Chain * expectedShort * bodyScale), Format(bodyScale));
            return failures;
        }

        private static int Mirrored(float bodyScale)
        {
            int failures = 0;
            int seen = 0;
            foreach (Item item in Mirrors)
            {
                seen++;
                float factor = Factor(item, bodyScale);
                Console.WriteLine("  mirrored root look {0} | magnitude {1} | factor {2} on a body of {3}", item.Name, Format(item.Root), Format(factor), Format(bodyScale));
                failures += Assert("a mirrored root contributes its magnitude, so the holder scale stays positive and no winding flips", factor, item.Root);
                if (!(factor > 0f))
                {
                    Console.WriteLine("  check: FAIL the mirrored root produced a negative holder factor");
                    failures++;
                }
            }
            if (seen == 0)
            {
                Console.WriteLine("  check: FAIL no mirrored root look was measured, so the absolute value rule is not exercised");
                failures++;
            }
            return failures;
        }

        private static int Refusal(Item item)
        {
            int failures = 0;
            float factor;
            string reason;
            bool accepted = SceneLookFrame.HolderRatio(item.Look, 0f, out factor, out reason);
            if (accepted)
            {
                Console.WriteLine("  check: FAIL a zero target body scale was accepted");
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS a zero target body scale is refused: " + reason);
            }
            accepted = SceneLookFrame.HolderRatio(item.Look, SceneLookFrame.MinScale / 10f, out factor, out reason);
            if (accepted)
            {
                Console.WriteLine("  check: FAIL a target body scale of " + Format(SceneLookFrame.MinScale / 10f) + " was accepted");
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS a target body scale below " + Format(SceneLookFrame.MinScale) + " is refused: " + reason);
            }
            return failures;
        }

        internal static int Mirror(SceneLookFiles files, string scene, string path)
        {
            Console.WriteLine("mirrored root look case: {0} in {1}", path, scene);
            string reason;
            SceneLookFile file = files.Get(scene, out reason);
            if (file == null)
            {
                Console.WriteLine("  check: FAIL " + scene + " is not readable: " + reason);
                return 1;
            }
            SceneLookFileScan scan = SceneLookScan.ScanFile(files, scene);
            if (scan.Error != null)
            {
                Console.WriteLine("  check: FAIL " + scene + " is not readable: " + scan.Error);
                return 1;
            }
            long root = 0;
            foreach (SceneLookRoot candidate in scan.Roots)
            {
                if (candidate.Path == path) { root = candidate.GameObject; break; }
            }
            if (root == 0)
            {
                Console.WriteLine("  check: FAIL the mirrored look root " + path + " was not found in " + scene);
                return 1;
            }
            SceneLookLook look = SceneLookLoad.Load(files, scene, root, out reason);
            if (look == null)
            {
                Console.WriteLine("  check: FAIL the mirrored look was refused: " + reason);
                return 1;
            }
            Dictionary<long, SceneLookNode> map = SceneLookFrame.ByTransform(look);
            SceneLookMatrix frame = SceneLookFrame.Holder(look, map);
            int unresolved;
            Dictionary<long, SceneLookMatrix> rest = SceneLookBind.Rest(SceneLookFrame.RestMeshes(look), out unresolved);
            return Case(file, look, map, frame, unitBody, Up(frame, rest));
        }

        private static float Up(SceneLookMatrix chain, Dictionary<long, SceneLookMatrix> rest)
        {
            float low = float.MaxValue;
            float high = float.MinValue;
            foreach (SceneLookMatrix bone in rest.Values)
            {
                SceneLookMatrix placed = chain.Multiply(bone);
                if (placed.M13 < low) low = placed.M13;
                if (placed.M13 > high) high = placed.M13;
            }
            if (rest.Count == 0) return 0f;
            return high - low;
        }

        private static float Factor(Item item, float bodyScale)
        {
            float factor;
            string reason;
            if (!SceneLookFrame.HolderRatio(item.Look, bodyScale, out factor, out reason)) return 0f;
            return factor;
        }

        private static Item First(float scale)
        {
            foreach (Item item in Cases)
            {
                if (item.Negative) continue;
                if (Math.Abs(item.Root - scale) <= Tolerance) return item;
            }
            return null;
        }

        private static int Assert(string what, float measured, float expected)
        {
            if (!SceneLookFrame.Finite(measured) || !SceneLookFrame.Finite(expected) || Math.Abs(measured - expected) > Tolerance)
            {
                Console.WriteLine("  check: FAIL {0}: measured {1}, expected {2}", what, Format(measured), Format(expected));
                return 1;
            }
            Console.WriteLine("  check: PASS {0}: measured {1}", what, Format(measured));
            return 0;
        }

        private static string Format(float value)
        {
            return value.ToString("0.00000000", CultureInfo.InvariantCulture);
        }
    }
}