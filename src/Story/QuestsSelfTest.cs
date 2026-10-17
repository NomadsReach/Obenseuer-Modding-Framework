using System;

namespace ObenseuerCore
{
    internal static class QuestsSelfTest
    {
        internal static string Run()
        {
            try
            {
                if (QuestsPatches.CounterMoved(2, 2, ObjectiveStatus.Active)) return "an unchanged counter was announced";
                if (!QuestsPatches.CounterMoved(0, 1, ObjectiveStatus.Active)) return "a counter change on an active objective was not announced";
                if (!QuestsPatches.CounterMoved(0, 1, ObjectiveStatus.Hidden)) return "a counter change on a hidden objective was not announced";
                if (QuestsPatches.CounterMoved(1, 2, ObjectiveStatus.Finished)) return "a completing counter was announced twice";
                if (QuestsPatches.CounterMoved(1, 2, ObjectiveStatus.Failed)) return "a counter change on a failed objective was announced";
                if (!QuestsPatches.CounterMoved(3, 2, ObjectiveStatus.Active)) return "a counter that went down was not announced";
                string gates = QuestsGateSelfTest.Run();
                if (gates != QuestsGateSelfTest.Ok) return gates;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "quests self-check ok";
        }
    }
}
