using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.Common.Helpers;
using LordKuper.WorkManager.Cache;
using LordKuper.WorkManager.Helpers;
using RimWorld;
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
/// </remarks>
/// <param name="map"></param>
[UsedImplicitly]
public class WorkPriorityUpdater(Map map) : MapComponent(map)
{
    private readonly HashSet<Pawn> _allPawns = [];
    private readonly HashSet<PawnCache> _capablePawns = [];
    private readonly Dictionary<WorkTypeDef, WorkTypeAssignmentRule> _managedWorkTypeRules = [];
    private readonly Dictionary<Pawn, PawnCache> _pawnCache = [];
    private RimWorldTime _workUpdateTime = new(0);

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
#if DEBUG
        Logger.LogMessage(
            $"Assigning common work types ({string.Join(", ", WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes.Select(workType => $"{workType.Key.defName}[{workType.Value}]"))})");
#endif
        var relevantWorkTypes =
            new HashSet<WorkTypeDef>(WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes.Keys);
        relevantWorkTypes.IntersectWith(_managedWorkTypeRules.Keys);
        var priorities = WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes;
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
        foreach (var workType in WorkManagerGameComponent.Instance.DedicatedWorkTypes)
        {
            relevantRules.Add(_managedWorkTypeRules[workType]);
        }
        if (relevantRules.Count == 0) return;
        relevantRules.Sort(WorkManagerGameComponent.WorkTypeAssignmentRuleComparer);
        foreach (var rule in relevantRules)
        {
            var targetWorkersCount = rule.GetTargetWorkersCount(map, _capablePawns.Count, relevantRules.Count);
            if (rule.EnsureWorkerAssigned == true)
                targetWorkersCount = Math.Max(targetWorkersCount, rule.MinWorkerNumber);
#if DEBUG
            Logger.LogMessage($"Target dedicated workers for {rule.Label} = {targetWorkersCount}");
#endif
            var allowedWorkers = new List<PawnCache>(_capablePawns.Count);
            foreach (var pc in _capablePawns)
            {
                if (pc.IsAllowedWorker(rule.Def)) allowedWorkers.Add(pc);
            }
            if (allowedWorkers.Count == 0) continue;
            var goodWorkers = new List<PawnCache>(allowedWorkers.Count);
            foreach (var pc in allowedWorkers)
            {
                if (!pc.IsBadWork(rule.Def) && !pc.IsDangerousWork(rule.Def)) goodWorkers.Add(pc);
            }
            var workerCount = 0;
            foreach (var pc in _capablePawns)
            {
                if (pc.IsActiveWork(rule.Def) &&
                    pc.GetWorkPriority(rule.Def) <= WorkManagerMod.Settings.DedicatedWorkerPriority)
                    workerCount++;
            }
            if (workerCount >= targetWorkersCount) continue;
            var pawnScores = GetDedicatedWorkersScores(goodWorkers, rule.Def, relevantRules);
            while (workerCount < targetWorkersCount && pawnScores.Count > 0)
            {
                PawnCache bestWorker = null;
                var bestScore = float.MinValue;
                foreach (var pair in pawnScores)
                {
                    if (pair.Value > bestScore)
                    {
                        bestScore = pair.Value;
                        bestWorker = pair.Key;
                    }
                }
                if (bestWorker == null) break;
#if DEBUG
                Logger.LogMessage($"Assigning '{bestWorker.Pawn.LabelShort}' as dedicated worker for '{rule.Label}'");
#endif
                bestWorker.SetWorkPriority(rule.Def, WorkManagerMod.Settings.DedicatedWorkerPriority);
                workerCount++;
                pawnScores.Remove(bestWorker);
            }
            if (workerCount >= targetWorkersCount) continue;
            {
                var availableWorkers = new List<PawnCache>(_capablePawns.Count);
                foreach (var pc in _capablePawns)
                {
                    if (!goodWorkers.Contains(pc)) availableWorkers.Add(pc);
                }
                pawnScores = GetDedicatedWorkersScores(availableWorkers, rule.Def, relevantRules);
                while (workerCount < targetWorkersCount && pawnScores.Count > 0)
                {
                    PawnCache bestWorker = null;
                    var bestScore = float.MinValue;
                    foreach (var pair in pawnScores)
                    {
                        if (pair.Value > bestScore)
                        {
                            bestScore = pair.Value;
                            bestWorker = pair.Key;
                        }
                    }
                    if (bestWorker == null) break;
#if DEBUG
                    Logger.LogMessage(
                        $"Assigning '{bestWorker.Pawn.LabelShort}' as dedicated worker for '{rule.Label}' (fail-safe)");
#endif
                    bestWorker.SetWorkPriority(rule.Def, WorkManagerMod.Settings.DedicatedWorkerPriority);
                    workerCount++;
                    pawnScores.Remove(bestWorker);
                }
            }
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
            .Except(WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes.Keys).ToArray();
        if (!WorkManagerMod.Settings.UseDedicatedWorkers)
        {
            var activeWorkMatrix = new Dictionary<PawnCache, HashSet<WorkTypeDef>>(_capablePawns.Count);
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
                PawnCache bestPc = null;
                var minActiveCount = int.MaxValue;
                foreach (var pc in _capablePawns)
                {
                    if (!pc.IsManaged || !pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) ||
                        pc.IsBadWork(workType) || pc.IsDangerousWork(workType)) continue;
                    var activeCount = activeWorkMatrix[pc].Count;
                    if (activeCount >= minActiveCount) continue;
                    minActiveCount = activeCount;
                    bestPc = pc;
                }
                if (bestPc == null) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {bestPc.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {WorkManagerMod.Settings.HighestSkillPriority}");
#endif
                bestPc.SetWorkPriority(workType, WorkManagerMod.Settings.HighestSkillPriority);
                break;
            }
            foreach (var pc in _capablePawns)
            {
                if (!pc.IsManaged || activeWorkMatrix[pc].Count != 0) continue;
                WorkTypeDef bestWorkType = null;
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
                    if (activeCount >= minActiveCount) continue;
                    minActiveCount = activeCount;
                    bestWorkType = wt;
                }
                if (bestWorkType == null) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {pc.Pawn.LabelShort}'s priority of '{bestWorkType.labelShort}' to {WorkManagerMod.Settings.HighestSkillPriority}");
#endif
                pc.SetWorkPriority(bestWorkType, WorkManagerMod.Settings.HighestSkillPriority);
            }
        }
        if (WorkManagerMod.Settings.AssignAllWorkTypes)
            foreach (var pc in _capablePawns)
            {
                if (!pc.IsManaged) continue;
                foreach (var workType in workTypes)
                {
                    if (!pc.IsManagedWork(workType) || pc.IsBadWork(workType) || pc.IsDangerousWork(workType) ||
                        !pc.IsAllowedWorker(workType) || pc.IsActiveWork(workType)) continue;
#if DEBUG
                    Logger.LogMessage(
                        $"Setting {pc.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {WorkManagerMod.Settings.LeftoverPriority}");
#endif
                    pc.SetWorkPriority(workType, WorkManagerMod.Settings.LeftoverPriority);
                }
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
            foreach (var workType in _managedWorkTypeRules.Keys.ToArray())
            {
                if (WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes.ContainsKey(workType)) continue;
                if (!pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) || pc.IsBadWork(workType) ||
                    pc.IsDangerousWork(workType) || pc.IsActiveWork(workType))
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
    /// <exception cref="NullReferenceException">
    ///     Thrown if a work type rule or passion cache is null during the assignment
    ///     process.
    /// </exception>
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
                if (WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes.ContainsKey(workType)) continue;
                if (!pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) || pc.IsBadWork(workType) ||
                    pc.IsDangerousWork(workType) || pc.IsActiveWork(workType))
                    continue;
                var passionCache = Common.Helpers.PassionHelper.GetPassionCache(pc.GetWorkPassion(workType)) ??
                                   throw new NullReferenceException("Passion cache is null.");
                var priority = WorkManagerMod.Settings.PassionPriorities[passionCache.DefName];
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
            if (rule.Def.relevantSkills is { Count: > 0 })
                relevantRules.Add(rule);
        }
        if (relevantRules.Count == 0) return;
        foreach (var rule in relevantRules)
        {
            var allowedWorkers = new List<PawnCache>(_capablePawns.Count);
            var maxSkillValue = int.MinValue;
            foreach (var pc in _capablePawns)
            {
                if (!pc.IsAllowedWorker(rule.Def)) continue;
                allowedWorkers.Add(pc);
                var skill = pc.GetWorkSkillLevel(rule.Def);
                if (skill > maxSkillValue) maxSkillValue = skill;
            }
#if DEBUG
            Logger.LogMessage(
                $"Allowed workers for '{rule.Label}': {string.Join(", ", allowedWorkers.Select(pc => $"{pc.Pawn.LabelShort} ({pc.GetWorkSkillLevel(rule.Def)})"))} (max skill = {maxSkillValue})");
#endif
            if (allowedWorkers.Count == 0) continue;
            var activeWorkerCount = 0;
            foreach (var pc in _capablePawns)
            {
                if (pc.IsActiveWork(rule.Def)) activeWorkerCount++;
            }
            allowedWorkers.Sort((a, b) => b.GetWorkSkillLevel(rule.Def).CompareTo(a.GetWorkSkillLevel(rule.Def)));
            foreach (var pc in allowedWorkers)
            {
                if (!pc.IsManaged || !pc.IsManagedWork(rule.Def) || pc.IsBadWork(rule.Def) ||
                    pc.IsDangerousWork(rule.Def)) continue;
                var skill = pc.GetWorkSkillLevel(rule.Def);
                if (skill < maxSkillValue && activeWorkerCount != 0) continue;
#if DEBUG
                Logger.LogMessage(
                    $"Setting {pc.Pawn.LabelShort}'s priority of '{rule.Label}' to {WorkManagerMod.Settings.HighestSkillPriority} (skill = {skill}, max = {maxSkillValue})");
#endif
                pc.SetWorkPriority(rule.Def, WorkManagerMod.Settings.HighestSkillPriority);
                activeWorkerCount++;
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
        List<PawnCache> noLongerIdlePawns = null;
        foreach (var pc in _pawnCache.Values)
        {
            if (pc.IdleSince == null || !(_workUpdateTime - pc.IdleSince.Value > 12)) continue;
            noLongerIdlePawns ??= [];
            pc.IdleSince = null;
            noLongerIdlePawns.Add(pc);
        }
#if DEBUG
        if (noLongerIdlePawns is { Count: > 0 })
            Logger.LogMessage(
                $"No longer idle pawns: {string.Join(", ", noLongerIdlePawns.Select(pc => pc.Pawn.LabelShort))}");
#endif
        List<PawnCache> idlePawns = null;
        foreach (var pc in _capablePawns)
        {
            if (!pc.IsManaged || (pc.IdleSince == null && (pc.Pawn.Drafted || !pc.Pawn.mindState.IsIdle))) continue;
            idlePawns ??= [];
            idlePawns.Add(pc);
        }
        if (idlePawns == null || idlePawns.Count == 0) return;
        var workTypes = new List<WorkTypeDef>();
        foreach (var wt in _managedWorkTypeRules.Keys)
        {
            if (!WorkManagerGameComponent.Instance.AssignEveryoneWorkTypes.ContainsKey(wt))
                workTypes.Add(wt);
        }
        foreach (var pc in idlePawns)
        {
            foreach (var workType in workTypes)
            {
                if (!pc.IsManagedWork(workType) || !pc.IsAllowedWorker(workType) || pc.IsBadWork(workType) ||
                    pc.IsDangerousWork(workType) || pc.IsActiveWork(workType)) continue;
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
    [NotNull]
    private static Dictionary<PawnCache, float> GetDedicatedWorkersScores(
        [NotNull] IReadOnlyCollection<PawnCache> pawns, [NotNull] WorkTypeDef workType,
        [NotNull] IReadOnlyCollection<WorkTypeAssignmentRule> rules)
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
                if (pc.IsActiveWork(rule.Def) &&
                    pc.GetWorkPriority(rule.Def) <= WorkManagerMod.Settings.DedicatedWorkerPriority)
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
            var normalizedLearningRate = MathHelper.NormalizeValue(pawnLearningRates[pc], learningRange);
            var normalizedDedications = MathHelper.NormalizeValue(pawnDedicationsCounts[pc], dedicationsRange);
            var score = WorkManagerMod.Settings.DedicatedWorkerSkillScoreFactor * normalizedSkill +
                        WorkManagerMod.Settings.DedicatedWorkerPassionScoreFactor * normalizedPassion +
                        WorkManagerMod.Settings.DedicatedWorkerLearningRateScoreFactor * normalizedLearningRate -
                        WorkManagerMod.Settings.DedicatedWorkerWorkCountScoreFactor * normalizedDedications;
            if (pc.IsDangerousWork(workType)) score -= 50f;
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
        if (!WorkManagerGameComponent.Instance.PriorityManagementEnabled) return;
        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused || (Find.TickManager.TicksGame & 0x3F) != 0) return;
        var time = RimWorldTime.GetHomeTime();
        var hoursPassed = time - _workUpdateTime;
        if (hoursPassed < WorkManagerMod.Settings.WorkPrioritiesUpdateFrequency * RimWorldTime.HoursInDay) return;
        if (!Current.Game.playSettings.useWorkPriorities)
        {
            Current.Game.playSettings.useWorkPriorities = true;
            var pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive;
            var count = pawns.Count;
            for (var i = 0; i < count; i++)
            {
                var pawn = pawns[i];
                if (pawn.Faction == Faction.OfPlayer) pawn.workSettings?.Notify_UseWorkPrioritiesChanged();
            }
        }
#if DEBUG
        Logger.LogMessage($"Updating work priorities... ({time}, passed = {hoursPassed:N1})");
#endif
        _workUpdateTime = time;
        UpdateCache();
        UpdateWorkPriorities();
        ApplyWorkPriorities();
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
        foreach (var rule in WorkManagerGameComponent.Instance.CombinedRules)
        {
            if (WorkManagerGameComponent.Instance.GetWorkTypeEnabled(rule.Def))
                _managedWorkTypeRules.Add(rule.Def, rule);
        }
        _allPawns.Clear();
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
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