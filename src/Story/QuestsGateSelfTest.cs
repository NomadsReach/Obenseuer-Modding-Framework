using System;

namespace ObenseuerCore
{
    internal static class QuestsGateSelfTest
    {
        internal const string Ok = "quest gate self-check ok";

        internal static string Run()
        {
            try
            {
                RetryGate gate = new RetryGate();
                if (gate.Waiting || gate.Done) return "a new gate is already running";
                if (gate.Try(true)) return "an unarmed gate reported ready";
                gate.Arm();
                if (!gate.Waiting || gate.Attempts != 0) return "an armed gate is not waiting at zero attempts";
                int attempts = 0;
                while (gate.Waiting)
                {
                    gate.Try(false);
                    attempts++;
                }
                if (attempts != RetryGate.MaxAttempts) return "the gate ran " + attempts + " attempts instead of " + RetryGate.MaxAttempts;
                if (gate.Done) return "an exhausted gate reported done";
                if (gate.Try(false)) return "an exhausted gate tried again";
                gate.Arm();
                if (!gate.Waiting) return "an exhausted gate did not re-arm";
                if (!gate.Try(true)) return "a ready gate did not report ready";
                if (gate.Waiting || !gate.Done) return "a ready gate stayed waiting";
                if (gate.Attempts != 1) return "a ready gate counted the wrong attempts";
                gate.Arm();
                if (gate.Waiting) return "a done gate re-armed";
                if (gate.Try(true)) return "a done gate reported ready twice";
                gate.Reset();
                if (gate.Waiting || gate.Done || gate.Attempts != 0) return "a reset gate kept its state";
                if (gate.Try(true)) return "a reset gate reported ready while it was not waiting";
                gate.Arm();
                if (!gate.Waiting || gate.Done) return "a reset gate did not arm for the next load";
                if (!gate.Try(true)) return "a reset gate did not report ready";
                if (!gate.Done || gate.Waiting) return "a reset gate stayed waiting after it reported ready";
                if (gate.Attempts != 1) return "a reset gate counted the wrong attempts";
                gate.Clear();
                if (gate.Waiting) return "a cleared gate stayed waiting";
                if (gate.Try(true)) return "a cleared gate reported ready";

                ReadySaveGate saves = new ReadySaveGate();
                if (saves.ShouldArm(SessionKind.LevelChange)) return "a level change armed the ready gate";
                if (!saves.ShouldArm(SessionKind.SaveLoad)) return "a loaded save did not arm the ready gate";
                if (!saves.ShouldArm(SessionKind.SaveLoad)) return "the same save loaded again did not arm the ready gate";
                if (saves.ShouldArm(SessionKind.LevelChange)) return "a level change inside a save armed the ready gate";
                if (!saves.ShouldArm(SessionKind.SaveLoad)) return "another save did not arm the ready gate";
                if (!saves.ShouldArm(SessionKind.NewGame)) return "a new game did not arm the ready gate";
                if (!saves.ShouldArm(SessionKind.NewGame)) return "a second new game did not arm the ready gate";
                if (!saves.ShouldArm(SessionKind.SaveLoad)) return "a save loaded after a new game did not arm the ready gate";
                if (saves.ShouldArm(SessionKind.LevelChange)) return "a level change after a save armed the ready gate";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return Ok;
        }
    }
}
