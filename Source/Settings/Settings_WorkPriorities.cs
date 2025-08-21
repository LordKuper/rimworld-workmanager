using System.Collections.Generic;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.Common.Compatibility;
using LordKuper.Common.Helpers;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings.WorkPriorities;

namespace LordKuper.WorkManager;

/// <summary>
///     Contains settings and logic for managing work priorities in the mod.
/// </summary>
public partial class Settings
{
    /// <summary>Default score factor for learning rate of dedicated workers.</summary>
    private const float DedicatedWorkerLearningRateScoreFactorDefault = 0.5f;

    /// <summary>Default score factor for passion of dedicated workers.</summary>
    private const float DedicatedWorkerPassionScoreFactorDefault = 1.5f;

    /// <summary>Default priority for dedicated workers.</summary>
    private const int DedicatedWorkerPriorityDefault = 1;

    /// <summary>Default score factor for skill of dedicated workers.</summary>
    private const float DedicatedWorkerSkillScoreFactorDefault = 1f;

    /// <summary>Default score factor for number of assigned dedicated work types of dedicated workers.</summary>
    private const float DedicatedWorkerWorkCountScoreFactorDefault = 0.5f;

    /// <summary>Default priority for highest skill.</summary>
    private const int HighestSkillPriorityDefault = 1;

    /// <summary>Default priority for idle work.</summary>
    private const int IdlePriorityDefault = 4;

    /// <summary>Default priority for leftover work.</summary>
    private const int LeftoverPriorityDefault = 4;

    /// <summary>Default priority for major learning rate.</summary>
    private const int MajorLearningRatePriorityDefault = 2;

    /// <summary>Default threshold for major learning rate.</summary>
    private const float MajorLearningRateThresholdDefault = 1.2f;

    /// <summary>Default priority for minor learning rate.</summary>
    private const int MinorLearningRatePriorityDefault = 3;

    /// <summary>Default threshold for minor learning rate.</summary>
    private const float MinorLearningRateThresholdDefault = 0.8f;

    /// <summary>Maximum allowed score factor.</summary>
    private const float ScoreFactorMax = 5f;

    /// <summary>Minimum allowed score factor.</summary>
    private const float ScoreFactorMin = 0f;

    /// <summary>Default update frequency for work priorities.</summary>
    private const float WorkPrioritiesUpdateFrequencyDefault = 1f / 24f;

    /// <summary>Maximum allowed update frequency for work priorities.</summary>
    private const float WorkPrioritiesUpdateFrequencyMax = RimWorldTime.DaysInQuadrum / 3f;

    /// <summary>Minimum allowed update frequency for work priorities.</summary>
    private const float WorkPrioritiesUpdateFrequencyMin = 1f / 48f;

    /// <summary>
    ///     Maximum allowed threshold for major learning rate, depending on mod compatibility.
    /// </summary>
    private static readonly float MajorLearningRateThresholdMax = Vse.VanillaSkillsExpandedActive ? 3f : 2f;

    /// <summary>Cached height of the work priorities content for UI layout.</summary>
    private float _workPrioritiesContentHeight;

    /// <summary>If true, assign all work types to pawns.</summary>
    public bool AssignAllWorkTypes;

    /// <summary>If true, assign work to idle pawns.</summary>
    public bool AssignWorkToIdlePawns = true;

    /// <summary>Score factor for learning rate of dedicated workers.</summary>
    public float DedicatedWorkerLearningRateScoreFactor = DedicatedWorkerLearningRateScoreFactorDefault;

    /// <summary>Score factor for passion of dedicated workers.</summary>
    public float DedicatedWorkerPassionScoreFactor = DedicatedWorkerPassionScoreFactorDefault;

    /// <summary>Priority for dedicated workers.</summary>
    public int DedicatedWorkerPriority = DedicatedWorkerPriorityDefault;

    /// <summary>Score factor for skill of dedicated workers.</summary>
    public float DedicatedWorkerSkillScoreFactor = DedicatedWorkerSkillScoreFactorDefault;

