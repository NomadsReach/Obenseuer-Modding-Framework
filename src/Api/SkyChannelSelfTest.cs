using System;

namespace ObenseuerCore
{
    internal static class SkyChannelSelfTest
    {
        internal static string Run()
        {
            try
            {
                float[] cell = new float[] { 1f };
                SkyChannel<float> channel = NewFloat();
                channel.Apply(cell, 5f);
                if (cell[0] != 5f || !channel.Held) return "override not written";
                channel.Apply(cell, 5f);
                channel.Apply(cell, 6f);
                if (cell[0] != 6f) return "newer value not written";
                cell[0] = 2f;
                channel.Apply(cell, 6f);
                if (cell[0] != 6f) return "vanilla write not overridden again";
                cell[0] = 3f;
                channel.Apply(cell, null);
                if (cell[0] != 3f || channel.Held) return "release overwrote a newer vanilla value";
                channel.Apply(cell, 7f);
                channel.Apply(cell, null);
                if (cell[0] != 3f) return "release did not restore the last vanilla value";
                channel.Apply(cell, 8f);
                cell[0] = 4f;
                channel.Apply(cell, 8f);
                channel.Forget();
                channel.Apply(cell, null);
                if (cell[0] != 8f) return "forget wrote a value";
                cell[0] = 1f;
                float[] other = new float[] { 1f };
                channel.Apply(cell, 9f);
                channel.Apply(other, 9f);
                channel.Apply(other, null);
                if (cell[0] != 9f) return "target change restored the old value";
                SkyChannel<float> idle = NewFloat();
                cell[0] = 1f;
                idle.Apply(cell, null);
                if (cell[0] != 1f) return "idle release wrote a value";
                string equal = EqualWrites();
                if (equal != null) return equal;
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "sky channel self-check passed";
        }

        private static string EqualWrites()
        {
            bool[] fog = new bool[] { true };
            SkyChannel<bool> channel = NewBool();
            channel.Apply(fog, false);
            fog[0] = true;
            channel.Apply(fog, false);
            Game(channel, fog, false);
            channel.Apply(fog, false);
            channel.Apply(fog, null);
            if (fog[0]) return "game write equal to the override was lost on release";
            fog[0] = false;
            channel = NewBool();
            channel.Apply(fog, true);
            Game(channel, fog, true);
            channel.Apply(fog, true);
            channel.Apply(fog, null);
            if (!fog[0]) return "game write equal to the override was undone on release";
            fog[0] = true;
            channel = NewBool();
            channel.Apply(fog, false);
            channel.Suspend();
            if (!fog[0]) return "suspend did not write the vanilla value";
            channel.Capture();
            channel.Apply(fog, false);
            channel.Apply(fog, null);
            if (!fog[0]) return "a writer that skipped its write changed the vanilla value";
            fog[0] = false;
            channel = NewBool();
            channel.Apply(fog, true);
            fog[0] = false;
            channel.Suspend();
            channel.Capture();
            channel.Apply(fog, true);
            channel.Apply(fog, null);
            if (fog[0]) return "suspend wrote over a newer foreign value";
            return null;
        }

        private static void Game(SkyChannel<bool> channel, bool[] cell, bool value)
        {
            channel.Suspend();
            cell[0] = value;
            channel.Capture();
        }

        private static SkyChannel<float> NewFloat()
        {
            return new SkyChannel<float>(t => ((float[])t)[0], (t, v) => ((float[])t)[0] = v);
        }

        private static SkyChannel<bool> NewBool()
        {
            return new SkyChannel<bool>(t => ((bool[])t)[0], (t, v) => ((bool[])t)[0] = v);
        }
    }
}
