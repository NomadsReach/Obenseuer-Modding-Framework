using System;

namespace ObenseuerCore
{
    internal static class NpcWalkMath
    {
        private const float MinSpeed = 0.5f;
        private const float TimeoutFactor = 3f;
        private const float TimeoutSlack = 10f;
        private const float MinWait = 4f;
        private const float WaitSpan = 11f;

        internal static float Timeout(float distance, float speed)
        {
            return distance / Math.Max(speed, MinSpeed) * TimeoutFactor + TimeoutSlack;
        }

        internal static void DiscPoint(Random random, float radius, out float dx, out float dz)
        {
            double angle = random.NextDouble() * 2.0 * Math.PI;
            double distance = Math.Sqrt(random.NextDouble()) * radius;
            dx = (float)(Math.Cos(angle) * distance);
            dz = (float)(Math.Sin(angle) * distance);
        }

        internal static int NextPatrolIndex(int current, int count)
        {
            return count <= 0 ? 0 : (current + 1) % count;
        }

        internal static float WaitSeconds(Random random)
        {
            return MinWait + (float)random.NextDouble() * WaitSpan;
        }

        internal static bool IsOrigin(float x, float y, float z)
        {
            return x == 0f && y == 0f && z == 0f;
        }
    }

    internal static class NpcWalkMathCheck
    {
        internal static string Run()
        {
            if (NpcWalkMath.Timeout(0f, 0f) != 10f) return "timeout for a zero walk";
            if (NpcWalkMath.Timeout(10f, 0f) != 70f) return "timeout at zero speed";
            if (NpcWalkMath.Timeout(9f, 3f) != 19f) return "timeout at walk speed";
            Random random = new Random(7);
            for (int i = 0; i < 10000; i++)
            {
                float dx;
                float dz;
                NpcWalkMath.DiscPoint(random, 4f, out dx, out dz);
                if (dx * dx + dz * dz > 16f * (1f + 1e-5f)) return "disc point outside the radius";
            }
            if (NpcWalkMath.NextPatrolIndex(0, 3) != 1) return "patrol step";
            if (NpcWalkMath.NextPatrolIndex(2, 3) != 0) return "patrol wrap";
            if (NpcWalkMath.NextPatrolIndex(0, 0) != 0) return "patrol without points";
            for (int i = 0; i < 10000; i++)
            {
                float wait = NpcWalkMath.WaitSeconds(random);
                if (wait < 4f || wait > 15f) return "wait outside 4 to 15 seconds";
            }
            if (!NpcWalkMath.IsOrigin(0f, 0f, 0f)) return "origin not detected";
            if (NpcWalkMath.IsOrigin(0f, 0.1f, 0f)) return "off origin detected as origin";
            return null;
        }
    }
}