    /// <summary>Score factor for number of assigned dedicated work types of dedicated workers.</summary>
    public float DedicatedWorkerWorkCountScoreFactor = DedicatedWorkerWorkCountScoreFactorDefault;

    /// <summary>Priority for highest skill.</summary>
    public int HighestSkillPriority = HighestSkillPriorityDefault;

    /// <summary>Priority for idle work.</summary>
    public int IdlePriority = IdlePriorityDefault;

    /// <summary>Priority for leftover work.</summary>
    public int LeftoverPriority = LeftoverPriorityDefault;

    /// <summary>Priority for major learning rate.</summary>
    public int MajorLearningRatePriority = MajorLearningRatePriorityDefault;

    /// <summary>Threshold for major learning rate.</summary>
    public float MajorLearningRateThreshold = MajorLearningRateThresholdDefault;

    /// <summary>Maximum allowed work type priority.</summary>
    internal int MaxWorkTypePriority = 4;

    /// <summary>Priority for minor learning rate.</summary>
    public int MinorLearningRatePriority = MinorLearningRatePriorityDefault;

    /// <summary>Threshold for minor learning rate.</summary>
    public float MinorLearningRateThreshold = MinorLearningRateThresholdDefault;

    /// <summary>Dictionary mapping passion def names to their priorities.</summary>
    public Dictionary<string, int> PassionPriorities = PassionPrioritiesDefault;

    /// <summary>If true, use dedicated workers logic.</summary>
    public bool UseDedicatedWorkers = true;

    /// <summary>If true, use learning rates for priorities.</summary>
    public bool UseLearningRatesPriorities;

    /// <summary>If true, use passion priorities.</summary>
    public bool UsePassionPriorities = true;

    /// <summary>If true, use pawn-specific learning rate thresholds.</summary>
    public bool UsePawnLearningRateThresholds;

    /// <summary>Frequency at which work priorities are updated.</summary>
    public float WorkPrioritiesUpdateFrequency = WorkPrioritiesUpdateFrequencyDefault;

    /// <summary>
    ///     Gets the default dictionary of passion priorities, depending on mod compatibility.
    /// </summary>
    [NotNull]
    private static Dictionary<string, int> PassionPrioritiesDefault =>
        Vse.VanillaSkillsExpandedActive
            ? new Dictionary<string, int>
            {
                { nameof(Passion.None), 0 }, { nameof(Passion.Minor), 3 }, { nameof(Passion.Major), 2 },
                { "VSE_Apathy", 0 }, { "VSE_Natural", 2 }, { "VSE_Critical", 1 }
            }
            : new Dictionary<string, int>
                { { nameof(Passion.None), 0 }, { nameof(Passion.Minor), 3 }, { nameof(Passion.Major), 2 } };

    /// <summary>
    ///     Draws the work priorities tab in the settings UI.
    /// </summary>
    /// <param name="rect">The rectangle area to draw the tab in.</param>
    private void DoWorkPrioritiesTab(Rect rect)
    {
        Tabs.DoTab(rect, 0, null, _workPrioritiesContentHeight, ref _scrollPosition, DoWorkPrioritiesTabContent, 0,
            null);
    }

