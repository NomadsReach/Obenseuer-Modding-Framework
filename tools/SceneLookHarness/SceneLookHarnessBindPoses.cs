using System;
using System.Collections.Generic;
using System.Globalization;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static class SceneLookHarnessBindPoses
    {
        private const string LookScene = "level15";
        private const string LookPath = "Content/mr_duck/Wasteland Hide/mr_duck";
        private const string DefaultTarget = "Ville";
        private const float Tolerance = 0.0001f;
        private const float MeshMargin = 0.02f;

        private sealed class MeshPoses
        {
            internal string Label;
            internal readonly List<string> Names = new List<string>();
            internal readonly List<long> Transforms = new List<long>();
            internal readonly List<SceneLookMatrix> FilePoses = new List<SceneLookMatrix>();
        }

        private sealed class Measure
        {
            internal int Bones;
            internal int Pairs;
            internal int WithoutParent;
            internal float MaxLength;
            internal float MaxMatrix;
            internal float MaxSpread;
        }

        internal static int Run(string dataDir, string sceneArg, string lookArg, string targetArg)
        {
            SceneLookFiles files = new SceneLookFiles(dataDir);
            try
            {
                string scene = string.IsNullOrEmpty(sceneArg) ? LookScene : sceneArg;
                string path = string.IsNullOrEmpty(lookArg) ? LookPath : lookArg;
                string targetName = string.IsNullOrEmpty(targetArg) ? DefaultTarget : targetArg;
                string reason;
                SceneLookFileScan scan = SceneLookScan.ScanFile(files, scene);
                if (scan.Error != null)
                {
                    Console.WriteLine("scene " + scene + " is not readable: " + scan.Error);
                    return 1;
                }
                SceneLookFile file = files.Get(scene, out reason);
                if (file == null)
                {
                    Console.WriteLine("scene " + scene + " is not readable: " + reason);
                    return 1;
                }
                long lookRoot = FindRoot(file, scan.Roots, path, out reason);
                if (lookRoot == 0)
                {
                    Console.WriteLine("look root " + path + " was not found in " + scene + ": " + reason);
                    return 1;
                }
                SceneLookLook look = SceneLookLoad.Load(files, scene, lookRoot, out reason);
                if (look == null)
                {
                    Console.WriteLine("look " + path + " was refused: " + reason);
                    return 1;
                }
                Console.WriteLine("bindposes: look {0} in {1} (GameObject pid {2})", path, scene, lookRoot);
                Console.WriteLine("look: {0} renderers | {1} nodes | {2} file bind poses over {3} look bones", look.Renderers.Count, look.Nodes.Count, PoseCount(look), BoneCount(look));
                Dictionary<long, SceneLookNode> byTransform = ByTransform(look);
                List<MeshPoses> meshes = LookMeshes(look);
                List<SceneLookBindMesh<long>> bind = LookBindMeshes(meshes);
                Dictionary<long, int> sources = new Dictionary<long, int>();
                int unresolved;
                Dictionary<long, SceneLookMatrix> rest = SceneLookBind.Rest(bind, sources, out unresolved);
                Console.WriteLine("file bind poses: row 3 of {0} is {1}", meshes[0].Label, Row3(meshes[0].FilePoses[0]));
                Console.WriteLine("look rest: {0} of {1} bones resolved from the file bind poses | {2} meshes | max mesh frame difference {3}", rest.Count, BoneCount(look), meshes.Count, Format(FrameMargin(meshes, bind, rest)));
                Console.WriteLine("look renderer nodes at their own bind frame: {0} of {1} | worst position difference {2}", RendererFrame(look, byTransform, meshes, bind, rest, sources), meshes.Count, Format(WorstRendererFrame(look, byTransform, meshes, bind, rest, sources)));
                Console.WriteLine();
                int failures = Retarget(look, byTransform, meshes, bind, rest, sources);
                Console.WriteLine();
                failures += Name(files, file, scan, look, byTransform, meshes, targetName);
                Console.WriteLine();
                Console.WriteLine(failures == 0 ? "bindposes: PASS" : "bindposes: FAIL " + failures + " check(s)");
                return failures == 0 ? 0 : 1;
            }
            finally
            {
                files.Dispose();
            }
        }

        private static int Retarget(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<MeshPoses> meshes, List<SceneLookBindMesh<long>> bind, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, int> sources)
        {
            Console.WriteLine("retarget path (the look skeleton rebuilt from the file bind poses)");
            float composition = 0f;
            float drift = 0f;
            Dictionary<long, SceneLookMatrix> worlds = new Dictionary<long, SceneLookMatrix>();
            foreach (SceneLookNode node in look.Nodes)
            {
                SceneLookMatrix world = Place(look, node, byTransform, rest, worlds, 0);
                SceneLookMatrix bone;
                if (rest.TryGetValue(node.Transform, out bone)) composition = Math.Max(composition, world.MaxDifference(bone));
                drift = Math.Max(drift, SceneLookMatrix.Distance(world, Relative(look, node, byTransform)));
            }
            int fileBones = 0;
            int sceneNodes = 0;
            foreach (SceneLookNode node in look.Nodes)
            {
                if (rest.ContainsKey(node.Transform)) fileBones++;
                else sceneNodes++;
            }
            List<SceneLookMatrix[]> poses = new List<SceneLookMatrix[]>(look.Renderers.Count);
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookLookRenderer renderer = look.Renderers[r];
                SceneLookMatrix rendererWorld = OwnFrame(bind[r], rest, sources, r);
                SceneLookMatrix[] rendererPoses = new SceneLookMatrix[renderer.Bones.Count];
                for (int i = 0; i < renderer.Bones.Count; i++) rendererPoses[i] = SceneLookBind.BindPose(rest[renderer.Bones[i].Transform.PathId], rendererWorld);
                poses.Add(rendererPoses);
            }
            Console.WriteLine("  rest from the file bind poses: {0} bones | rest from the scene pose: {1} nodes | local composition difference {2}", fileBones, sceneNodes, Format(composition));
            Console.WriteLine("  derived rest vs the scene pose: max position difference {0}", Format(drift));
            Measure own = Compare(look, byTransform, meshes, poses, sources, -2);
            Console.WriteLine("  computed bind pose vs file bind pose, bones anchored in their own mesh: {0} bone slots | {1} bone pairs | max bone length difference {2} | max matrix difference {3}", own.Bones, own.Pairs, Format(own.MaxLength), Format(own.MaxMatrix));
            int failures = Check(own, Tolerance);
            Measure all = Compare(look, byTransform, meshes, poses, sources, -1);
            Console.WriteLine("  computed bind pose vs file bind pose, every mesh: {0} bone pairs | max bone length difference {1} | max matrix difference {2}", all.Pairs, Format(all.MaxLength), Format(all.MaxMatrix));
            float margin = FrameMargin(meshes, bind, rest);
            Console.WriteLine("  every mesh frame against the derived rest: max difference {0}", Format(margin));
            if (margin > MeshMargin)
            {
                Console.WriteLine("  check: FAIL the look's own meshes disagree by more than " + Format(MeshMargin));
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the look's own meshes agree on one rest skeleton within " + Format(MeshMargin));
            }
            Measure scene = SceneRule(look, byTransform, meshes);
            Console.WriteLine("  scene pose rule (the defect): {0} bone pairs | max bone length difference {1} | max matrix difference {2}", scene.Pairs, Format(scene.MaxLength), Format(scene.MaxMatrix));
            if (scene.MaxMatrix <= all.MaxMatrix)
            {
                Console.WriteLine("  check: FAIL the scene pose rule is not worse than the rest pose rule");
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS the rest pose rule is closer to the file than the scene pose rule ({0} against {1})", Format(all.MaxMatrix), Format(scene.MaxMatrix));
            }
            return failures;
        }

        private static int Name(SceneLookFiles files, SceneLookFile file, SceneLookFileScan scan, SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<MeshPoses> meshes, string targetName)
        {
            SceneLookRoot target = FindTarget(scan.Roots, targetName);
            if (target == null)
            {
                Console.WriteLine("name path: no target root matching " + targetName + " was found");
                return 1;
            }
            Console.WriteLine("name path: target {0} (GameObject pid {1}) | rig {2} | avatar {3} | {4} names under the root", target.Path, target.GameObject, target.Rig, target.Avatar, target.Hierarchy.Count);
            SceneLookRigPlan plan = SceneLookRig.Decide(look, target.Hierarchy, target.Name, target.Avatar);
            Console.WriteLine("  decision: {0}{1}", Label(plan.Path), plan.Reason == null ? string.Empty : " (" + plan.Reason + ")");
            if (plan.Path != SceneLookRigPath.Name)
            {
                Console.WriteLine("  check: FAIL the Mr. Duck look does not take the name path on this target");
                return 1;
            }
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (SceneLookRigBone bone in plan.Bones) map[bone.LookName] = bone.TargetName;
            long rootTransform = SceneLookReader.TransformOf(file, target.GameObject);
            List<MeshPoses> targetMeshes = TargetMeshes(files, file, target);
            List<SceneLookBindMesh<string>> targetBind = TargetBindMeshes(targetMeshes);
            Dictionary<string, int> targetSources = new Dictionary<string, int>();
            int unresolved;
            Dictionary<string, SceneLookMatrix> rest = SceneLookBind.Rest(targetBind, targetSources, out unresolved);
            Console.WriteLine("  target rest from the target's own mesh bind poses: {0} bones over {1} meshes | {2} unresolved", rest.Count, targetBind.Count, unresolved);
            int mapped = 0;
            int fallback = 0;
            float drift = 0f;
            List<SceneLookMatrix[]> poses = new List<SceneLookMatrix[]>(look.Renderers.Count);
            List<SceneLookMatrix[]> frames = new List<SceneLookMatrix[]>(look.Renderers.Count);
            List<bool[]> falls = new List<bool[]>(look.Renderers.Count);
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookLookRenderer renderer = look.Renderers[r];
                SceneLookNode node = Node(look, renderer.Source.GameObject.PathId);
                SceneLookMatrix placement = node == null ? SceneLookMatrix.Identity() : Relative(look, node, byTransform);
                SceneLookMatrix[] rendererPoses = new SceneLookMatrix[renderer.Bones.Count];
                SceneLookMatrix[] rendererFrames = new SceneLookMatrix[renderer.Bones.Count];
                bool[] rendererFalls = new bool[renderer.Bones.Count];
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    SceneLookBone bone = renderer.Bones[i];
                    string name;
                    SceneLookMatrix boneRest;
                    if (map.TryGetValue(bone.Name, out name) && rest.TryGetValue(name, out boneRest))
                    {
                        mapped++;
                        drift = Math.Max(drift, SceneLookMatrix.Distance(boneRest, TargetScene(targetMeshes, file, rootTransform, name)));
                        rendererPoses[i] = SceneLookBind.BindPose(boneRest, SceneLookMatrix.Identity());
                        rendererFrames[i] = placement;
                    }
                    else
                    {
                        fallback++;
                        rendererFalls[i] = true;
                        rendererPoses[i] = meshes[r].FilePoses[i];
                        rendererFrames[i] = placement;
                    }
                }
                poses.Add(rendererPoses);
                frames.Add(rendererFrames);
                falls.Add(rendererFalls);
            }
            Console.WriteLine("  mapped bone slots: {0} | file bind pose fallbacks: {1}", mapped, fallback);
            Console.WriteLine("  target rest vs the target's scene pose: max position difference {0}", Format(drift));
            Measure own = CompareTarget(look, byTransform, poses, targetBind, targetSources);
            Console.WriteLine("  computed bind pose vs the target file bind pose, bones anchored in their own mesh: {0} bone slots | {1} bone pairs | max bone length difference {2} | max matrix difference {3}", own.Bones, own.Pairs, Format(own.MaxLength), Format(own.MaxMatrix));
            int failures = Check(own, Tolerance);
            Measure frame = Frames(look, frames, falls);
            Console.WriteLine("  computed bind pose holds the look placement: {0} bones | max spread {1}", frame.Bones, Format(frame.MaxSpread));
            if (frame.Bones == 0)
            {
                Console.WriteLine("  check: FAIL no bind pose frame was compared");
                failures++;
            }
            else if (frame.MaxSpread > Tolerance)
            {
                Console.WriteLine("  check: FAIL the computed bind poses do not hold the look placement, worst spread " + Format(frame.MaxSpread));
                failures++;
            }
            else
            {
                Console.WriteLine("  check: PASS every computed bind pose holds the look placement within " + Format(Tolerance));
            }
            Measure all = Compare(look, byTransform, meshes, poses, sources: null, ownIndex: -1);
            Console.WriteLine("  computed bind pose vs the look file bind pose (the look keeps its own shape, the two rigs differ by design): {0} bone pairs | max bone length difference {1}", all.Pairs, Format(all.MaxLength));
            return failures;
        }

        private static int Check(Measure measure, float tolerance)
        {
            if (measure.Pairs == 0)
            {
                Console.WriteLine("  check: FAIL no bone pair was compared");
                return 1;
            }
            if (measure.MaxLength > tolerance || measure.MaxMatrix > tolerance)
            {
                Console.WriteLine("  check: FAIL the computed bone lengths match the file within " + Format(tolerance) + ", worst bone length " + Format(measure.MaxLength) + " and matrix " + Format(measure.MaxMatrix));
                return 1;
            }
            Console.WriteLine("  check: PASS the computed bone lengths match the file within " + Format(tolerance));
            return 0;
        }

        private static SceneLookMatrix OwnFrame(SceneLookBindMesh<long> mesh, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, int> sources, int index)
        {
            for (int i = 0; i < mesh.Bones.Count; i++)
            {
                int source;
                SceneLookMatrix bone;
                if (!sources.TryGetValue(mesh.Bones[i], out source) || source != index) continue;
                if (!rest.TryGetValue(mesh.Bones[i], out bone)) continue;
                return bone.Multiply(mesh.Poses[i]);
            }
            bool known;
            return SceneLookBind.Frame(mesh, rest, out known);
        }

        private static float FrameMargin(List<MeshPoses> meshes, List<SceneLookBindMesh<long>> bind, Dictionary<long, SceneLookMatrix> rest)
        {
            float worst = 0f;
            for (int m = 0; m < meshes.Count; m++)
            {
                bool known;
                SceneLookMatrix frame = SceneLookBind.Frame(bind[m], rest, out known);
                if (!known) continue;
                for (int i = 0; i < meshes[m].Transforms.Count; i++)
                {
                    SceneLookMatrix bone;
                    if (!rest.TryGetValue(meshes[m].Transforms[i], out bone)) continue;
                    worst = Math.Max(worst, frame.MaxDifference(bone.Multiply(meshes[m].FilePoses[i])));
                }
            }
            return worst;
        }

        private static int RendererFrame(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<MeshPoses> meshes, List<SceneLookBindMesh<long>> bind, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, int> sources)
        {
            int matching = 0;
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                if (RendererOffset(look, byTransform, r, bind, rest, sources) <= MeshMargin) matching++;
            }
            return matching;
        }

        private static float WorstRendererFrame(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<MeshPoses> meshes, List<SceneLookBindMesh<long>> bind, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, int> sources)
        {
            float worst = 0f;
            for (int r = 0; r < look.Renderers.Count; r++) worst = Math.Max(worst, RendererOffset(look, byTransform, r, bind, rest, sources));
            return worst;
        }

        private static float RendererOffset(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, int renderer, List<SceneLookBindMesh<long>> bind, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, int> sources)
        {
            SceneLookNode node = Node(look, look.Renderers[renderer].Source.GameObject.PathId);
            if (node == null) return 0f;
            SceneLookMatrix scene = Relative(look, node, byTransform);
            SceneLookMatrix frame = OwnFrame(bind[renderer], rest, sources, renderer);
            return SceneLookMatrix.Distance(frame, scene);
        }

        private static Measure Compare(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<MeshPoses> meshes, List<SceneLookMatrix[]> poses, Dictionary<long, int> sources, int ownIndex)
        {
            Measure measure = new Measure();
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookLookRenderer renderer = look.Renderers[r];
                MeshPoses mesh = meshes[r];
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    long transform = renderer.Bones[i].Transform.PathId;
                    bool own = Own(sources, transform, ownIndex, r);
                    if (!own) continue;
                    measure.Bones++;
                    measure.MaxMatrix = Math.Max(measure.MaxMatrix, mesh.FilePoses[i].MaxDifference(poses[r][i]));
                    SceneLookNode node = Bone(byTransform, renderer.Bones[i]);
                    SceneLookNode parent = node == null ? null : Parent(node, byTransform);
                    int parentIndex = parent == null ? -1 : Index(renderer, parent.Transform);
                    if (parentIndex < 0)
                    {
                        measure.WithoutParent++;
                        continue;
                    }
                    if (!Own(sources, renderer.Bones[parentIndex].Transform.PathId, ownIndex, r)) continue;
                    measure.Pairs++;
                    float difference = Math.Abs(SceneLookMatrix.Distance(poses[r][i], poses[r][parentIndex]) - SceneLookMatrix.Distance(mesh.FilePoses[i], mesh.FilePoses[parentIndex]));
                    measure.MaxLength = Math.Max(measure.MaxLength, difference);
                }
            }
            return measure;
        }

        private static Measure CompareTarget(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<SceneLookMatrix[]> poses, List<SceneLookBindMesh<string>> targetBind, Dictionary<string, int> sources)
        {
            Measure measure = new Measure();
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookLookRenderer renderer = look.Renderers[r];
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    if (!Own(sources, renderer.Bones[i].Name, 0, 0)) continue;
                    measure.Bones++;
                    measure.MaxMatrix = Math.Max(measure.MaxMatrix, TargetPose(targetBind, renderer.Bones[i].Name).MaxDifference(poses[r][i]));
                    SceneLookNode node = Bone(byTransform, renderer.Bones[i]);
                    SceneLookNode parent = node == null ? null : Parent(node, byTransform);
                    int parentIndex = parent == null ? -1 : Index(renderer, parent.Transform);
                    if (parentIndex < 0) continue;
                    if (!Own(sources, renderer.Bones[parentIndex].Name, 0, 0)) continue;
                    measure.Pairs++;
                    float difference = Math.Abs(SceneLookMatrix.Distance(poses[r][i], poses[r][parentIndex]) - SceneLookMatrix.Distance(TargetPose(targetBind, renderer.Bones[i].Name), TargetPose(targetBind, renderer.Bones[parentIndex].Name)));
                    measure.MaxLength = Math.Max(measure.MaxLength, difference);
                }
            }
            return measure;
        }

        private static Measure Frames(SceneLookLook look, List<SceneLookMatrix[]> frames, List<bool[]> falls)
        {
            Measure measure = new Measure();
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookMatrix frame = SceneLookMatrix.Identity();
                bool have = false;
                for (int i = 0; i < frames[r].Length; i++)
                {
                    if (falls[r][i]) continue;
                    if (!have)
                    {
                        frame = frames[r][i];
                        have = true;
                    }
                    measure.Bones++;
                    measure.MaxSpread = Math.Max(measure.MaxSpread, frame.MaxDifference(frames[r][i]));
                }
            }
            return measure;
        }

        private static Measure SceneRule(SceneLookLook look, Dictionary<long, SceneLookNode> byTransform, List<MeshPoses> meshes)
        {
            List<SceneLookMatrix[]> poses = new List<SceneLookMatrix[]>(look.Renderers.Count);
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookLookRenderer renderer = look.Renderers[r];
                SceneLookNode node = Node(look, renderer.Source.GameObject.PathId);
                SceneLookMatrix rendererWorld = node == null ? SceneLookMatrix.Identity() : Relative(look, node, byTransform);
                SceneLookMatrix[] rendererPoses = new SceneLookMatrix[renderer.Bones.Count];
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    SceneLookNode boneNode = Bone(byTransform, renderer.Bones[i]);
                    SceneLookMatrix world = boneNode == null ? SceneLookMatrix.Identity() : Relative(look, boneNode, byTransform);
                    rendererPoses[i] = SceneLookBind.BindPose(world, rendererWorld);
                }
                poses.Add(rendererPoses);
            }
            Measure measure = new Measure();
            for (int r = 0; r < look.Renderers.Count; r++)
            {
                SceneLookLookRenderer renderer = look.Renderers[r];
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    measure.Bones++;
                    measure.MaxMatrix = Math.Max(measure.MaxMatrix, meshes[r].FilePoses[i].MaxDifference(poses[r][i]));
                    SceneLookNode node = Bone(byTransform, renderer.Bones[i]);
                    SceneLookNode parent = node == null ? null : Parent(node, byTransform);
                    int parentIndex = parent == null ? -1 : Index(renderer, parent.Transform);
                    if (parentIndex < 0) continue;
                    measure.Pairs++;
                    float difference = Math.Abs(SceneLookMatrix.Distance(poses[r][i], poses[r][parentIndex]) - SceneLookMatrix.Distance(meshes[r].FilePoses[i], meshes[r].FilePoses[parentIndex]));
                    measure.MaxLength = Math.Max(measure.MaxLength, difference);
                }
            }
            return measure;
        }

        private static bool Own(Dictionary<long, int> sources, long transform, int ownIndex, int renderer)
        {
            if (ownIndex == -1) return true;
            if (sources == null) return true;
            int source;
            if (!sources.TryGetValue(transform, out source)) return false;
            if (ownIndex == -2) return source == renderer;
            return source == ownIndex;
        }

        private static bool Own(Dictionary<string, int> sources, string name, int ownIndex, int renderer)
        {
            if (ownIndex == -1) return true;
            if (sources == null) return true;
            int source;
            if (!sources.TryGetValue(name, out source)) return false;
            return source == ownIndex;
        }

        private static SceneLookMatrix TargetPose(List<SceneLookBindMesh<string>> bind, string name)
        {
            foreach (SceneLookBindMesh<string> mesh in bind)
            {
                for (int i = 0; i < mesh.Bones.Count; i++)
                {
                    if (mesh.Bones[i] != name) continue;
                    return mesh.Poses[i];
                }
            }
            return SceneLookMatrix.Identity();
        }

        private static SceneLookMatrix Place(SceneLookLook look, SceneLookNode node, Dictionary<long, SceneLookNode> byTransform, Dictionary<long, SceneLookMatrix> rest, Dictionary<long, SceneLookMatrix> worlds, int depth)
        {
            SceneLookMatrix cached;
            if (worlds.TryGetValue(node.Transform, out cached)) return cached;
            if (node.GameObject == look.Root || depth > 256)
            {
                worlds[node.Transform] = SceneLookMatrix.Identity();
                return SceneLookMatrix.Identity();
            }
            worlds[node.Transform] = SceneLookMatrix.Identity();
            SceneLookNode parentNode = Parent(node, byTransform);
            SceneLookMatrix parentWorld = parentNode == null ? SceneLookMatrix.Identity() : Place(look, parentNode, byTransform, rest, worlds, depth + 1);
            SceneLookMatrix bone;
            SceneLookMatrix world;
            if (rest.TryGetValue(node.Transform, out bone)) world = parentWorld.Multiply(SceneLookBind.Local(parentWorld, bone));
            else world = parentWorld.Multiply(Trs(node));
            worlds[node.Transform] = world;
            return world;
        }

        private static List<MeshPoses> LookMeshes(SceneLookLook look)
        {
            List<MeshPoses> meshes = new List<MeshPoses>(look.Renderers.Count);
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                MeshPoses mesh = new MeshPoses();
                mesh.Label = renderer.Name;
                for (int i = 0; i < renderer.Bones.Count; i++)
                {
                    mesh.Names.Add(renderer.Bones[i].Name);
                    mesh.Transforms.Add(renderer.Bones[i].Transform.PathId);
                    mesh.FilePoses.Add(Pose(renderer, i));
                }
                meshes.Add(mesh);
            }
            return meshes;
        }

        private static List<MeshPoses> TargetMeshes(SceneLookFiles files, SceneLookFile file, SceneLookRoot target)
        {
            List<MeshPoses> meshes = new List<MeshPoses>();
            SceneLookScan.CheckState state = new SceneLookScan.CheckState();
            foreach (long pathId in SceneLookScan.RenderersUnder(file, target.GameObject))
            {
                SceneLookScan.RendererCheck check = SceneLookScan.CheckRenderer(files, file, pathId, state);
                SceneLookTarget meshTarget = file.Target(check.Source.Mesh);
                if (meshTarget.Error != null) continue;
                SceneLookMesh mesh = SceneLookReaderMesh.ParseMesh(meshTarget.File, meshTarget.PathId, true);
                if (mesh.BindPoses == null) continue;
                MeshPoses item = new MeshPoses();
                item.Label = check.Source.PathId.ToString(CultureInfo.InvariantCulture);
                for (int i = 0; i < check.Source.Bones.Count && i < mesh.BindPoses.Length; i++)
                {
                    SceneLookTarget bone = file.Target(check.Source.Bones[i]);
                    if (bone.Error != null) continue;
                    item.Names.Add(BoneName(file, check.Source.Bones[i]));
                    item.Transforms.Add(check.Source.Bones[i].PathId);
                    item.FilePoses.Add(SceneLookMatrix.FromFile(mesh.BindPoses[i]));
                }
                meshes.Add(item);
            }
            return meshes;
        }

        private static List<SceneLookBindMesh<long>> LookBindMeshes(List<MeshPoses> meshes)
        {
            List<SceneLookBindMesh<long>> bind = new List<SceneLookBindMesh<long>>(meshes.Count);
            foreach (MeshPoses item in meshes)
            {
                SceneLookBindMesh<long> mesh = new SceneLookBindMesh<long>();
                for (int i = 0; i < item.Transforms.Count; i++) mesh.Add(item.Transforms[i], item.FilePoses[i]);
                bind.Add(mesh);
            }
            return bind;
        }

        private static List<SceneLookBindMesh<string>> TargetBindMeshes(List<MeshPoses> meshes)
        {
            List<SceneLookBindMesh<string>> bind = new List<SceneLookBindMesh<string>>(meshes.Count);
            foreach (MeshPoses item in meshes)
            {
                SceneLookBindMesh<string> mesh = new SceneLookBindMesh<string>();
                for (int i = 0; i < item.Names.Count; i++) mesh.Add(item.Names[i], item.FilePoses[i]);
                bind.Add(mesh);
            }
            return bind;
        }

        private static SceneLookMatrix TargetScene(List<MeshPoses> meshes, SceneLookFile file, long rootTransform, string name)
        {
            foreach (MeshPoses mesh in meshes)
            {
                for (int i = 0; i < mesh.Names.Count; i++)
                {
                    if (mesh.Names[i] != name) continue;
                    return World(file, mesh.Transforms[i], rootTransform);
                }
            }
            return SceneLookMatrix.Identity();
        }

        private static string BoneName(SceneLookFile file, SceneLookRef bone)
        {
            SceneLookTarget target = file.Target(bone);
            if (target.Error != null) return "?";
            return SceneLookReader.BoneName(target);
        }

        private static SceneLookMatrix Pose(SceneLookLookRenderer renderer, int index)
        {
            if (renderer.Mesh.BindPoses == null || index >= renderer.Mesh.BindPoses.Length) return SceneLookMatrix.Identity();
            return SceneLookMatrix.FromFile(renderer.Mesh.BindPoses[index]);
        }

        private static SceneLookMatrix Relative(SceneLookLook look, SceneLookNode node, Dictionary<long, SceneLookNode> byTransform)
        {
            SceneLookMatrix result = Trs(node);
            SceneLookNode current = node;
            while (current != null && current.GameObject != look.Root)
            {
                SceneLookNode parent;
                if (current.Father.FileId != 0 || !byTransform.TryGetValue(current.Father.PathId, out parent)) break;
                result = Trs(parent).Multiply(result);
                current = parent;
            }
            return result;
        }

        private static SceneLookMatrix World(SceneLookFile file, long transform, long stop)
        {
            List<long> chain = new List<long>();
            long current = transform;
            while (current != 0 && current != stop)
            {
                chain.Add(current);
                SceneLookRef father = SceneLookReader.Transform(file, current).Father;
                if (father.FileId != 0 || !file.Contains(father.PathId))
                {
                    current = 0;
                    break;
                }
                current = father.PathId;
            }
            SceneLookMatrix world = SceneLookMatrix.Identity();
            for (int i = chain.Count - 1; i >= 0; i--) world = world.Multiply(Trs(SceneLookReader.Transform(file, chain[i])));
            return world;
        }

        private static SceneLookMatrix Trs(SceneLookNode node)
        {
            return SceneLookMatrix.Trs(node.Position, node.Rotation, node.Scale);
        }

        private static SceneLookMatrix Trs(SceneLookTransform transform)
        {
            return SceneLookMatrix.Trs(transform.Position, transform.Rotation, transform.Scale);
        }

        private static SceneLookNode Parent(SceneLookNode node, Dictionary<long, SceneLookNode> byTransform)
        {
            if (node.Father.FileId != 0) return null;
            SceneLookNode parent;
            if (!byTransform.TryGetValue(node.Father.PathId, out parent)) return null;
            return parent;
        }

        private static SceneLookNode Bone(Dictionary<long, SceneLookNode> byTransform, SceneLookBone bone)
        {
            SceneLookNode node;
            if (byTransform.TryGetValue(bone.Transform.PathId, out node)) return node;
            return null;
        }

        private static int Index(SceneLookLookRenderer renderer, long transform)
        {
            for (int i = 0; i < renderer.Bones.Count; i++)
            {
                if (renderer.Bones[i].Transform.PathId == transform) return i;
            }
            return -1;
        }

        private static SceneLookNode Node(SceneLookLook look, long gameObject)
        {
            foreach (SceneLookNode node in look.Nodes)
            {
                if (node.GameObject == gameObject) return node;
            }
            return null;
        }

        private static Dictionary<long, SceneLookNode> ByTransform(SceneLookLook look)
        {
            Dictionary<long, SceneLookNode> map = new Dictionary<long, SceneLookNode>(look.Nodes.Count);
            foreach (SceneLookNode node in look.Nodes) map[node.Transform] = node;
            return map;
        }

        private static int PoseCount(SceneLookLook look)
        {
            int count = 0;
            foreach (SceneLookLookRenderer renderer in look.Renderers) count += renderer.Bones.Count;
            return count;
        }

        private static int BoneCount(SceneLookLook look)
        {
            HashSet<long> bones = new HashSet<long>();
            foreach (SceneLookLookRenderer renderer in look.Renderers)
            {
                foreach (SceneLookBone bone in renderer.Bones) bones.Add(bone.Transform.PathId);
            }
            return bones.Count;
        }

        private static SceneLookRoot FindTarget(List<SceneLookRoot> roots, string wanted)
        {
            foreach (SceneLookRoot root in roots)
            {
                if (root.Renderers == 0) continue;
                if (root.Name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)) return root;
            }
            return null;
        }

        private static string Row3(SceneLookMatrix matrix)
        {
            return "(" + Format(matrix.M30) + ", " + Format(matrix.M31) + ", " + Format(matrix.M32) + ", " + Format(matrix.M33) + ")";
        }

        private static string Format(float value)
        {
            return value.ToString("0.00000000", CultureInfo.InvariantCulture);
        }

        private static string Label(SceneLookRigPath path)
        {
            if (path == SceneLookRigPath.Name) return "name";
            if (path == SceneLookRigPath.Retarget) return "retarget";
            return "refuse";
        }

        private static long FindRoot(SceneLookFile file, List<SceneLookRoot> roots, string path, out string reason)
        {
            reason = null;
            foreach (SceneLookRoot root in roots)
            {
                if (root.Path == path) return root.GameObject;
            }
            string name = path;
            int slash = path.LastIndexOf('/');
            if (slash >= 0) name = path.Substring(slash + 1);
            foreach (long candidate in file.Order)
            {
                if (file.ClassId(candidate) != SceneLookClass.GameObject) continue;
                if (SceneLookReader.GameObject(file, candidate).Name != name) continue;
                if (SceneLookReader.PathName(file, candidate) == path) return candidate;
            }
            reason = "no GameObject has that path";
            return 0;
        }
    }
}
