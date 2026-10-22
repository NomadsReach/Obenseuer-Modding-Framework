using System.Collections.Generic;
using ObenseuerCore;

namespace SceneLookHarness
{
    internal static partial class SceneLookHarnessBindPoses
    {
        internal static SceneLookRoot Target(List<SceneLookRoot> roots, string wanted)
        {
            return FindTarget(roots, wanted);
        }

        internal static Dictionary<string, SceneLookMatrix> TargetRest(SceneLookFiles files, SceneLookFile file, SceneLookRoot target)
        {
            List<SceneLookBindMesh<string>> bind = TargetBindMeshes(TargetMeshes(files, file, target));
            int unresolved;
            return SceneLookBind.Rest(bind, out unresolved);
        }
    }
}
