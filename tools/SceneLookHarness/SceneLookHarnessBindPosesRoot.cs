using System;
using System.Collections.Generic;
using System.Globalization;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessBindPosesRoot
    {
        private const float FrameTolerance = 0.02f;
        private const float ScaleTolerance = 0.01f;
        private const float FrameScaleTolerance = 0.001f;

        internal static SceneLookMatrix Frame(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform)
        {
            return SceneLookFrame.Holder(look, byTransform);
        }

        internal static int Check(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, int> sources, List<SceneLookLookRenderer> restRenderers, SceneLookFile file, float bodyScale)
        {
            SceneLookNode root = SceneLookFrame.Node(look, look.Root);
            if (root == null)
            {
                Console.WriteLine("  check: FAIL the look has no root node");
                return 1;
            }
            SceneLookNode reference = SceneLookFrame.Reference(look);
            if (reference == null)
            {
                Console.WriteLine("  check: FAIL the look has no renderer node with bones");
                return 1;
            }
            SceneLookMatrix frame = Frame(look, byTransform);
            SceneLookMatrix rootWorld = SceneLookHarnessBindPoses.SceneWorld(file, root.Transform);
            SceneLookMatrix referenceWorld = rootWorld.Inverse().Multiply(SceneLookHarnessBindPoses.SceneWorld(file, reference.Transform));
            Console.WriteLine("  look root node {0} | {1}", root.Name, Shape(SceneLookFrame.Local(root)));
            Console.WriteLine("  reference renderer {0} | file node chain with the look root excluded | {1}", reference.Name, Shape(SceneLookFrame.RelativeWithoutRoot(look, reference, byTransform)));
            Console.WriteLine("  holder frame the retarget path applies | {0}", Shape(frame));
            Console.WriteLine("  no part of the look root TRS enters the holder: the holder is a child of the body, whose own transform already carries the look instance placement");
            int failures = Selection(reference, restRenderers, sources);
            failures += FrameTruth(frame, referenceWorld);
            Scene(look, byTransform, rest, file, rootWorld, frame);
            failures += FrameCheck(look, byTransform, rest);
            float up;
            float side;
            float depth;
            failures += PlaceCheck(look, byTransform, rest, file, rootWorld, frame, out up, out side, out depth);
            failures += SceneLookHarnessBindPosesScale.Case(file, look, byTransform, frame, bodyScale, up);
            return failures;
        }

        private static int Selection(SceneLookNode reference, List<SceneLookLookRenderer> restRenderers, Dictionary<long, int> sources)
        {
            int frame = -1;
            for (int i = 0; i < restRenderers.Count; i++)
            {
                if (restRenderers[i].Source.GameObject.PathId == reference.GameObject)
                {
                    frame = i;
                    break;
                }
            }
            if (frame < 0)
            {
                Console.WriteLine("  check: FAIL the rest skeleton does not contain the renderer the holder frame references");
                return 1;
            }
            int bones = 0;
            int seed = -1;
            bool same = true;
            foreach (SceneLookBone bone in restRenderers[frame].Bones)
            {
                int source;
                if (!sources.TryGetValue(bone.Transform.PathId, out source)) continue;
                bones++;
                if (seed < 0) seed = source;
                if (source != frame) same = false;
            }
            Console.WriteLine("  holder frame reference renderer is mesh {0} of {1} in the rest skeleton the runtime builds", frame, restRenderers.Count);
            Console.WriteLine("  the rest skeleton's own sources place {0} of the reference renderer's {1} bones on mesh {2}", bones, restRenderers[frame].Bones.Count, seed);
            if (seed != frame || bones == 0 || !same)
            {
                Console.WriteLine("  check: FAIL the rest skeleton is not seeded from the renderer the holder frame references");
                return 1;
            }
            Console.WriteLine("  check: PASS the rest skeleton is seeded from the renderer the holder frame references, mesh " + frame);
            return 0;
        }

        private static int FrameTruth(SceneLookMatrix frame, SceneLookMatrix truth)
        {
            float difference = frame.MaxDifference(truth);
            float scale = SceneLookFrame.Scale(truth);
            float ratio = scale == 0f ? 0f : SceneLookFrame.Scale(frame) / scale;
            Console.WriteLine("  holder frame against the scene world of the reference renderer read through every ancestor, the look root divided out: max difference {0} | scale ratio {1}", Format(difference), Format(ratio));
            int failures = 0;
            if (difference > FrameTolerance)
            {
                Console.WriteLine("  check: FAIL the holder frame is not the reference renderer's scene chain with the look root divided out");
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the holder frame is the reference renderer's scene chain with the look root divided out");
            }
            if (Math.Abs(ratio - 1f) > FrameScaleTolerance)
            {
                Console.WriteLine("  check: FAIL the holder frame carries " + Format(ratio) + " of the scene chain below the look root, which is the look root counted again");
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the holder frame excludes the look root and carries the scene chain below it once");
            }
            return failures;
        }

        private static void Scene(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, Dictionary<long, SceneLookMatrix> rest, SceneLookFile file, SceneLookMatrix rootWorld, SceneLookMatrix frame)
        {
            SceneLookMatrix inverse = rootWorld.Inverse();
            int bones = 0;
            float worst = 0f;
            string worstName = null;
            foreach (SceneLookNode node in look.Nodes)
            {
                SceneLookMatrix bone;
                if (!rest.TryGetValue(node.Transform, out bone)) continue;
                bones++;
                SceneLookMatrix placed = frame.Multiply(bone);
                float difference = placed.MaxDifference(inverse.Multiply(SceneLookHarnessBindPoses.SceneWorld(file, node.Transform)));
                if (difference > worst)
                {
                    worst = difference;
                    worstName = node.Name;
                }
            }
            Console.WriteLine("  retargeted skeleton against the look's own scene pose read through every ancestor: {0} bones | worst difference {1} at {2}", bones, Format(worst), worstName);
            Console.WriteLine("  that figure is the look's own file bind pose against its scene pose, which this data does not make equal, so it is printed and not checked");
        }

        private static int FrameCheck(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, Dictionary<long, SceneLookMatrix> rest)
        {
            bool have = false;
            SceneLookMatrix first = SceneLookMatrix.Identity();
            float spread = 0f;
            int bones = 0;
            foreach (SceneLookNode node in look.Nodes)
            {
                SceneLookMatrix bone;
                if (!rest.TryGetValue(node.Transform, out bone)) continue;
                SceneLookMatrix unit = bone.Multiply(SceneLookFrame.RelativeWithoutRoot(look, node, byTransform).Inverse());
                if (!have)
                {
                    have = true;
                    first = unit;
                }
                else
                {
                    spread = Math.Max(spread, unit.MaxDifference(first));
                }
                bones++;
            }
            if (bones == 0)
            {
                Console.WriteLine("  check: FAIL no rest bone was read");
                return 1;
            }
            Console.WriteLine("  rest skeleton under the file node chains without the look root, one frame per bone: {0} bones | spread {1}", bones, Format(spread));
            if (spread > FrameTolerance)
            {
                Console.WriteLine("  check: FAIL the file bind poses do not agree on one frame, spread " + Format(spread));
                return 1;
            }
            Console.WriteLine("  check: PASS the file bind poses agree on one frame, spread " + Format(spread) + " within " + Format(FrameTolerance));
            return 0;
        }

        private static int PlaceCheck(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, Dictionary<long, SceneLookMatrix> rest, SceneLookFile file, SceneLookMatrix rootWorld, SceneLookMatrix frame, out float upOut, out float sideOut, out float depthOut)
        {
            int failures = 0;
            upOut = 0f;
            sideOut = 0f;
            depthOut = 0f;
            float[] produced = new float[6];
            for (int i = 0; i < 6; i++) produced[i] = i < 3 ? float.MaxValue : float.MinValue;
            float scaleLow = float.MaxValue;
            float scaleHigh = 0f;
            string scaleName = null;
            int bones = 0;
            SceneLookMatrix inverse = rootWorld.Inverse();
            foreach (SceneLookNode node in look.Nodes)
            {
                SceneLookMatrix bone;
                if (!rest.TryGetValue(node.Transform, out bone)) continue;
                bones++;
                SceneLookMatrix placed = frame.Multiply(bone);
                float scale = SceneLookFrame.Scale(inverse.Multiply(SceneLookHarnessBindPoses.SceneWorld(file, node.Transform)));
                if (scale > 0f)
                {
                    float ratio = SceneLookFrame.Scale(placed) / scale;
                    if (ratio < scaleLow || ratio > scaleHigh) scaleName = node.Name;
                    scaleLow = Math.Min(scaleLow, ratio);
                    scaleHigh = Math.Max(scaleHigh, ratio);
                }
                Extend(produced, placed);
            }
            if (bones == 0)
            {
                Console.WriteLine("  check: FAIL no rest bone was placed");
                return 1;
            }
            float up = produced[4] - produced[1];
            float side = produced[3] - produced[0];
            float depth = produced[5] - produced[2];
            upOut = up;
            sideOut = side;
            depthOut = depth;
            Console.WriteLine("  retargeted world scale against the file world scale, every bone against its own scene world: worst {0} | best {1} | worst bone {2}", Format(scaleLow), Format(scaleHigh), scaleName);
            if (Math.Abs(scaleLow - 1f) > ScaleTolerance || Math.Abs(scaleHigh - 1f) > ScaleTolerance)
            {
                Console.WriteLine("  check: FAIL the retargeted look is not at file scale, its world scale ratio runs " + Format(scaleLow) + " to " + Format(scaleHigh));
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the retargeted look is at file scale, its world scale ratio runs " + Format(scaleLow) + " to " + Format(scaleHigh));
            }
            Console.WriteLine("  retargeted extents | up {0} | side {1} | depth {2}", Format(up), Format(side), Format(depth));
            if (up <= side || up <= depth)
            {
                Console.WriteLine("  check: FAIL the retargeted look is not upright, its up extent {0} does not lead side {1} and depth {2}", Format(up), Format(side), Format(depth));
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the retargeted look stands up, its up extent {0} leads side {1} and depth {2}", Format(up), Format(side), Format(depth));
            }
            return failures;
        }

        private static void Extend(float[] extent, SceneLookMatrix matrix)
        {
            if (matrix.M03 < extent[0]) extent[0] = matrix.M03;
            if (matrix.M13 < extent[1]) extent[1] = matrix.M13;
            if (matrix.M23 < extent[2]) extent[2] = matrix.M23;
            if (matrix.M03 > extent[3]) extent[3] = matrix.M03;
            if (matrix.M13 > extent[4]) extent[4] = matrix.M13;
            if (matrix.M23 > extent[5]) extent[5] = matrix.M23;
        }

        private static string Shape(SceneLookMatrix matrix)
        {
            return "position (" + Format(matrix.M03) + ", " + Format(matrix.M13) + ", " + Format(matrix.M23) + ") | scale " + Format(SceneLookFrame.Scale(matrix));
        }

        private static string Format(float value)
        {
            return value.ToString("0.00000000", CultureInfo.InvariantCulture);
        }
    }
}
