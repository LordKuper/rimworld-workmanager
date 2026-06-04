using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.Common;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager;

/// <summary>
///     Map component that assigns pawn work schedules based on configured work shifts.
/// </summary>
/// <remarks>
///     <strong>Invariant:</strong> <see cref="WorkManagerGameComponent.Instance" /> is non-null for the
///     entire lifetime of this component. A <see cref="MapComponent" /> can only exist while a
///     <see cref="Map" /> exists, which requires an active <see cref="Game" />; the
///     <see cref="WorkManagerGameComponent" /> constructor runs when that game is created and sets
///     <c>Instance</c> before any map tick can fire. No null guard is required or expected here.
/// </remarks>
/// <param name="map">The map this component belongs to.</param>
[UsedImplicitly]
public class ScheduleUpdater(Map map) : MapComponent(map)
{
    /// <summary>
    ///     Shift score bonus for each skill first covered by a pawn in that shift.
    /// </summary>
    private const float CoverageBonus = 6f;

    /// <summary>
    ///     Shift score penalty per worker already assigned to that shift (even distribution).
    /// </summary>
    private const float EvennessWeight = 1f;

    /// <summary>
    ///     Weight of the skill learning rate in the per-skill score.
    /// </summary>
    private const float LearnRateCoef = 5f;

    /// <summary>
    ///     Weight of the normalized skill level in the per-skill score.
    /// </summary>
    private const float LevelWeight = 10f;

    /// <summary>
    ///     Shift score bonus per lover/partner already assigned to that shift.
    /// </summary>
    private const float LoverShiftScoreBonus = 10f;

    /// <summary>
    ///     Bitmask gating updates to once every 64 game ticks.
    /// </summary>
    private const int UpdateTickMask = 0x3F;

    private static TraitDef? _nightOwlTrait;
    private readonly Dictionary<Pawn, WorkShift> _workers = [];
    private RimWorldTime _scheduleUpdateTime = new(0);

    private static TraitDef? NightOwlTrait =>
        _nightOwlTrait ??= DefDatabase<TraitDef>.GetNamedSilentFail("NightOwl");

