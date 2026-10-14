using System;

namespace ObenseuerCore
{
    internal static class PlayerWait
    {
        private sealed class Pending
        {
            internal string ModId;
            internal bool IgnoreNeeds;
            internal bool NoPassOut;
            internal int Seconds;
            internal Action<int> OnDone;
        }

        private static Pending pending;

        internal static bool Installed;

        internal static bool PassingOut;

        internal static bool Starting;

        internal static bool Available
        {
            get { return Installed && !CorePatcher.IsDisabled(PlayerState.WaitResultOwner); }
        }

        internal static bool Active
        {
            get { return pending != null; }
        }

        internal static bool FreezeNeeds
        {
            get { return pending != null && pending.IgnoreNeeds; }
        }

        internal static bool BlockPassOut
        {
            get { return pending != null && pending.NoPassOut; }
        }

        internal static bool Start(string modId, int seconds, WaitKind kind, WaitOptions options, Action<int> onDone, WaitingController waiting)
        {
            pending = new Pending();
            pending.ModId = modId;
            pending.IgnoreNeeds = options.IgnoreNeeds;
            pending.NoPassOut = options.NoPassOut;
            pending.OnDone = onDone;
            Starting = true;
            try
            {
                waiting.Wait(seconds, (WaitingController.Type)(int)kind, 0f, options.IgnoreNeeds, null);
                return true;
            }
            catch
            {
                pending = null;
                throw;
            }
            finally
            {
                Starting = false;
            }
        }

        internal static void GameWaitStarting()
        {
            if (!Starting) Finish();
        }

        internal static void Tick(int sec)
        {
            if (pending != null) pending.Seconds += sec;
        }

        internal static void EndIfOver()
        {
            WaitingController waiting = WaitingController.instance;
            if (pending != null && !PassingOut && (waiting == null || !waiting.IsWaiting)) Finish();
        }

        internal static void SceneChanged()
        {
            Finish();
        }

        private static void Finish()
        {
            Pending done = pending;
            if (done == null) return;
            pending = null;
            try
            {
                done.OnDone(PlayerWaitMath.MinutesPassed(done.Seconds));
            }
            catch (Exception e)
            {
                ApiLog.Once(done.ModId, "PlayerState.Wait onDone", e);
            }
        }
    }
}
