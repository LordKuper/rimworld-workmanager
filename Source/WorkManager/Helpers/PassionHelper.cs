using System.Collections.Generic;
using LordKuper.Common.Helpers;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager.Helpers;

/// <summary>
///     Provides helper methods for calculating and retrieving normalized passion scores.
/// </summary>
internal static class PassionHelper
{
    /// <summary>
    ///     Caches normalized scores for each <see cref="Passion" /> value.
    /// </summary>
    private static readonly Dictionary<Passion, float> PassionScores = [];

    /// <summary>
    ///     Gets the normalized score for a given <see cref="Passion" />.
    ///     The score is calculated based on the learn and forget rate factors,
    ///     normalized to a 0-1 range across all available passions.
    /// </summary>
    /// <param name="passion">The <see cref="Passion" /> to retrieve the score for.</param>
    /// <returns>The normalized score for the specified passion, or 0 if not found.</returns>
    internal static float GetPassionScore(Passion passion)
    {
        if (PassionScores.TryGetValue(passion, out var score)) return score;
        var passions = Common.Helpers.PassionHelper.Passions;
        float min = float.MaxValue, max = float.MinValue;
        var scores = new Dictionary<Passion, float>(passions.Count);
        foreach (var pc in passions)
        {
            var s = pc.LearnRateFactor * 2f - pc.ForgetRateFactor;
            scores[pc.Passion] = s;
            if (s < min) min = s;
            if (s > max) max = s;
        }
        var range = new FloatRange(min, max);
        foreach (var kvp in scores)
        {
            var normalized = MathHelper.NormalizeValue(kvp.Value, range);
            PassionScores[kvp.Key] = normalized;
        }
        return PassionScores.TryGetValue(passion, out score) ? score : 0f;
    }
}