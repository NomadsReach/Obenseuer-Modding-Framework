using System;
using System.Collections.Generic;
using System.Globalization;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessPlacement
    {
        private const float Tolerance = 0.0001f;

        private sealed class Placement
        {
            internal int Bones;
            internal int Fallbacks;
            internal float MaxMatrix;
            internal float MaxPosition;
        }

        internal static int Run(SceneLookLook look, Dictionary<string, string> map, Dictionary<string, SceneLookMatrix> rest)
        {
            return Run(look, map, rest, "name path");
        }

        internal static int Run(SceneLookLook look, Dictionary<string, string> map, Dictionary<string, SceneLookMatrix> rest, string label)
        {
            SceneLookMatrix holderLocal = SceneLookMatrix.Identity();
            SceneLookMatrix bodyOrigin = SceneLookMatrix.Identity();
            SceneLookMatrix bodyAway = SceneLookMatrix.Trs(new float[] { 37.5f, -2.25f, 118.75f }, new float[] { 0f, 0.3007058f, 0f, 0.9537169f }, new float[] { 1f, 1f, 1f });
            Placement origin = Measure(look, map, rest, bodyOrigin, holderLocal);
            Placement away = Measure(look, map, rest, bodyAway, holderLocal);
            Console.WriteLine(label + " at a body away from the origin");
            Console.WriteLine("  bone slots: {0} | file bind pose fallbacks: {1}", origin.Bones, origin.Fallbacks);
            Console.WriteLine("  body at the origin: max placement difference {0} | max position difference {1}", Format(origin.MaxMatrix), Format(origin.MaxPosition));
            Console.WriteLine("  body at (37.50, -2.25, 118.75) turned 35 degrees: max placement difference {0} | max position difference {1}", Format(away.MaxMatrix), Format(away.MaxPosition));
            int failures = 0;
            failures += Check(origin, "at the origin");
            failures += Check(away, "away from the origin");
            return failures;
        }

        private static Placement Measure(SceneLookLook look, Dictionary<string, string> map, Dictionary<string, SceneLookMatrix> rest, SceneLookMatrix body, SceneLookMatrix holderLocal)
        {
            Placement placement = new Placement();
            SceneLookMatrix frame = SceneLookBind.BindFrame(body, body.Multiply(holderLocal));
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                foreach (SceneLookBone bone in renderer.Bones)
                {
                    string name;
                    SceneLookMatrix boneRest;
                    if (!map.TryGetValue(bone.Name, out name) || !rest.TryGetValue(name, out boneRest))
                    {
                        placement.Fallbacks++;
                        continue;
                    }
                    SceneLookMatrix skin = body.Multiply(boneRest).Multiply(SceneLookBind.BindPose(boneRest, frame));
                    placement.Bones++;
                    placement.MaxMatrix = Math.Max(placement.MaxMatrix, body.MaxDifference(skin));
                    placement.MaxPosition = Math.Max(placement.MaxPosition, SceneLookMatrix.Distance(body, skin));
                }
            }
            return placement;
        }

        private static int Check(Placement placement, string where)
        {
            if (placement.Bones == 0)
            {
                Console.WriteLine("  check: FAIL no bind pose was compared " + where);
                return 1;
            }
            if (placement.MaxMatrix > Tolerance || placement.MaxPosition > Tolerance)
            {
                Console.WriteLine("  check: FAIL the look does not land on the body " + where + " within " + Format(Tolerance) + ", worst placement difference " + Format(placement.MaxMatrix) + " and position difference " + Format(placement.MaxPosition));
                return 1;
            }
            Console.WriteLine("  check: PASS the look lands on the body " + where + " within " + Format(Tolerance));
            return 0;
        }

        private static string Format(float value)
        {
            return value.ToString("0.00000000", CultureInfo.InvariantCulture);
        }
    }
}
