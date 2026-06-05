using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.Common.Helpers;
using LordKuper.WorkManager.Cache;
using LordKuper.WorkManager.Helpers;
using RimWorld;
using UnityEngine;
using Verse;
using PassionHelper = LordKuper.WorkManager.Helpers.PassionHelper;

namespace LordKuper.WorkManager;

/// <summary>
///     Manages and updates work priorities for pawns on a specific map.
/// </summary>
/// <remarks>
///     This class is responsible for dynamically assigning and updating work priorities for pawns based on
///     various factors such as skill levels, passions, learning rates, and predefined rules. It ensures that work
///     priorities are optimized and aligned with the current game state and configuration settings. The updates are
///     performed periodically and take into account both global and pawn-specific conditions.
///     <para>
///         <strong>Invariant:</strong> <see cref="WorkManagerGameComponent.Instance" /> is non-null for the
///         entire lifetime of this component. A <see cref="MapComponent" /> can only exist while a
///         <see cref="Map" /> exists, which requires an active <see cref="Game" />; the
///         <see cref="WorkManagerGameComponent" /> constructor runs when that game is created and sets
///         <c>Instance</c> before any map tick can fire. No null guard is required or expected here.
///     </para>
/// </remarks>
/// <param name="map"></param>
[UsedImplicitly]
public class WorkPriorityUpdater(Map map) : MapComponent(map)
{
    /// <summary>
    ///     Score penalty applied to a pawn for a work type considered dangerous by its ideology.
    /// </summary>
    private const float DangerousWorkScorePenalty = 50f;

    /// <summary>
    ///     Hours a pawn must remain idle before idle work assignment is cleared.
    /// </summary>
    private const float IdleHoursThreshold = 12f;

    /// <summary>
    ///     Bitmask gating updates to once every 64 game ticks.
    /// </summary>
    private const int UpdateTickMask = 0x3F;

    private readonly HashSet<Pawn> _allPawns = [];
    private readonly HashSet<PawnCache> _capablePawns = [];
    private readonly Dictionary<WorkTypeDef, WorkTypeAssignmentRule> _managedWorkTypeRules = [];
    private readonly Dictionary<Pawn, PawnCache> _pawnCache = [];
    private RimWorldTime _workUpdateTime = new(0);

    private static void AddActiveWork(Dictionary<PawnCache, HashSet<WorkTypeDef>> activeWorkMatrix,
        PawnCache pawnCache, WorkTypeDef workType)
    {
        if (!activeWorkMatrix.TryGetValue(pawnCache, out var activeSet))
        {
            activeSet = [];
            activeWorkMatrix[pawnCache] = activeSet;
        }
        activeSet.Add(workType);
    }

    /// <summary>
    ///     Applies the calculated work priorities to all managed pawns.
    /// </summary>
    /// <remarks>
    ///     Iterates through all managed pawns in the cache and sets their work priorities for each managed work type.
    ///     The priority value is retrieved from the pawn's cache and applied using
    ///     <see cref="WorkTypePriorityHelper.SetPriority" />.
    ///     Only work types that are managed for the pawn are updated.
    /// </remarks>
    private void ApplyWorkPriorities()
    {
        foreach (var pawnCache in _pawnCache.Values)
        {
            if (!pawnCache.IsManaged)
                continue;
#if DEBUG
            Logger.LogMessage(
                $"Applying work priorities for {pawnCache.Pawn.LabelShort}: {string.Join(", ", _managedWorkTypeRules.Keys.Where(wt => pawnCache.IsManagedWork(wt)).Select(wt => $"{wt.defName}={pawnCache.GetWorkPriority(wt)}"))}");
#endif
            foreach (var workType in _managedWorkTypeRules.Keys)
            {
                if (!pawnCache.IsManagedWork(workType))
                    continue;
                var priority = pawnCache.GetWorkPriority(workType);
                WorkTypePriorityHelper.SetPriority(pawnCache.Pawn, workType, priority);
            }
        }
    }

    /// <summary>
    ///     Repeatedly assigns the highest-scoring pawn as a dedicated worker for the rule's work type until the
    ///     target worker count is reached or no candidates remain.
    /// </summary>
    /// <param name="pawnScores">Candidate pawns mapped to their suitability scores. Assigned pawns are removed.</param>
    /// <param name="rule">The work type assignment rule being satisfied.</param>
    /// <param name="workerCount">The current number of assigned dedicated workers.</param>
    /// <param name="targetWorkersCount">The desired number of dedicated workers.</param>
    /// <returns>The updated worker count after assignments.</returns>
    private static int AssignBestDedicatedWorkers(Dictionary<PawnCache, float> pawnScores,
        WorkTypeAssignmentRule rule, int workerCount, int targetWorkersCount)
    {
        while (workerCount < targetWorkersCount && pawnScores.Count > 0)
        {
            PawnCache? bestWorker = null;
            var bestScore = float.MinValue;
            foreach (var pair in pawnScores)
            {
                if (bestWorker == null || pair.Value > bestScore ||
                    (Mathf.Approximately(pair.Value, bestScore) && pair.Key.Pawn.thingIDNumber <
                        bestWorker.Pawn.thingIDNumber))
                {
                    bestScore = pair.Value;
                    bestWorker = pair.Key;
                }
            }
            if (bestWorker == null) break;
            bestWorker.SetWorkPriority(rule.Def!, WorkManagerMod.Settings.DedicatedWorkerPriority);
            workerCount++;
            pawnScores.Remove(bestWorker);
        }
        return workerCount;
    }