    /// <summary>
    ///     Draws the content of the work priorities tab, including all relevant settings fields.
    /// </summary>
    /// <param name="rect">The rectangle area to draw the content in.</param>
    private void DoWorkPrioritiesTabContent(Rect rect)
    {
        var y = 0f;
        y += Fields.DoLabeledFrequencySlider(rect, 0, null, Strings.UpdateFrequency, Strings.UpdateFrequencyTooltip,
            ref WorkPrioritiesUpdateFrequency, WorkPrioritiesUpdateFrequencyMin, WorkPrioritiesUpdateFrequencyMax, true,
            null, out var remRect);
        y += Fields.DoLabeledCheckbox(remRect, 0, null, ref UseDedicatedWorkers, Strings.UseDedicatedWorkers,
            Strings.UseDedicatedWorkersTooltip, null, out remRect);
        if (UseDedicatedWorkers)
        {
            y += Fields.DoLabeledIntegerSlider(remRect, 1, null, Strings.DedicatedWorkerPriority,
                Strings.DedicatedWorkerPriorityTooltip, ref DedicatedWorkerPriority, 1, MaxWorkTypePriority, 1, null,
                out remRect);
            y += Fields.DoLabeledFloatSlider(remRect, 1, null, Strings.DedicatedWorkerSkillScoreFactor,
                Strings.DedicatedWorkerSkillScoreFactorTooltip, ref DedicatedWorkerSkillScoreFactor, ScoreFactorMin,
                ScoreFactorMax, 0.1f, null, out remRect);
            y += Fields.DoLabeledFloatSlider(remRect, 1, null, Strings.DedicatedWorkerPassionScoreFactor,
                Strings.DedicatedWorkerPassionScoreFactorTooltip, ref DedicatedWorkerPassionScoreFactor, ScoreFactorMin,
                ScoreFactorMax, 0.1f, null, out remRect);
            y += Fields.DoLabeledFloatSlider(remRect, 1, null, Strings.DedicatedWorkerLearningRateScoreFactor,
                Strings.DedicatedWorkerLearningRateScoreFactorTooltip, ref DedicatedWorkerLearningRateScoreFactor,
                ScoreFactorMin, ScoreFactorMax, 0.1f, null, out remRect);
            y += Fields.DoLabeledFloatSlider(remRect, 1, null, Strings.DedicatedWorkerWorkCountScoreFactor,
                Strings.DedicatedWorkerWorkCountScoreFactorTooltip, ref DedicatedWorkerWorkCountScoreFactor,
                ScoreFactorMin, ScoreFactorMax, 0.1f, null, out remRect);
        }
        else
        {
            y += Fields.DoLabeledIntegerSlider(remRect, 1, null, Strings.HighestSkillPriority,
                Strings.HighestSkillPriorityTooltip, ref HighestSkillPriority, 1, MaxWorkTypePriority, 1, null,
                out remRect);
        }
        y += Fields.DoLabeledCheckbox(remRect, 0, null, ref AssignAllWorkTypes, Strings.AssignAllWorkTypes,
            Strings.AssignAllWorkTypesTooltip, null, out remRect);
        if (AssignAllWorkTypes)
            y += Fields.DoLabeledIntegerSlider(remRect, 1, null, Strings.LeftoverPriority,
                Strings.LeftoverPriorityTooltip, ref LeftoverPriority, 1, MaxWorkTypePriority, 1, null, out remRect);
        y += Fields.DoLabeledCheckbox(remRect, 0, null, ref AssignWorkToIdlePawns, Strings.AssignWorkToIdlePawns,
            Strings.AssignWorkToIdlePawnsTooltip, null, out remRect);
        if (AssignWorkToIdlePawns)
            y += Fields.DoLabeledIntegerSlider(remRect, 1, null, Strings.IdlePriority, Strings.IdlePriorityTooltip,
                ref IdlePriority, 1, MaxWorkTypePriority, 1, null, out remRect);
        y += Fields.DoLabeledCheckbox(remRect, 0, null, ref UsePassionPriorities, Strings.UsePassionPriorities,
            Strings.UsePassionPrioritiesTooltip, null, out remRect);
        if (UsePassionPriorities)
            foreach (var pc in PassionHelper.Passions)
            {
                if (!PassionPriorities.TryGetValue(pc.DefName, out var priority)) continue;
                y += Fields.DoLabeledIntegerSlider(remRect, 1, null, pc.Label, pc.Description, ref priority, 0,
                    MaxWorkTypePriority, 1, pc.Icon, out remRect);
                PassionPriorities[pc.DefName] = priority;
            }
        y += Fields.DoLabeledCheckbox(remRect, 0, null, ref UseLearningRatesPriorities,
            Strings.UseLearningRatesPriorities, Strings.UseLearningRatesPrioritiesTooltip, null, out remRect);
        if (UseLearningRatesPriorities)
        {
            y += Fields.DoLabeledCheckbox(remRect, 1, null, ref UsePawnLearningRateThresholds,
                Strings.UsePawnLearningRateThresholds, Strings.UsePawnLearningRateThresholdsTooltip, null, out remRect);
            y += Fields.DoLabeledPercentSlider(remRect, 1, null, Strings.MinorLearningRateThreshold,
                Strings.MinorLearningRateThresholdTooltip, ref MinorLearningRateThreshold, 0.1f,
                MajorLearningRateThreshold - 0.1f, 0.1f, null, out remRect);
            y += Fields.DoLabeledIntegerSlider(remRect, 1, null, Strings.MinorLearningRatePriority,
                Strings.MinorLearningRatePriorityTooltip, ref MinorLearningRatePriority, 1, MaxWorkTypePriority, 1,
                null, out remRect);
            y += Fields.DoLabeledPercentSlider(remRect, 1, null, Strings.MajorLearningRateThreshold,
                Strings.MajorLearningRateThresholdTooltip, ref MajorLearningRateThreshold,
                MinorLearningRateThreshold + 0.1f, UsePawnLearningRateThresholds ? 1f : MajorLearningRateThresholdMax,
                0.1f, null, out remRect);
            y += Fields.DoLabeledIntegerSlider(remRect, 1, null, Strings.MajorLearningRatePriority,
                Strings.MajorLearningRatePriorityTooltip, ref MajorLearningRatePriority, 1, MaxWorkTypePriority, 1,
                null, out _);
        }
        if (Event.current.type == EventType.Layout) _workPrioritiesContentHeight = y;
    }

