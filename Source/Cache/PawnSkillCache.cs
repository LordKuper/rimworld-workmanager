using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.Common.Cache;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager.Cache;

/// <summary>
///     Caches skill learning rates and work skill levels for a specific pawn.
/// </summary>
/// <remarks>
///     This cache is updated periodically and provides efficient access to skill-related data for work management.
/// </remarks>
internal class PawnSkillCache(Pawn pawn) : TimedCache(1f)
{
    /// <summary>
    ///     Stores cached learning rates for individual skills.
    /// </summary>
    private readonly Dictionary<SkillDef, float> _skillLearningRates = [];

    /// <summary>
    ///     Stores cached average learning rates to work types.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, float> _workSkillLearningRates = [];

    /// <summary>
    ///     Stores cached average skill levels for work types.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, int> _workSkillLevels = [];

    /// <summary>
    ///     Gets the learning rate for a specific skill, caching the result.
    /// </summary>
    /// <param name="skill">The skill definition.</param>
    /// <returns>The learning rate factor for the skill.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="skill" /> is null.</exception>
    private float GetSkillLearningRate([NotNull] SkillDef skill)
    {
        if (skill == null) throw new ArgumentNullException(nameof(skill));
        if (_skillLearningRates.TryGetValue(skill, out var rate)) return rate;
        var value = pawn.skills.GetSkill(skill).LearnRateFactor();
        _skillLearningRates.Add(skill, value);
        return value;
    }

    /// <summary>
    ///     Gets the average learning rate for all relevant skills of a work type, caching the result.
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>The average learning rate factor for the work type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    public float GetWorkSkillLearningRate([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_workSkillLearningRates.TryGetValue(workType, out var rate)) return rate;
        var relevantSkills = workType.relevantSkills;
        if (relevantSkills == null || relevantSkills.Count == 0)
        {
            _workSkillLearningRates.Add(workType, 0f);
            return 0f;
        }
        var sum = 0f;
        var count = relevantSkills.Count;
        for (var i = 0; i < count; i++)
        {
            sum += GetSkillLearningRate(relevantSkills[i]);
        }
        var value = sum / count;
        _workSkillLearningRates.Add(workType, value);
        return value;
    }

    /// <summary>
    ///     Gets the average skill level for all relevant skills of a work type, caching the result.
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>The average skill level for the work type, rounded down.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    public int GetWorkSkillLevel([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_workSkillLevels.TryGetValue(workType, out var level)) return level;
        var relevantSkills = workType.relevantSkills;
        if (relevantSkills == null || relevantSkills.Count == 0)
        {
            _workSkillLevels.Add(workType, 0);
            return 0;
        }
        var sum = 0;
        var count = relevantSkills.Count;
        for (var i = 0; i < count; i++)
        {
            sum += pawn.skills.GetSkill(relevantSkills[i]).Level;
        }
        var value = (int)Math.Floor((double)sum / count);
        _workSkillLevels.Add(workType, value);
        return value;
    }

    /// <summary>
    ///     Updates the cache with the latest skill and work type data for the pawn.
    /// </summary>
    /// <param name="time">The current RimWorld time.</param>
    /// <returns>
    ///     <c>true</c> if the cache was updated; otherwise, <c>false</c>.
    /// </returns>
    public override bool Update(RimWorldTime time)
    {
        if (!base.Update(time)) return false;
        _workSkillLearningRates.Clear();
        _workSkillLevels.Clear();
        if (WorkManagerMod.Settings.UsePawnLearningRateThresholds)
        {
            _skillLearningRates.Clear();
            var allSkills = DefDatabase<SkillDef>.AllDefsListForReading;
            var count = allSkills.Count;
            for (var i = 0; i < count; i++)
            {
                var skill = allSkills[i];
                _skillLearningRates.Add(skill, pawn.skills.GetSkill(skill).LearnRateFactor());
            }
        }
        return true;
    }
}