    /// <summary>
    ///     Adds the highest-scoring workers on the clock at the given hour to the dedicated set until the
    ///     hour's target count is reached or no further candidates remain.
    /// </summary>
    /// <param name="pawnScores">Candidate pawns mapped to their suitability scores.</param>
    /// <param name="hour">The hour of the day (0-23) being covered.</param>
    /// <param name="targetWorkersCount">The number of dedicated workers wanted for the hour.</param>
    /// <param name="dedicatedSet">The accumulating set of dedicated workers (mutated).</param>
    /// <param name="alreadyPicked">Workers already counted toward the hour from a previous pass.</param>
    /// <returns>The number of workers covering the hour after this pass.</returns>
    private static int AssignBestDedicatedWorkersForHour(Dictionary<PawnCache, float> pawnScores,
        int hour, int targetWorkersCount, HashSet<PawnCache> dedicatedSet, int alreadyPicked = 0)
    {
        var candidates = new List<PawnCache>(pawnScores.Count);
        foreach (var pair in pawnScores)
        {
            if (pair.Key.IsWorkingHour(hour)) candidates.Add(pair.Key);
        }
        candidates.Sort((a, b) =>
        {
            var comparison = pawnScores[b].CompareTo(pawnScores[a]);
            return comparison != 0
                ? comparison
                : a.Pawn.thingIDNumber.CompareTo(b.Pawn.thingIDNumber);
        });
        var picked = alreadyPicked;
        for (var i = 0; i < candidates.Count && picked < targetWorkersCount; i++)
        {
            dedicatedSet.Add(candidates[i]);
            picked++;
        }
        return picked;
    }

    /// <summary>
    ///     Assigns common work priorities to managed pawns based on predefined work types and conditions.
    /// </summary>
    /// <remarks>
    ///     This method evaluates all managed pawns and assigns work priorities for specific work types
    ///     that are both relevant and compatible with the pawn's capabilities. Only pawns that are managed, capable, and
    ///     not recovering are considered. Work types that are disabled or deemed unsuitable for a pawn are excluded from
    ///     assignment.
    /// </remarks>
    private void AssignCommonWork()
    {
        // ReSharper disable once RedundantSuppressNullableWarningExpression : Instance is non-null on this game-scoped path by the Map=>Game lifecycle invariant (ADR-0001); the compiler still requires the null-forgiving operator on the nullable property read.
        var instance = WorkManagerGameComponent.Instance!;
#if DEBUG
        Logger.LogMessage(
            $"Assigning common work types ({string.Join(", ", instance.AssignEveryoneWorkTypes.Select(workType => $"{workType.Key.defName}[{workType.Value}]"))})");
#endif
        var relevantWorkTypes =
            new HashSet<WorkTypeDef>(instance.AssignEveryoneWorkTypes.Keys);
        relevantWorkTypes.IntersectWith(_managedWorkTypeRules.Keys);
        var priorities = instance.AssignEveryoneWorkTypes;
        foreach (var pawnCache in _pawnCache.Values)
        {
            if (!pawnCache.IsManaged || !pawnCache.IsCapable)
                continue;
            foreach (var workType in relevantWorkTypes)
            {
                if (!pawnCache.IsManagedWork(workType) || !pawnCache.IsAllowedWorker(workType) ||
                    pawnCache.IsBadWork(workType) || pawnCache.IsDangerousWork(workType))
                    continue;
                pawnCache.SetWorkPriority(workType, priorities[workType]);
            }
        }
    }

    /// <summary>
    ///     Assigns dedicated workers to specific work types based on predefined rules and priorities.
    /// </summary>
    /// <remarks>
    ///     This method evaluates the available workers and assigns them to work types that allow
    ///     dedicated workers, ensuring that the target number of workers is met for each applicable work type. The
    ///     assignment process considers worker suitability, existing assignments, and priority settings. If no suitable
    ///     workers are available, a fallback mechanism is used to assign less optimal workers.
    /// </remarks>
    private void AssignDedicatedWorkers()
    {
        if (_capablePawns.Count == 0) return;
#if DEBUG
        Logger.LogMessage("Assigning dedicated workers...");
#endif
        var relevantRules = new List<WorkTypeAssignmentRule>();
        foreach (var workType in WorkManagerGameComponent.Instance!.DedicatedWorkTypes)
        {
            if (_managedWorkTypeRules.TryGetValue(workType, out var rule)) relevantRules.Add(rule);
        }
        if (relevantRules.Count == 0) return;
        relevantRules.Sort(WorkManagerGameComponent.WorkTypeAssignmentRuleComparer);
        var useSchedule = WorkManagerMod.Settings.UseScheduleForDedicatedWorkers;
        foreach (var rule in relevantRules)
        {
            // Schedule-aware selection picks, for every working hour, the best-suited workers
            // who are actually on the clock that hour. It returns false only when no allowed
            // worker has any Work hour for this work type, in which case we fall back to the
            // legacy day-level selection so the work type is never left without dedicated workers.
            if (useSchedule && TryAssignDedicatedWorkersBySchedule(rule, relevantRules))
                continue;
            AssignDedicatedWorkersForDay(rule, relevantRules);
        }
    }