    /// <summary>
    ///     Exposes (saves/loads) the work priorities settings data using the Scribe system.
    /// </summary>
    private void ExposeWorkPrioritiesData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) ValidateWorkPriorities();
        Scribe_Values.Look(ref WorkPrioritiesUpdateFrequency, nameof(WorkPrioritiesUpdateFrequency),
            WorkPrioritiesUpdateFrequencyDefault);
        Scribe_Values.Look(ref UseDedicatedWorkers, nameof(UseDedicatedWorkers), true);
        Scribe_Values.Look(ref DedicatedWorkerPriority, nameof(DedicatedWorkerPriority),
            DedicatedWorkerPriorityDefault);
        Scribe_Values.Look(ref DedicatedWorkerSkillScoreFactor, nameof(DedicatedWorkerSkillScoreFactor),
            DedicatedWorkerSkillScoreFactorDefault);
        Scribe_Values.Look(ref DedicatedWorkerPassionScoreFactor, nameof(DedicatedWorkerPassionScoreFactor),
            DedicatedWorkerPassionScoreFactorDefault);
        Scribe_Values.Look(ref DedicatedWorkerLearningRateScoreFactor, nameof(DedicatedWorkerLearningRateScoreFactor),
            DedicatedWorkerLearningRateScoreFactorDefault);
        Scribe_Values.Look(ref DedicatedWorkerWorkCountScoreFactor, nameof(DedicatedWorkerWorkCountScoreFactor),
            DedicatedWorkerWorkCountScoreFactorDefault);
        Scribe_Values.Look(ref HighestSkillPriority, nameof(HighestSkillPriority), HighestSkillPriorityDefault);
        Scribe_Values.Look(ref AssignAllWorkTypes, nameof(AssignAllWorkTypes));
        Scribe_Values.Look(ref LeftoverPriority, nameof(LeftoverPriority), LeftoverPriorityDefault);
        Scribe_Values.Look(ref AssignWorkToIdlePawns, nameof(AssignWorkToIdlePawns), true);
        Scribe_Values.Look(ref IdlePriority, nameof(IdlePriority), 4);
        Scribe_Values.Look(ref UsePassionPriorities, nameof(UsePassionPriorities), true);
        Scribe_Collections.Look(ref PassionPriorities, nameof(PassionPriorities), LookMode.Value, LookMode.Value);
        Scribe_Values.Look(ref UseLearningRatesPriorities, nameof(UseLearningRatesPriorities));
        Scribe_Values.Look(ref UsePawnLearningRateThresholds, nameof(UsePawnLearningRateThresholds));
        Scribe_Values.Look(ref MinorLearningRateThreshold, nameof(MinorLearningRateThreshold),
            MinorLearningRateThresholdDefault);
        Scribe_Values.Look(ref MinorLearningRatePriority, nameof(MinorLearningRatePriority),
            MinorLearningRatePriorityDefault);
        Scribe_Values.Look(ref MajorLearningRateThreshold, nameof(MajorLearningRateThreshold),
            MajorLearningRateThresholdDefault);
        Scribe_Values.Look(ref MajorLearningRatePriority, nameof(MajorLearningRatePriority),
            MajorLearningRatePriorityDefault);
    }

    /// <summary>
    ///     Validates and adjusts work priority-related settings to ensure they fall within acceptable ranges or default
    ///     values.
    /// </summary>
    /// <remarks>
    ///     This method ensures that all work priority parameters, thresholds, and factors are properly
    ///     initialized and constrained  within their defined limits. Default values are applied where necessary, and
    ///     invalid or out-of-range values are clamped  to maintain consistency. Additionally, missing passion priorities
    ///     are initialized with default values.
    /// </remarks>
    private void ValidateWorkPriorities()
    {
        WorkPrioritiesUpdateFrequency = WorkPrioritiesUpdateFrequency == 0
            ? WorkPrioritiesUpdateFrequencyDefault
            : Mathf.Clamp(WorkPrioritiesUpdateFrequency, WorkPrioritiesUpdateFrequencyMin,
                WorkPrioritiesUpdateFrequencyMax);
        DedicatedWorkerPriority = DedicatedWorkerPriority == 0
            ? DedicatedWorkerPriorityDefault
            : Mathf.Clamp(DedicatedWorkerPriority, 1, MaxWorkTypePriority);
        DedicatedWorkerSkillScoreFactor = Mathf.Clamp(DedicatedWorkerSkillScoreFactor, ScoreFactorMin, ScoreFactorMax);
        DedicatedWorkerPassionScoreFactor =
            Mathf.Clamp(DedicatedWorkerPassionScoreFactor, ScoreFactorMin, ScoreFactorMax);
        DedicatedWorkerLearningRateScoreFactor =
            Mathf.Clamp(DedicatedWorkerLearningRateScoreFactor, ScoreFactorMin, ScoreFactorMax);
        HighestSkillPriority = HighestSkillPriority == 0
            ? HighestSkillPriorityDefault
            : Mathf.Clamp(HighestSkillPriority, 1, MaxWorkTypePriority);
        LeftoverPriority = LeftoverPriority == 0
            ? LeftoverPriorityDefault
            : Mathf.Clamp(LeftoverPriority, 1, MaxWorkTypePriority);
        IdlePriority = IdlePriority == 0 ? IdlePriorityDefault : Mathf.Clamp(IdlePriority, 1, MaxWorkTypePriority);
        if (PassionPriorities == null || PassionPriorities.Count == 0)
            PassionPriorities = PassionPrioritiesDefault;
        foreach (var passion in PassionHelper.Passions)
        {
            if (PassionPriorities.TryGetValue(passion.DefName, out var priority))
                PassionPriorities[passion.DefName] = Mathf.Clamp(priority, 0, MaxWorkTypePriority);
            else
                PassionPriorities[passion.DefName] = 0;
        }
        MajorLearningRateThreshold = MajorLearningRateThreshold == 0
            ? MajorLearningRateThresholdDefault
            : Mathf.Clamp(MajorLearningRateThreshold, MinorLearningRateThreshold + 0.1f, MajorLearningRateThresholdMax);
        MajorLearningRatePriority = MajorLearningRatePriority == 0
            ? MajorLearningRatePriorityDefault
            : Mathf.Clamp(MajorLearningRatePriority, 1, MaxWorkTypePriority);
        MinorLearningRateThreshold = MinorLearningRateThreshold == 0
            ? MinorLearningRateThresholdDefault
            : Mathf.Clamp(MinorLearningRateThreshold, 0.1f, MajorLearningRateThreshold - 0.1f);
        MinorLearningRatePriority = MinorLearningRatePriority == 0
            ? MinorLearningRatePriorityDefault
            : Mathf.Clamp(MinorLearningRatePriority, 1, MaxWorkTypePriority);
    }
}