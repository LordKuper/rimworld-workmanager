using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.WorkManager.Compatibility;
using LordKuper.WorkManager.Helpers;
using RimWorld;
using Verse;
using Enumerable = System.Linq.Enumerable;

namespace LordKuper.WorkManager.Cache;

internal class PawnCache(Pawn pawn)
{
    private readonly Dictionary<WorkTypeDef, bool> _managedWorkTypes = [];
    private readonly Dictionary<SkillDef, float> _skillLearningRates = [];
    private readonly Dictionary<WorkTypeDef, float> _workSkillLearningRates = [];
    private readonly Dictionary<WorkTypeDef, int> _workSkillLevels = [];
    private RimWorldTime _updateTime = new(0);
    private Dictionary<WorkTypeDef, bool> BadWorkTypes { get; } = [];
    private Dictionary<WorkTypeDef, bool> DisabledWorkTypes { get; } = [];
    public RimWorldTime? IdleSince { get; set; }
    public bool IsCapable { get; private set; }
    private bool IsForeigner { get; set; }
    public bool IsManaged { get; private set; }
    public bool IsRecovering { get; private set; }
    private bool IsSlave { get; set; }
    public Pawn Pawn { get; } = pawn;
    private static WorkManagerGameComponent WorkManager => Current.Game.GetComponent<WorkManagerGameComponent>();
    public Dictionary<WorkTypeDef, int> WorkPriorities { get; } = [];

    public Passion GetPassion([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        return !workType.relevantSkills.Any()
            ? Passion.None
            : Enumerable.Max(workType.relevantSkills, skill => Pawn.skills.GetSkill(skill)?.passion ?? Passion.None);
    }

    private float GetSkillLearningRate([NotNull] SkillDef skill)
    {
        if (skill == null) throw new ArgumentNullException(nameof(skill));
        if (_skillLearningRates.TryGetValue(skill, out var rate)) return rate;
        var value = Pawn.skills.GetSkill(skill).LearnRateFactor();
        _skillLearningRates.Add(skill, value);
        return value;
    }

    private float GetWorkSkillLearningRate([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_workSkillLearningRates.TryGetValue(workType, out var rate)) return rate;
        var value = workType.relevantSkills.Any()
            ? Enumerable.Average(Enumerable.Select(workType.relevantSkills, GetSkillLearningRate))
            : 0;
        _workSkillLearningRates.Add(workType, value);
        return value;
    }

    public int GetWorkSkillLevel([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_workSkillLevels.TryGetValue(workType, out var level)) return level;
        var value = workType.relevantSkills.Any()
            ? (int)Math.Floor(Enumerable.Average(Enumerable.Select(workType.relevantSkills,
                skill => Pawn.skills.GetSkill(skill).Level)))
            : 0;
        _workSkillLevels.Add(workType, value);
        return value;
    }

    public bool IsActiveWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        return WorkPriorities[workType] > 0;
    }