    /// <summary>
    ///     Assigns dedicated workers for a single rule using the legacy day-level selection:
    ///     picks the highest-scoring workers for the whole day, ignoring schedules.
    /// </summary>
    /// <param name="rule">The work type assignment rule being satisfied.</param>
    /// <param name="relevantRules">All dedicated rules, used for scoring (dedication counts).</param>
    private void AssignDedicatedWorkersForDay(WorkTypeAssignmentRule rule,
        List<WorkTypeAssignmentRule> relevantRules)
    {
        var def = rule.Def!;
        var targetWorkersCount =
            rule.GetTargetWorkersCount(map, _capablePawns.Count, relevantRules.Count);
        if (rule.EnsureWorkerAssigned == true)
            targetWorkersCount = Math.Max(targetWorkersCount, rule.MinWorkerNumber);
#if DEBUG
        Logger.LogMessage($"Target dedicated workers for {rule.Label} = {targetWorkersCount}");
        Logger.LogMessage(
            $"Allowed workers filter for {rule.Label}:\n{rule.AllowedWorkers!.GetSummary(0)}");
#endif
        var allowedWorkers = new List<PawnCache>(_capablePawns.Count);
        foreach (var pc in _capablePawns)
        {
            if (pc.IsAllowedWorker(def)) allowedWorkers.Add(pc);
        }
#if DEBUG
        Logger.LogMessage(
            $"Allowed workers for '{rule.Label}': {string.Join(", ", allowedWorkers.Select(pc => $"{pc.Pawn.LabelShort}"))}");
#endif
        if (allowedWorkers.Count == 0) return;
        var goodWorkers = new List<PawnCache>(allowedWorkers.Count);
        foreach (var pc in allowedWorkers)
        {
            if (!pc.IsBadWork(def) && !pc.IsDangerousWork(def)) goodWorkers.Add(pc);
        }
        var workerCount = 0;
        foreach (var pc in _capablePawns)
        {
            if (pc.IsActiveWork(def) && pc.GetWorkPriority(def) <=
                WorkManagerMod.Settings.DedicatedWorkerPriority)
                workerCount++;
        }
        if (workerCount >= targetWorkersCount) return;
        var pawnScores = GetDedicatedWorkersScores(goodWorkers, def, relevantRules);
        workerCount = AssignBestDedicatedWorkers(pawnScores, rule, workerCount, targetWorkersCount);
        if (workerCount >= targetWorkersCount) return;
        {
            var availableWorkers = new List<PawnCache>(_capablePawns.Count);
            foreach (var pc in _capablePawns)
            {
                if (!goodWorkers.Contains(pc)) availableWorkers.Add(pc);
            }
            pawnScores = GetDedicatedWorkersScores(availableWorkers, def, relevantRules);
            AssignBestDedicatedWorkers(pawnScores, rule, workerCount, targetWorkersCount);
        }
    }