    /// <summary>
    ///     Distributes the pawns of a group across shifts: evenly, with per-skill coverage,
    ///     keeping lovers/partners together.
    /// </summary>
    /// <param name="pawns">The pawns to assign.</param>
    /// <param name="shifts">The eligible shifts for the group.</param>
    /// <param name="relevantSkills">Skills considered for coverage.</param>
    /// <param name="maxLevels">Maximum colony level per skill, used for normalization.</param>
    private void AssignGroup(List<Pawn> pawns, List<WorkShift> shifts,
        List<SkillDef> relevantSkills, Dictionary<SkillDef, int> maxLevels)
    {
        if (pawns.Count == 0 || shifts.Count == 0) return;
        var shiftCount = shifts.Count;

        // Precompute skillScore (normalized level [0;1] + learning rate).
        var scores = new Dictionary<Pawn, Dictionary<SkillDef, float>>();
        foreach (var pawn in pawns)
        {
            var bySkill = new Dictionary<SkillDef, float>();
            foreach (var skill in relevantSkills)
            {
                var max = maxLevels[skill];
                var levelTerm = max > 0
                    ? LevelWeight * (pawn.skills.GetSkill(skill).Level / (float)max)
                    : 0f;
                var learnTerm = LearnRateCoef * pawn.skills.GetSkill(skill).LearnRateFactor();
                bySkill[skill] = levelTerm + learnTerm;
            }
            scores[pawn] = bySkill;
        }

        // Key pawns of a skill = top-K by skillScore (K = number of shifts).
        var keyPawns = new Dictionary<SkillDef, HashSet<Pawn>>();
        foreach (var skill in relevantSkills)
        {
            var top = pawns.OrderByDescending(pawn => scores[pawn][skill])
                .ThenBy(pawn => pawn.thingIDNumber).Take(shiftCount);
            keyPawns[skill] = [.. top];
        }

        // Specialists first (by max skillScore), deterministic tie-break.
        var ordered = pawns
            .OrderByDescending(pawn =>
                relevantSkills.Count == 0 ? 0f : relevantSkills.Max(skill => scores[pawn][skill]))
            .ThenBy(pawn => pawn.thingIDNumber).ToList();
        var workersByShift = new Dictionary<WorkShift, List<Pawn>>();
        var shiftCoverage = new Dictionary<WorkShift, HashSet<SkillDef>>();
        foreach (var shift in shifts)
        {
            shiftCoverage[shift] = [];
        }
        foreach (var pawn in ordered)
        {
            var lovers = GetLovers(pawn);
            var pawnKeySkills =
                relevantSkills.Where(skill => keyPawns[skill].Contains(pawn)).ToList();
            WorkShift? bestShift = null;
            var bestScore = float.MinValue;
            foreach (var shift in shifts)
            {
                workersByShift.TryGetValue(shift, out var shiftWorkers);
                var workerCount = shiftWorkers?.Count ?? 0;
                var loverCount = 0;
                if (shiftWorkers != null && lovers.Count > 0)
                    foreach (var worker in shiftWorkers)
                    {
                        if (lovers.Contains(worker))
                            loverCount++;
                    }
                var coverage = shiftCoverage[shift];
                var newCovered = 0;
                foreach (var skill in pawnKeySkills)
                {
                    if (!coverage.Contains(skill))
                        newCovered++;
                }
                var score = LoverShiftScoreBonus * loverCount + CoverageBonus * newCovered -
                            EvennessWeight * workerCount;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestShift = shift;
                }
            }
            if (bestShift == null)
            {
#if DEBUG
                Logger.LogMessage(
                    $"No eligible work shifts for {pawn.LabelShort}; skipping schedule assignment.");
#endif
                continue;
            }
            _workers.Add(pawn, bestShift);
            if (!workersByShift.TryGetValue(bestShift, out var assigned))
            {
                assigned = [];
                workersByShift[bestShift] = assigned;
            }
            assigned.Add(pawn);
            foreach (var skill in pawnKeySkills)
            {
                shiftCoverage[bestShift].Add(skill);
            }
        }
    }

    /// <summary>
    ///     Gets the lovers/partners of a pawn (for clustering them into the same shift).
    /// </summary>
    /// <param name="pawn">The pawn whose lovers to resolve.</param>
    /// <returns>The distinct lover/partner pawns, or an empty list.</returns>
    private static List<Pawn> GetLovers(Pawn pawn)
    {
        return pawn.relations?.DirectRelations.Where(relation => new[]
        {
            PawnRelationDefOf.Fiance, PawnRelationDefOf.Lover, PawnRelationDefOf.Spouse
        }.Contains(relation.def)).Select(relation => relation.otherPawn).Distinct().ToList() ?? [];
    }

    /// <summary>
    ///     Gets the skills relevant to active (visible) work types.
    /// </summary>
    /// <returns>The distinct relevant skills.</returns>
    private static List<SkillDef> GetRelevantSkills()
    {
        var skills = new HashSet<SkillDef>();
        var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
        for (var i = 0; i < workTypes.Count; i++)
        {
            var workType = workTypes[i];
            if (!workType.visible || workType.relevantSkills == null) continue;
            for (var j = 0; j < workType.relevantSkills.Count; j++)
            {
                skills.Add(workType.relevantSkills[j]);
            }
        }
        return [.. skills];
    }

    /// <summary>
    ///     Periodically updates pawn work schedules according to the configured update frequency.
    /// </summary>
    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (!WorkManagerMod.Settings.ManageWorkSchedule) return;
        if (!WorkManagerGameComponent.Instance.ScheduleManagementEnabled) return;
        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused ||
            (Find.TickManager.TicksGame & UpdateTickMask) != 0) return;
        var time = RimWorldTime.GetHomeTime();
        var hoursPassed = time - _scheduleUpdateTime;
        if (hoursPassed < 24f / WorkManagerMod.Settings.ScheduleUpdateFrequency) return;
        _scheduleUpdateTime = time;
        UpdateSchedule();
    }

    internal void UpdateNow()
    {
        if (!WorkManagerMod.Settings.ManageWorkSchedule ||
            !WorkManagerGameComponent.Instance.ScheduleManagementEnabled) return;
        _scheduleUpdateTime = RimWorldTime.GetHomeTime();
        UpdateSchedule();
    }

    private void UpdateSchedule()
    {
#if DEBUG
        Logger.LogMessage("Updating work schedule...");
#endif
        try
        {
            _workers.Clear();
            var allPawns = map.mapPawns.AllPawnsSpawned.Where(pawn =>
                pawn.Faction == Faction.OfPlayer && pawn.story != null && pawn.timetable != null &&
                pawn.skills != null && !pawn.RaceProps.IsMechanoid).ToList();

            // Skills relevant to active (visible) work types.
            var relevantSkills = GetRelevantSkills();

            // Maximum level of each skill across the colony (for normalization to [0;1]).
            var maxLevels = new Dictionary<SkillDef, int>();
            foreach (var skill in relevantSkills)
            {
                var max = 0;
                foreach (var pawn in allPawns)
                {
                    var level = pawn.skills.GetSkill(skill).Level;
                    if (level > max) max = level;
                }
                maxLevels[skill] = max;
            }
            var colonistCount = allPawns.Count(pawn => !pawn.story.traits.HasTrait(NightOwlTrait));
            var nightOwlCount = allPawns.Count - colonistCount;
            var scheduled = allPawns
                .Where(pawn => WorkManagerGameComponent.Instance.GetPawnScheduleEnabled(pawn))
                .ToList();

            // Two independent groups: regular colonists and night owls.
            AssignGroup(
                scheduled.Where(pawn => !pawn.story.traits.HasTrait(NightOwlTrait)).ToList(),
                WorkManagerMod.Settings.ColonistWorkShifts
                    .Where(shift => shift.PawnThreshold <= colonistCount).ToList(), relevantSkills,
                maxLevels);
            AssignGroup(scheduled.Where(pawn => pawn.story.traits.HasTrait(NightOwlTrait)).ToList(),
                WorkManagerMod.Settings.NightOwlWorkShifts
                    .Where(shift => shift.PawnThreshold <= nightOwlCount).ToList(), relevantSkills,
                maxLevels);
            foreach (var worker in _workers)
            {
                for (var hour = 0; hour < 24; hour++)
                {
                    worker.Key.timetable.SetAssignment(hour, worker.Value.GetTimeAssignment(hour));
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to update work schedule for map {map}.", e);
        }
    }
}