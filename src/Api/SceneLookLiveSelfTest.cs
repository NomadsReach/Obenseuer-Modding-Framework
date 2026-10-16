using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class SceneLookLiveSelfTest
    {
        internal const string Ok = "scene look live self-check ok";

        internal static string Run()
        {
            try
            {
                if (SceneLookLivePlan.Spawn(false, false) != SceneLookLiveAction.Refuse) return "a spawn that did not happen was not refused";
                if (SceneLookLivePlan.Spawn(true, false) != SceneLookLiveAction.Despawn) return "a failed look apply did not ask for a despawn";
                if (SceneLookLivePlan.Spawn(true, true) != SceneLookLiveAction.Keep) return "a successful look apply did not keep the spawned npc";

                List<int> hidden = new List<int>(new[] { 11, 12, 13 });
                List<int> now = new List<int>(new[] { 13, 11, 21 });
                List<int> watch = SceneLookLivePlan.Watch(hidden, now);
                if (watch.Count != 2 || watch[0] != 0 || watch[1] != 2) return "the watched objects are not the npc's own renderer objects that still exist";
                if (SceneLookLivePlan.Watch(hidden, new List<int>()).Count != 0) return "an object destroyed with the npc was still watched";
                if (SceneLookLivePlan.Watch(hidden, new List<int>(new[] { 21, 22 })).Count != 0) return "an object that only the look made was watched";

                if (SceneLookLivePlan.Holder(new List<int>(new[] { 1, 2 }), new List<int>(new[] { 1, 2, 3 })) != 3) return "the one object the look added was not found";
                if (SceneLookLivePlan.Holder(new List<int>(new[] { 1, 2 }), new List<int>(new[] { 1, 2 })) != 0) return "a look that added nothing reported a holder";
                if (SceneLookLivePlan.Holder(new List<int>(new[] { 1, 2 }), new List<int>(new[] { 1, 2, 3, 4 })) != 0) return "a look that added two objects reported one of them as the holder";
                if (SceneLookLivePlan.Holder(new List<int>(new[] { 1, 2 }), new List<int>(new[] { 2, 1 })) != 0) return "a reordered object list reported a holder";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return Ok;
        }
    }
}
