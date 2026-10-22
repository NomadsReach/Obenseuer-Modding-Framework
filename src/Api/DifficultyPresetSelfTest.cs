using System;

namespace ObenseuerCore
{
    internal static class DifficultyPresetSelfTest
    {
        internal const string Ok = "difficulty preset map self-check ok";

        internal static string Run()
        {
            int[] levels = new int[] { 0, 1, 2, 3, 4, 5, 6 };
            if (DifficultyPresetMap.IndexOf(levels, 0) != 0) return "the first level is the first entry";
            if (DifficultyPresetMap.IndexOf(levels, 6) != 6) return "custom is the seventh entry";
            if (DifficultyPresetMap.IndexOf(levels, 7) != -1) return "a level the game does not have has no index";
            if (DifficultyPresetMap.IndexOf(levels, -1) != -1) return "a level below zero has no index";
            if (DifficultyPresetMap.IndexOf(null, 0) != -1) return "no levels means no index";
            int[] withoutCustom = new int[] { 0, 1, 2, 3, 4, 5 };
            if (DifficultyPresetMap.IndexOf(withoutCustom, 6) != -1) return "a game with six levels has no custom level";
            float[] gameOrder = new float[] { 1f, 0f, 0.25f, 0.5f, 0.75f, 1.25f, 1.5f, 1.75f, 2f, 2.25f, 2.5f, 2.75f, 3f, 3.25f, 3.5f, 3.75f, 4f };
            int[] expected = new int[] { 1, 2, 3, 4, 0, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            int[] order = DifficultyPresetMap.OrderByValue(gameOrder);
            if (order.Length != expected.Length) return "every multiplier step has an order";
            for (int i = 0; i < expected.Length; i++)
            {
                if (order[i] != expected[i]) return "the game's steps sort into value order";
            }
            float[] steps = new float[order.Length];
            for (int i = 0; i < order.Length; i++) steps[i] = gameOrder[order[i]];
            if (steps[0] != 0f || steps[1] != 0.25f || steps[16] != 4f) return "the sorted steps run from off to four times";
            int[] ties = DifficultyPresetMap.OrderByValue(new float[] { 2f, 1f, 1f });
            if (ties[0] != 1 || ties[1] != 2 || ties[2] != 0) return "two equal steps keep their order";
            if (DifficultyPresetMap.OrderByValue(null).Length != 0) return "no steps means no order";
            if (DifficultyPresetMap.Nearest(1f, steps) != 4) return "a normal value stays normal";
            if (DifficultyPresetMap.Nearest(0f, steps) != 0) return "a zero value is the first step";
            if (DifficultyPresetMap.Nearest(4f, steps) != 16) return "the largest step is the last";
            if (DifficultyPresetMap.Nearest(1.6f, steps) != 6) return "a value between two steps takes the nearer one";
            if (DifficultyPresetMap.Nearest(1.625f, steps) != 6) return "an even gap takes the lower step";
            if (DifficultyPresetMap.Nearest(-9f, steps) != 0) return "a value below the range takes the first step";
            if (DifficultyPresetMap.Nearest(9f, steps) != 16) return "a value above the range takes the last step";
            if (DifficultyPresetMap.Nearest(float.NaN, steps) != 0) return "a value that is not a number takes the first step";
            if (DifficultyPresetMap.Nearest(1f, null) != -1) return "no steps means no index";
            for (float value = -3f; value <= 7f; value += 0.05f)
            {
                int step = DifficultyPresetMap.Nearest(value, steps);
                if (step < 0 || step >= steps.Length) return "a step index outside the game's values";
            }
            if (DifficultyModPresets.Clamp(3, 7, 6) != 3) return "an index inside the game's levels is kept";
            if (DifficultyModPresets.Clamp(7, 7, 6) != 6) return "an index past the game's levels takes the preferred level";
            if (DifficultyModPresets.Clamp(99, 7, -1) != 6) return "with no preferred level an index past the end takes the last level";
            if (DifficultyModPresets.Clamp(-4, 7, -1) != 0) return "an index below zero takes the first level";
            if (DifficultyModPresets.Clamp(-4, 7, 3) != 3) return "an index below zero takes the preferred level";
            if (DifficultyModPresets.Clamp(9, 7, 9) != 6) return "a preferred level outside the range is ignored";
            if (DifficultyModPresets.Clamp(2, 0, 0) != 0) return "a game with no levels has no index";
            if (DifficultyModPresets.Length(7, 3) != 10) return "the listed presets follow the game's own levels";
            if (DifficultyModPresets.Length(-1, -1) != 0) return "a missing count is no levels";
            if (!DifficultyModPresets.IsListed(7, 7, 3)) return "the first listed preset is a listed index";
            if (!DifficultyModPresets.IsListed(9, 7, 3)) return "the last listed preset is a listed index";
            if (DifficultyModPresets.IsListed(10, 7, 3)) return "an index past the listed presets is not listed";
            if (DifficultyModPresets.IsListed(6, 7, 3)) return "an index inside the game's levels is not listed";
            if (DifficultyModPresets.IsListed(7, 7, 0)) return "no listed presets means no listed index";
            if (DifficultyModPresets.Restore("example.harsh", "") != "example.harsh") return "a preset picked before a new game is kept when the save has none";
            if (DifficultyModPresets.Restore("example.harsh", "example.other") != "example.other") return "a preset already in the save is not replaced by the pending one";
            if (DifficultyModPresets.Restore("", "example.other") != "example.other") return "a level picked on the screen leaves the save's preset alone";
            if (DifficultyModPresets.Restore(null, "") != "") return "no pending preset leaves the save empty";
            return Ok;
        }
    }
}
