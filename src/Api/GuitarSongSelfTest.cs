using System;

namespace ObenseuerCore
{
    internal static class GuitarSongSelfTest
    {
        internal static string Run()
        {
            try
            {
                if (GuitarSong.Valid(null) || GuitarSong.Valid(new float[0])) return "empty song accepted";
                if (GuitarSong.Valid(new float[] { float.NaN }) || GuitarSong.Valid(new float[] { -0.5f }) || GuitarSong.Valid(new float[] { float.PositiveInfinity })) return "bad delay accepted";
                if (!GuitarSong.Valid(new float[] { 0f, 0.5f })) return "good delays refused";
                GuitarSong spaced = new GuitarSong(new float[] { 0.5f, 0.5f, 0f });
                if (spaced.Advance(0.25f) != 0) return "strum before its delay";
                if (spaced.Advance(0.25f) != 1) return "first strum not at 0.5 s";
                if (spaced.Advance(0.25f) != 0) return "second strum before 1 s";
                if (spaced.Advance(0.25f) != 2) return "second strum and the zero delay not at 1 s";
                if (!spaced.Done) return "song not done after the last note";
                GuitarSong now = new GuitarSong(new float[] { 0f });
                if (now.Advance(0.016f) != 1 || !now.Done) return "zero delay not played on the first tick";
                GuitarSong burst = new GuitarSong(new float[] { 0.1f, 0.1f, 0.1f });
                if (burst.Advance(1f) != 3 || !burst.Done) return "missed notes after a long frame";
                GuitarSong crowd = new GuitarSong(new float[10]);
                if (crowd.Advance(1f) != 4 || crowd.Done) return "strums not capped per frame";
                if (crowd.Advance(0f) != 4 || crowd.Done) return "capped strums not carried to the next frame";
                if (crowd.Advance(0f) != 2 || !crowd.Done) return "capped strums lost";
                GuitarSong late = new GuitarSong(new float[] { 1f });
                if (late.Advance(0.5f) != 0 || late.Done) return "note played before its delay";
                if (late.Advance(0.5f) != 1 || !late.Done) return "note not played at its delay";
                if (new GuitarSong(new float[0]).Advance(1f) != 0) return "empty song played";
            }
            catch (Exception e)
            {
                return "exception: " + e.Message;
            }
            return "guitar song self-check ok";
        }
    }
}