    public bool IsBadWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (!MoreThanCapable.MoreThanCapableActive) return false;
        if (BadWorkTypes.TryGetValue(workType, out var work)) return work;
        var value = MoreThanCapable.IsBadWork(Pawn, workType);
        BadWorkTypes.Add(workType, value);
        return value;
    }

    public bool IsDisabledWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (DisabledWorkTypes.TryGetValue(workType, out var work)) return work;
        var value = Pawn.WorkTypeIsDisabled(workType) ||
                    (IsForeigner &&
                     WorkManagerMod.Settings.DisabledWorkTypesForForeigners.Any(dwt => dwt.WorkTypeDef == workType)) ||
                    (IsSlave && WorkManagerMod.Settings.DisabledWorkTypesForSlaves.Any(dwt =>
                        dwt.WorkTypeDef == workType));
        DisabledWorkTypes.Add(workType, value);
        return value;
    }

    public bool IsHunter()
    {
        return !IsDisabledWork(WorkTypeDefOf.Hunting) && !IsBadWork(WorkTypeDefOf.Hunting) &&
               (WorkManagerMod.Settings.AllowMeleeHunters || !Pawn.story.traits.HasTrait(TraitDefOf.Brawler)) &&
               (WorkManagerMod.Settings.AllowMeleeHunters ||
                (Pawn.equipment.Primary != null && !Pawn.equipment.Primary.def.IsMeleeWeapon));
    }

    public bool IsLearningRateAboveThreshold([NotNull] WorkTypeDef workType, bool majorThreshold)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        var threshold = majorThreshold
            ? WorkManagerMod.Settings.MajorLearningRateThreshold
            : WorkManagerMod.Settings.MinorLearningRateThreshold;
        var learnRate = GetWorkSkillLearningRate(workType);
        if (!WorkManagerMod.Settings.UsePawnLearningRateThresholds) return learnRate >= threshold;
        var minLearningRate = Enumerable.Min(_skillLearningRates.Values);
        var maxLearningRate = Enumerable.Max(_skillLearningRates.Values);
        var learningRateRange = maxLearningRate - minLearningRate;
        if (learningRateRange < 0.01) return false;
        return learnRate >= minLearningRate + learningRateRange * threshold;
    }

    public bool IsManagedWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_managedWorkTypes.TryGetValue(workType, out var work)) return work;
        var value = IsManaged && WorkManager.GetWorkTypeEnabled(workType) &&
                    WorkManager.GetPawnWorkTypeEnabled(Pawn, workType);
        _managedWorkTypes.Add(workType, value);
        return value;
    }

    public void Update(RimWorldTime time)
    {
        var hoursPassed = (time.Year - _updateTime.Year) * 60 * 24 + (time.Day - _updateTime.Day) * 24 + time.Hour -
                          _updateTime.Hour;
        _updateTime = time;
        IsCapable = WorkManagerMod.Settings.UncontrollablePawnsUnfitForWork
            ? !Pawn.Dead && !Pawn.Downed && !Pawn.InMentalState && !Pawn.InContainerEnclosed
            : !Pawn.Dead;
        IsRecovering = IsCapable && WorkManagerMod.Settings.RecoveringPawnsUnfitForWork &&
                       HealthAIUtility.ShouldSeekMedicalRest(Pawn);
        IsManaged = WorkManager.GetPawnEnabled(Pawn);
        IsForeigner = Pawn.Faction != Faction.OfPlayer || Pawn.HasExtraMiniFaction() || Pawn.HasExtraHomeFaction();
#if DEBUG
        if (IsForeigner)
            Logger.LogMessage(
                $"{Pawn.LabelShort} is Foreigner {(Pawn.Faction != Faction.OfPlayer ? "(Non-player)" : "")}{(Pawn.HasExtraMiniFaction() ? "(Extra Mini)" : "")}{(Pawn.HasExtraHomeFaction() ? "(Extra Home)" : "")}");
#endif
        IsSlave = ModsConfig.IdeologyActive && Pawn.IsSlaveOfColony;
        WorkPriorities.Clear();
        _managedWorkTypes.Clear();
        var workTypes = Enumerable.Where(DefDatabase<WorkTypeDef>.AllDefsListForReading, w => w.visible);
        foreach (var workType in workTypes)
        {
            WorkPriorities.Add(workType,
                IsManagedWork(workType) ? 0 : WorkTypePriorityHelper.GetPriority(Pawn, workType));
        }
        if (!IsCapable)
        {
#if DEBUG
            Logger.LogMessage(
                $"NOT Updating work type cache for {(!IsCapable ? "[!C]" : "")}{Pawn.LabelShort} (hours passed = {hoursPassed:N1}).");
#endif
            return;
        }
        if (hoursPassed >= 24)
        {
#if DEBUG
            Logger.LogMessage(
                $"Updating work type cache for {(IsForeigner ? "[F]" : "")}{(IsSlave ? "[S]" : "")}{Pawn.LabelShort} (hours passed = {hoursPassed:N1})...");
#endif
            DisabledWorkTypes.Clear();
            BadWorkTypes.Clear();
        }
        if (hoursPassed >= 6)
        {
#if DEBUG
            Logger.LogMessage(
                $"Updating skill cache for {(IsForeigner ? "[F]" : "")}{(IsSlave ? "[S]" : "")}{Pawn.LabelShort} (hours passed = {hoursPassed:N1})...");
#endif
            if (WorkManagerMod.Settings.UsePawnLearningRateThresholds)
            {
                _skillLearningRates.Clear();
                foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
                {
                    _skillLearningRates.Add(skill, Pawn.skills.GetSkill(skill).LearnRateFactor());
                }
            }
            _workSkillLearningRates.Clear();
            _workSkillLevels.Clear();
        }
    }
}