    /// <summary>
    ///     Assigns unallocated work types to capable pawns based on their suitability and the current configuration
    ///     settings.
    /// </summary>
    private void AssignLeftoverWorkTypes()
    {
        if (_capablePawns.Count == 0) return;
#if DEBUG
        Logger.LogMessage("Assigning leftover work types...");
#endif
        var workTypes = _managedWorkTypeRules.Keys
            .Except(WorkManagerGameComponent.Instance!.AssignEveryoneWorkTypes.Keys).ToArray();
        if (!WorkManagerMod.Settings.UseDedicatedWorkers)
        {
            var activeWorkMatrix =
                new Dictionary<PawnCache, HashSet<WorkTypeDef>>(_capablePawns.Count);
            foreach (var pc in _capablePawns)
            {
                var activeSet = new HashSet<WorkTypeDef>();
                foreach (var wt in workTypes)
                {
                    if (pc.IsActiveWork(wt))
                        activeSet.Add(wt);
                }
                activeWorkMatrix[pc] = activeSet;
            }
            foreach (var workType in workTypes)
            {
                var hasActive = false;
                foreach (var pc in _capablePawns)
                {
                    if (!activeWorkMatrix[pc].Contains(workType)) continue;
                    hasActive = true;
                    break;
                }
                if (hasActive) continue;
                PawnCache? bestPc = null;
                var minActiveCount = int.MaxValue;
                foreach (var pc in _capablePawns)
                {
                    if (!pc.IsManaged || !pc.IsManagedWork(workType) ||
                        !pc.IsAllowedWorker(workType) || pc.IsBadWork(workType) ||
                        pc.IsDangerousWork(workType)) continue;
                    var activeCount = activeWorkMatrix[pc].Count;
                    if (activeCount > minActiveCount) continue;
                    if (activeCount == minActiveCount && bestPc != null &&
                        pc.Pawn.thingIDNumber >= bestPc.Pawn.thingIDNumber) continue;
                    minActiveCount = activeCount;
                    bestPc = pc;
                }
                if (bestPc == null) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {bestPc.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {WorkManagerMod.Settings.HighestSkillPriority}");
#endif
                bestPc.SetWorkPriority(workType, WorkManagerMod.Settings.HighestSkillPriority);
                AddActiveWork(activeWorkMatrix, bestPc, workType);
            }
            foreach (var pc in _capablePawns)
            {
                if (!pc.IsManaged || activeWorkMatrix[pc].Count != 0) continue;
                WorkTypeDef? bestWorkType = null;
                var minActiveCount = int.MaxValue;
                foreach (var wt in workTypes)
                {
                    if (!pc.IsManagedWork(wt) || !pc.IsAllowedWorker(wt) || pc.IsBadWork(wt) ||
                        pc.IsDangerousWork(wt)) continue;
                    var activeCount = 0;
                    foreach (var otherPc in _capablePawns)
                    {
                        if (activeWorkMatrix[otherPc].Contains(wt))
                            activeCount++;
                    }
                    if (activeCount > minActiveCount) continue;
                    if (activeCount == minActiveCount && bestWorkType != null &&
                        string.Compare(wt.defName, bestWorkType.defName,
                            StringComparison.Ordinal) >= 0) continue;
                    minActiveCount = activeCount;
                    bestWorkType = wt;
                }
                if (bestWorkType == null) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {pc.Pawn.LabelShort}'s priority of '{bestWorkType.labelShort}' to {WorkManagerMod.Settings.HighestSkillPriority}");
#endif
                pc.SetWorkPriority(bestWorkType, WorkManagerMod.Settings.HighestSkillPriority);
                AddActiveWork(activeWorkMatrix, pc, bestWorkType);
            }
        }
        if (WorkManagerMod.Settings.AssignAllWorkTypes)
            foreach (var pc in _capablePawns)
            {
                if (!pc.IsManaged) continue;
                foreach (var workType in workTypes)
                {
                    if (!pc.IsManagedWork(workType) || pc.IsBadWork(workType) ||
                        pc.IsDangerousWork(workType) || !pc.IsAllowedWorker(workType) ||
                        pc.IsActiveWork(workType)) continue;
#if DEBUG
                    Logger.LogMessage(
                        $"Setting {pc.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {WorkManagerMod.Settings.LeftoverPriority}");
#endif
                    pc.SetWorkPriority(workType, WorkManagerMod.Settings.LeftoverPriority);
                }
            }
    }

    /// <summary>
    ///     Assigns work priorities to pawns that are currently idle, based on managed work type rules and eligibility.
    /// </summary>
    /// <remarks>
    ///     This method identifies pawns that have been idle for a specified duration and assigns them
    ///     work priorities  for eligible work types. It ensures that only managed pawns and work types that meet specific
    ///     conditions  (e.g., allowed and not marked as bad work) are considered. If no idle pawns are found, the method
    ///     exits early.
    /// </remarks>
    private void AssignWorkToIdlePawns()
    {
#if DEBUG
        Logger.LogMessage("Assigning work to idle pawns...");
#endif
        List<PawnCache>? noLongerIdlePawns = null;
        foreach (var pc in _pawnCache.Values)
        {
            if (pc.IdleSince == null ||
                !(_workUpdateTime - pc.IdleSince.Value > IdleHoursThreshold)) continue;
            noLongerIdlePawns ??= [];
            pc.IdleSince = null;
            noLongerIdlePawns.Add(pc);
        }
#if DEBUG
        if (noLongerIdlePawns is { Count: > 0 })
            Logger.LogMessage(
                $"No longer idle pawns: {string.Join(", ", noLongerIdlePawns.Select(pc => pc.Pawn.LabelShort))}");
#endif
        List<PawnCache>? idlePawns = null;
        foreach (var pc in _capablePawns)
        {
            if (!pc.IsManaged ||
                (pc.IdleSince == null && (pc.Pawn.Drafted || !pc.Pawn.mindState.IsIdle))) continue;
            idlePawns ??= [];
            idlePawns.Add(pc);
        }
        if (idlePawns == null || idlePawns.Count == 0) return;
        var workTypes = new List<WorkTypeDef>();
        foreach (var wt in _managedWorkTypeRules.Keys)
        {
            if (!WorkManagerGameComponent.Instance!.AssignEveryoneWorkTypes.ContainsKey(wt))
                workTypes.Add(wt);
        }
        foreach (var pc in idlePawns)
        {
            foreach (var workType in workTypes)
            {
                if (!pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) ||
                    pc.IsBadWork(workType) || pc.IsDangerousWork(workType) ||
                    pc.IsActiveWork(workType)) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {pc.Pawn.LabelShort}'s priority of '{workType.defName}' to {WorkManagerMod.Settings.IdlePriority}");
#endif
                pc.SetWorkPriority(workType, WorkManagerMod.Settings.IdlePriority);
            }
            pc.IdleSince ??= _workUpdateTime;
        }
    }

    /// <summary>
    ///     Assigns work priorities to managed workers based on their learning rates for specific work types.
    /// </summary>
    /// <remarks>
    ///     This method evaluates each managed worker's learning rate for eligible work types and assigns
    ///     a priority  if the learning rate meets or exceeds predefined thresholds. Workers with higher learning rates are
    ///     assigned  higher priorities. Only workers and work types that meet specific conditions are considered for
    ///     assignment.
    /// </remarks>
    private void AssignWorkersByLearningRate()
    {
        if (_capablePawns.Count == 0) return;
#if DEBUG
        Logger.LogMessage("Assigning workers by learning rate...");
#endif
        var majorThreshold = WorkManagerMod.Settings.MajorLearningRateThreshold;
        var minorThreshold = WorkManagerMod.Settings.MinorLearningRateThreshold;
        var majorPriority = WorkManagerMod.Settings.MajorLearningRatePriority;
        var minorPriority = WorkManagerMod.Settings.MinorLearningRatePriority;
        foreach (var pc in _capablePawns)
        {
            if (!pc.IsManaged) continue;
            foreach (var workType in _managedWorkTypeRules.Keys)
            {
                if (WorkManagerGameComponent.Instance!.AssignEveryoneWorkTypes.ContainsKey(workType))
                    continue;
                if (!pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) ||
                    pc.IsBadWork(workType) || pc.IsDangerousWork(workType) ||
                    pc.IsActiveWork(workType))
                    continue;
                var learningRate = pc.GetLearningRate(workType);
                var priority = 0;
                if (learningRate >= majorThreshold)
                    priority = majorPriority;
                else if (learningRate >= minorThreshold) priority = minorPriority;
                if (priority > 0)
                {
#if DEBUG
                    Logger.LogMessage(
                        $"Setting {pc.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (learning rate = {learningRate:F2})");
#endif
                    pc.SetWorkPriority(workType, priority);
                }
            }
        }
    }

    /// <summary>
    ///     Assigns work priorities to managed workers based on their passions and predefined rules.
    /// </summary>
    /// <remarks>
    ///     This method iterates through all capable and managed workers, evaluating their suitability
    ///     for specific work types based on passion levels, work type rules, and other constraints. If a worker's passion
    ///     for a work type meets the criteria and a priority is defined for that passion, the worker's priority for the
    ///     work type is updated accordingly.
    /// </remarks>
    private void AssignWorkersByPassion()
    {
        if (_capablePawns.Count == 0) return;
#if DEBUG
        Logger.LogMessage("Assigning workers by passion...");
#endif
        foreach (var pc in _capablePawns)
        {
            if (!pc.IsManaged) continue;
            foreach (var workType in _managedWorkTypeRules.Keys)
            {
                if (WorkManagerGameComponent.Instance!.AssignEveryoneWorkTypes.ContainsKey(workType))
                    continue;
                if (!pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) ||
                    pc.IsBadWork(workType) || pc.IsDangerousWork(workType) ||
                    pc.IsActiveWork(workType))
                    continue;
                var passionCache =
                    Common.Helpers.PassionHelper.GetPassionCache(pc.GetWorkPassion(workType));
                if (passionCache == null) continue;
                if (!WorkManagerMod.Settings.PassionPriorities!.TryGetValue(passionCache.DefName,
                        out var priority))
                    continue;
                if (priority <= 0) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {pc.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (passion = {passionCache.Label})");
#endif
                pc.SetWorkPriority(workType, priority);
            }
        }
    }

    /// <summary>
    ///     Assigns workers to tasks based on their skill levels and predefined work type rules.
    /// </summary>
    /// <remarks>
    ///     This method evaluates the skills of available workers and assigns them to tasks where they
    ///     are most proficient,  following the rules defined in the managed work type configuration. Workers with higher
    ///     skill levels are prioritized,  and only those who meet the criteria for a specific task are considered. If no
    ///     workers or relevant rules are available,  the method exits without making any assignments.
    /// </remarks>
    private void AssignWorkersBySkill()
    {
        if (_capablePawns.Count == 0) return;
#if DEBUG
        Logger.LogMessage("Assigning workers by skill...");
#endif
        var relevantRules = new List<WorkTypeAssignmentRule>(_managedWorkTypeRules.Count);
        foreach (var rule in _managedWorkTypeRules.Values)
        {
            if (rule.Def!.relevantSkills is { Count: > 0 })
                relevantRules.Add(rule);
        }
        if (relevantRules.Count == 0) return;
        foreach (var rule in relevantRules)
        {
            var def = rule.Def!;
            var allowedWorkers = new List<PawnCache>(_capablePawns.Count);
            var maxSkillValue = int.MinValue;
            foreach (var pc in _capablePawns)
            {
                if (!pc.IsAllowedWorker(def)) continue;
                allowedWorkers.Add(pc);
                var skill = pc.GetWorkSkillLevel(def);
                if (skill > maxSkillValue) maxSkillValue = skill;
            }
#if DEBUG
            Logger.LogMessage(
                $"Allowed workers for '{rule.Label}': {string.Join(", ", allowedWorkers.Select(pc => $"{pc.Pawn.LabelShort} ({pc.GetWorkSkillLevel(def)})"))} (max skill = {maxSkillValue})");
#endif
            if (allowedWorkers.Count == 0) continue;
            var activeWorkerCount = 0;
            foreach (var pc in _capablePawns)
            {
                if (pc.IsActiveWork(def)) activeWorkerCount++;
            }
            allowedWorkers.Sort((a, b) =>
                b.GetWorkSkillLevel(def).CompareTo(a.GetWorkSkillLevel(def)));
            foreach (var pc in allowedWorkers)
            {
                if (!pc.IsManaged || !pc.IsManagedWork(def) || pc.IsBadWork(def) ||
                    pc.IsDangerousWork(def)) continue;
                var skill = pc.GetWorkSkillLevel(def);
                if (skill < maxSkillValue && activeWorkerCount != 0) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {pc.Pawn.LabelShort}'s priority of '{rule.Label}' to {WorkManagerMod.Settings.HighestSkillPriority} (skill = {skill}, max = {maxSkillValue})");
#endif
                pc.SetWorkPriority(def, WorkManagerMod.Settings.HighestSkillPriority);
                activeWorkerCount++;
            }
        }
    }

    /// <summary>
    ///     Calculates and returns a dictionary of scores for a collection of pawns, indicating their suitability as
    ///     dedicated workers for a specified work type based on various factors.
    /// </summary>
    /// <remarks>
    ///     The score for each pawn is calculated based on several factors, including:
    ///     <list
    ///         type="bullet">
    ///         <item>
    ///             <description>The pawn's skill level for the specified work type.</description>
    ///         </item>
    ///         <item>
    ///             <description>The pawn's passion for the work type.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The pawn's
    ///                 learning rate for the work type.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The number of other work types the pawn
    ///                 is dedicated to.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     Each factor is normalized and weighted according to predefined
    ///     settings, and the final score is computed as a weighted sum of these factors. Pawns that are not managed for the
    ///     specified work type are excluded from the results.
    /// </remarks>
    /// <param name="pawns">The collection of pawns to evaluate. Cannot be <see langword="null" />.</param>
    /// <param name="workType">The work type for which the scores are being calculated. Cannot be <see langword="null" />.</param>
    /// <param name="rules">
    ///     The collection of work type assignment rules to consider during evaluation. Cannot be
    ///     <see langword="null" />.
    /// </param>
    /// <returns>
    ///     A dictionary where each key is a <see cref="PawnCache" /> representing a pawn, and the value is a
    ///     <see
    ///         cref="float" />
    ///     representing the calculated score for that pawn. Higher scores indicate greater suitability as a
    ///     dedicated worker for the specified work type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="pawns" />, <paramref name="workType" />, or <paramref name="rules" /> is
    ///     <see
    ///         langword="null" />
    ///     .
    /// </exception>
    private static Dictionary<PawnCache, float> GetDedicatedWorkersScores(
        IReadOnlyCollection<PawnCache> pawns, WorkTypeDef workType,
        IReadOnlyCollection<WorkTypeAssignmentRule> rules)
    {
        if (pawns == null) throw new ArgumentNullException(nameof(pawns));
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        var count = pawns.Count;
        var pawnScores = new Dictionary<PawnCache, float>(count);
        var pawnSkills = new Dictionary<PawnCache, int>(count);
        var pawnDedicationsCounts = new Dictionary<PawnCache, int>(count);
        var pawnLearningRates = new Dictionary<PawnCache, float>(count);
        int skillMin = int.MaxValue, skillMax = int.MinValue;
        int dedicationsMin = int.MaxValue, dedicationsMax = int.MinValue;
        float learningMin = float.MaxValue, learningMax = float.MinValue;
        foreach (var pc in pawns)
        {
            var skill = pc.GetWorkSkillLevel(workType);
            pawnSkills[pc] = skill;
            if (skill < skillMin) skillMin = skill;
            if (skill > skillMax) skillMax = skill;
            var dedications = 0;
            foreach (var rule in rules)
            {
                if (pc.IsActiveWork(rule.Def!) && pc.GetWorkPriority(rule.Def!) <=
                    WorkManagerMod.Settings.DedicatedWorkerPriority)
                    dedications++;
            }
            pawnDedicationsCounts[pc] = dedications;
            if (dedications < dedicationsMin) dedicationsMin = dedications;
            if (dedications > dedicationsMax) dedicationsMax = dedications;
            var learning = pc.GetLearningRate(workType);
            pawnLearningRates[pc] = learning;
            if (learning < learningMin) learningMin = learning;
            if (learning > learningMax) learningMax = learning;
        }
        var skillRange = new FloatRange(skillMin, skillMax);
        var dedicationsRange = new FloatRange(dedicationsMin, dedicationsMax);
        var learningRange = new FloatRange(learningMin, learningMax);
        foreach (var pc in pawns)
        {
            if (!pc.IsManagedWork(workType)) continue;
            var normalizedSkill = MathHelper.NormalizeValue(pawnSkills[pc], skillRange);
            var normalizedPassion = PassionHelper.GetPassionScore(pc.GetWorkPassion(workType));
            var normalizedLearningRate =
                MathHelper.NormalizeValue(pawnLearningRates[pc], learningRange);
            var normalizedDedications =
                MathHelper.NormalizeValue(pawnDedicationsCounts[pc], dedicationsRange);
            var score = WorkManagerMod.Settings.DedicatedWorkerSkillScoreFactor * normalizedSkill +
                        WorkManagerMod.Settings.DedicatedWorkerPassionScoreFactor *
                        normalizedPassion +
                        WorkManagerMod.Settings.DedicatedWorkerLearningRateScoreFactor *
                        normalizedLearningRate -
                        WorkManagerMod.Settings.DedicatedWorkerWorkCountScoreFactor *
                        normalizedDedications;
            if (pc.IsDangerousWork(workType)) score -= DangerousWorkScorePenalty;
            pawnScores.Add(pc, score);
#if DEBUG
            Logger.LogMessage($"{workType.defName} score of {pc.Pawn.LabelShortCap} =" +
                              $" S({normalizedSkill:F1}[{skillRange.TrueMin:N0};{skillRange.TrueMax:N0}])*{WorkManagerMod.Settings.DedicatedWorkerSkillScoreFactor:F1}" +
                              $" + P({normalizedPassion:F1}*{WorkManagerMod.Settings.DedicatedWorkerPassionScoreFactor:F1})" +
                              $" + L({normalizedLearningRate:F1}[{learningRange.TrueMin:F2};{learningRange.TrueMax:F2}])*{WorkManagerMod.Settings.DedicatedWorkerLearningRateScoreFactor:F1}" +
                              $" - D({normalizedDedications:F1}[{dedicationsRange.TrueMin:N0};{dedicationsRange.TrueMax:N0}])*{WorkManagerMod.Settings.DedicatedWorkerWorkCountScoreFactor:F1}" +
                              $" = {score:F2}");
#endif
        }
        return pawnScores;
    }

    /// <summary>
    ///     Executes periodic updates for work priorities and related settings in the game.
    /// </summary>
    /// <remarks>
    ///     This method is called on each tick of the game and performs updates to work priorities  if
    ///     priority management is enabled and certain conditions are met. It ensures that work  priorities are updated at a
    ///     configurable frequency and applies the changes to all  player-controlled pawns. The method also enables work
    ///     priorities if they are disabled  in the game settings.  This method skips execution when the game paused or
    ///     when the current tick does not  align with the update interval. Additionally, it respects the configured update
    ///     frequency  to avoid unnecessary recalculations.
    /// </remarks>
    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (!WorkManagerGameComponent.Instance!.PriorityManagementEnabled) return;
        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused ||
            (Find.TickManager.TicksGame & UpdateTickMask) != 0) return;
        var time = RimWorldTime.GetHomeTime();
        var hoursPassed = time - _workUpdateTime;
        if (hoursPassed < WorkManagerMod.Settings.WorkPrioritiesUpdateFrequency *
            RimWorldTime.HoursInDay) return;
        _workUpdateTime = time;
        Update();
    }

    /// <summary>
    ///     Assigns dedicated workers for a single rule taking pawn schedules into account.
    /// </summary>
    /// <remarks>
    ///     For every hour of the day the target worker count is recomputed from the number of capable
    ///     pawns actually working that hour, and the best-scoring workers on the clock that hour are
    ///     selected. The union of those workers receives the (whole-day) dedicated worker priority, so
    ///     each working hour is covered by the most suitable workers available at that time. When fewer
    ///     than the target good workers are on the clock, allowed-but-bad workers of that hour are used
    ///     as a fail-safe.
    /// </remarks>
    /// <param name="rule">The work type assignment rule being satisfied.</param>
    /// <param name="relevantRules">All dedicated rules, used for scoring (dedication counts).</param>
    /// <returns>
    ///     <c>true</c> if the schedule-aware selection was applied (including when there are no allowed
    ///     workers); <c>false</c> when no allowed worker has any working hour, signalling the caller to
    ///     fall back to the legacy day-level selection.
    /// </returns>
    private bool TryAssignDedicatedWorkersBySchedule(WorkTypeAssignmentRule rule,
        List<WorkTypeAssignmentRule> relevantRules)
    {
        var def = rule.Def!;
        var allowedWorkers = new List<PawnCache>(_capablePawns.Count);
        foreach (var pc in _capablePawns)
        {
            if (pc.IsAllowedWorker(def)) allowedWorkers.Add(pc);
        }
        if (allowedWorkers.Count == 0) return true;
        var anyWorkHour = false;
        foreach (var pc in allowedWorkers)
        {
            for (var hour = 0; hour < 24; hour++)
            {
                if (!pc.IsWorkingHour(hour)) continue;
                anyWorkHour = true;
                break;
            }
            if (anyWorkHour) break;
        }
        if (!anyWorkHour) return false;
        var goodWorkers = new List<PawnCache>(allowedWorkers.Count);
        foreach (var pc in allowedWorkers)
        {
            if (!pc.IsBadWork(def) && !pc.IsDangerousWork(def)) goodWorkers.Add(pc);
        }
        var goodScores = GetDedicatedWorkersScores(goodWorkers, def, relevantRules);
        Dictionary<PawnCache, float>? availableScores = null;
        var dedicatedSet = new HashSet<PawnCache>();
        for (var hour = 0; hour < 24; hour++)
        {
            var capableAtHour = 0;
            foreach (var pc in _capablePawns)
            {
                if (pc.IsWorkingHour(hour)) capableAtHour++;
            }
            if (capableAtHour == 0) continue;
            var targetWorkersCount =
                rule.GetTargetWorkersCount(map, capableAtHour, relevantRules.Count);
            if (rule.EnsureWorkerAssigned == true)
                targetWorkersCount = Math.Max(targetWorkersCount, rule.MinWorkerNumber);
            if (targetWorkersCount <= 0) continue;
            var picked =
                AssignBestDedicatedWorkersForHour(goodScores, hour, targetWorkersCount,
                    dedicatedSet);
            if (picked < targetWorkersCount)
            {
                if (availableScores == null)
                {
                    var availableWorkers = new List<PawnCache>(allowedWorkers.Count);
                    foreach (var pc in allowedWorkers)
                    {
                        if (!goodWorkers.Contains(pc)) availableWorkers.Add(pc);
                    }
                    availableScores =
                        GetDedicatedWorkersScores(availableWorkers, def, relevantRules);
                }
                AssignBestDedicatedWorkersForHour(availableScores, hour, targetWorkersCount,
                    dedicatedSet, picked);
            }
        }
        foreach (var pc in dedicatedSet)
        {
            pc.SetWorkPriority(def, WorkManagerMod.Settings.DedicatedWorkerPriority);
        }
        return true;
    }

    /// <summary>
    ///     Updates the work priorities for all player-controlled pawns, ensuring that work priorities are enabled and
    ///     recalculated as necessary.
    /// </summary>
    /// <remarks>
    ///     This method ensures that the game's work priorities setting is enabled and notifies all
    ///     relevant pawns of the change. It then updates internal caches and recalculates work priorities to reflect the
    ///     current state.
    /// </remarks>
    internal void Update()
    {
        try
        {
            if (!Current.Game.playSettings.useWorkPriorities)
            {
                Current.Game.playSettings.useWorkPriorities = true;
                var pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
                var count = pawns.Count;
                for (var i = 0; i < count; i++)
                {
                    var pawn = pawns[i];
                    if (pawn.Faction == Faction.OfPlayer)
                        pawn.workSettings?.Notify_UseWorkPrioritiesChanged();
                }
            }
#if DEBUG
            Logger.LogMessage("Updating work priorities...");
#endif
            UpdateCache();
            UpdateWorkPriorities();
            ApplyWorkPriorities();
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to update work priorities for map {map}.", e);
        }
    }

    /// <summary>
    ///     Updates the internal cache of managed work types and pawn data for the current map.
    /// </summary>
    /// <remarks>
    ///     This method synchronizes the cache with the current state of the game, ensuring that:
    ///     <list
    ///         type="bullet">
    ///         <item>
    ///             <description>
    ///                 Only enabled work types are tracked in the managed work types
    ///                 list.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 All free colonists currently spawned on the map are included in
    ///                 the pawn list.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Stale entries in the pawn cache are removed, and new
    ///                 entries are added for any missing pawns.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     The cache is updated to reflect the latest
    ///     game state, and any necessary updates are applied to individual pawn caches.
    /// </remarks>
    private void UpdateCache()
    {
        _managedWorkTypeRules.Clear();
        // ReSharper disable once RedundantSuppressNullableWarningExpression : Instance is non-null on this game-scoped path by the Map=>Game lifecycle invariant (ADR-0001); the compiler still requires the null-forgiving operator on the nullable property read.
        var instance = WorkManagerGameComponent.Instance!;
        foreach (var rule in instance.CombinedRules)
        {
            if (instance.GetWorkTypeEnabled(rule.Def!))
                _managedWorkTypeRules.Add(rule.Def!, rule);
        }
        _allPawns.Clear();
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Faction != Faction.OfPlayer || pawn.workSettings is not { EverWork: true } ||
                pawn.skills == null || pawn.RaceProps.IsMechanoid)
                continue;
            _allPawns.Add(pawn);
        }
        var pawnCacheToRemove = new List<Pawn>();
        foreach (var pawn in _pawnCache.Keys)
        {
            if (!_allPawns.Contains(pawn))
                pawnCacheToRemove.Add(pawn);
        }
        foreach (var pawn in pawnCacheToRemove)
        {
            _pawnCache.Remove(pawn);
        }
        foreach (var pawn in _allPawns)
        {
            if (!_pawnCache.TryGetValue(pawn, out var cache))
            {
                cache = new PawnCache(pawn);
                _pawnCache.Add(pawn, cache);
            }
            cache.Update(_workUpdateTime);
        }
        _capablePawns.Clear();
        foreach (var pc in _pawnCache.Values)
        {
            if (pc.IsCapable) _capablePawns.Add(pc);
        }
#if DEBUG
        Logger.LogMessage(
            $"Capable pawns: {string.Join(", ", _capablePawns.Select(pc => $"{pc.Pawn.LabelShort} ({string.Join(", ", EnumHelper.GetUniqueFlags(PawnHelper.GetPawnHealthState(pc.Pawn)))})"))}");
#endif
    }

    /// <summary>
    ///     Updates the work priorities for all workers based on the current settings and configurations.
    /// </summary>
    /// <remarks>
    ///     This method adjusts work priorities by applying a series of rules and strategies, such as
    ///     assigning  common work, using dedicated workers, prioritizing by skill, passion, or learning rate, and handling
    ///     leftover work types. The behavior of this method is influenced by the settings in
    ///     <see
    ///         cref="WorkManagerMod.Settings" />
    ///     .
    /// </remarks>
    private void UpdateWorkPriorities()
    {
        AssignCommonWork();
        if (WorkManagerMod.Settings.UseDedicatedWorkers)
            AssignDedicatedWorkers();
        else
            AssignWorkersBySkill();
        if (WorkManagerMod.Settings.UsePassionPriorities)
            AssignWorkersByPassion();
        if (WorkManagerMod.Settings.UseLearningRatesPriorities)
            AssignWorkersByLearningRate();
        AssignLeftoverWorkTypes();
        if (WorkManagerMod.Settings.AssignWorkToIdlePawns) AssignWorkToIdlePawns();
    }
}