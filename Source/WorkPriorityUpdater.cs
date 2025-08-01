using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.WorkManager.Helpers;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager
{
    [UsedImplicitly]
    public class WorkPriorityUpdater : MapComponent
    {
        private readonly HashSet<Pawn> _allPawns = new HashSet<Pawn>();
        private readonly HashSet<WorkTypeDef> _allWorkTypes = new HashSet<WorkTypeDef>();
        private readonly HashSet<WorkTypeDef> _managedWorkTypes = new HashSet<WorkTypeDef>();
        private readonly Dictionary<Pawn, PawnCache> _pawnCache = new Dictionary<Pawn, PawnCache>();
        private RimWorldTime _workUpdateTime = new RimWorldTime(0);
        public WorkPriorityUpdater(Map map) : base(map) { }

        private static WorkManagerGameComponent WorkManager { get; } =
            Current.Game.GetComponent<WorkManagerGameComponent>();

        private void ApplyWorkPriorities()
        {
            foreach (var pawnCache in _pawnCache.Values.Where(pc => pc.IsManaged))
            {
                foreach (var workType in _managedWorkTypes.Where(workType => pawnCache.IsManagedWork(workType)))
                {
                    var priority = pawnCache.WorkPriorities[workType];
                    WorkTypePriorityHelper.SetPriority(pawnCache.Pawn, workType, priority);
                }
            }
        }

        private void AssignCommonWork()
        {
#if DEBUG
            Logger.LogMessage(
                $"Assigning common work types ({string.Join(", ", Settings.Settings.AssignEveryoneWorkTypes.Select(workType => $"{workType.Label}[{workType.Priority}]"))}) --");
#endif
            var relevantWorkTypes = Settings.Settings.AssignEveryoneWorkTypes
                .Where(workType => workType.IsWorkTypeLoaded).Select(wt => wt.WorkTypeDef).Intersect(_managedWorkTypes);
            foreach (var workType in relevantWorkTypes)
            {
                foreach (var pawnCache in _pawnCache.Values.Where(pc =>
                             pc.IsManaged && pc.IsCapable && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                             !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType)))
                {
                    pawnCache.WorkPriorities[workType] = Settings.Settings.AssignEveryoneWorkTypes
                        .First(wt => wt.WorkTypeDef == workType).Priority;
                }
            }
        }

        private void AssignDedicatedWorkers()
        {
            var capablePawns = _pawnCache.Values.Where(pc => pc.IsCapable).ToList();
            if (!capablePawns.Any())
            {
                return;
            }
#if DEBUG
            Logger.LogMessage("Assigning dedicated workers...");
#endif
            var workTypes = _allWorkTypes.Intersect(_managedWorkTypes).Where(wt =>
                Settings.Settings.AssignEveryoneWorkTypes.FirstOrDefault(a => a.WorkTypeDef == wt)?.AllowDedicated ??
                true).ToList();
            if (Settings.Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!workTypes.Any())
            {
                return;
            }
            var targetWorkers = (int)Math.Ceiling((float)capablePawns.Count / workTypes.Count);
#if DEBUG
            Logger.LogMessage($"Target dedicated workers by work type = {targetWorkers}");
#endif
            foreach (var workType in workTypes.OrderByDescending(wt => wt.relevantSkills.Count)
                         .ThenByDescending(wt => wt.naturalPriority))
            {
                var relevantPawns = capablePawns.Where(pc =>
                    !pc.IsRecovering && !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType)).ToList();
                if (!relevantPawns.Any())
                {
                    continue;
                }
                var dedicatedWorkers = relevantPawns.Where(pc =>
                    pc.IsActiveWork(workType) &&
                    pc.WorkPriorities[workType] <= Settings.Settings.DedicatedWorkerPriority);
                if (dedicatedWorkers.Count() >= targetWorkers)
                {
                    continue;
                }
                var pawnSkills = relevantPawns.ToDictionary(pc => pc, pc => pc.GetWorkSkillLevel(workType));
                var maxSkill = pawnSkills.Max(pair => pair.Value);
                var minSkill = pawnSkills.Min(pair => pair.Value);
                var skillRange = maxSkill - minSkill;
                var pawnDedicationsCounts = relevantPawns.ToDictionary(pc => pc,
                    pc => workTypes.Count(wt =>
                        pc.IsActiveWork(wt) && pc.WorkPriorities[wt] <= Settings.Settings.DedicatedWorkerPriority));
                var maxDedications = pawnDedicationsCounts.Max(pair => pair.Value);
                var minDedications = pawnDedicationsCounts.Min(pair => pair.Value);
                var dedicationsCountRange = maxDedications - minDedications;
                var pawnScores = new Dictionary<PawnCache, float>();
                foreach (var pawnCache in relevantPawns.Where(pc => pc.IsManagedWork(workType)))
                {
                    var skill = pawnSkills[pawnCache];
                    var passion = pawnCache.GetPassion(workType);
                    var normalizedSkill = skillRange == 0f ? 0f : (float)skill / skillRange;
                    var normalizedPassion = passion == Passion.Major ? 1f : passion == Passion.Minor ? 0.5f : 0f;
                    var normalizedLearnRate = Settings.Settings.UseLearningRates
                        ? pawnCache.IsLearningRateAboveThreshold(workType, true) ? 1f :
                        pawnCache.IsLearningRateAboveThreshold(workType, false) ? 0.5f : 0f
                        : 0f;
                    var normalizedDedications = dedicationsCountRange == 0
                        ? 0f
                        : (float)pawnDedicationsCounts[pawnCache] / dedicationsCountRange;
                    var score = normalizedSkill - 0.5f * normalizedDedications;
                    if (Settings.Settings.UseLearningRates)
                    {
                        score += 0.2f * normalizedPassion;
                        score += 1.5f * normalizedLearnRate;
                    }
                    else
                    {
                        score += 1.5f * normalizedPassion;
                    }
                    pawnScores.Add(pawnCache, score);
                }
#if DEBUG
                Logger.LogMessage(
                    $"Skill range = {skillRange} [{minSkill};{maxSkill}]. Dedication range = {dedicationsCountRange} [{minDedications}; {maxDedications}]");
                Logger.LogMessage(
                    $"{string.Join(", ", pawnScores.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key.Pawn.LabelShort}({pair.Value:N2})"))}");
#endif
                while (capablePawns.Count(pc =>
                           pc.IsActiveWork(workType) &&
                           pc.WorkPriorities[workType] <= Settings.Settings.DedicatedWorkerPriority) < targetWorkers)
                {
                    var dedicatedWorker = pawnScores.Any()
                        ? pawnScores.OrderByDescending(pair => pair.Value).First().Key
                        : null;
                    if (dedicatedWorker == null)
                    {
                        break;
                    }
#if DEBUG
                    Logger.LogMessage(
                        $"Assigning '{dedicatedWorker.Pawn.LabelShort}' as dedicated worker for '{workType.labelShort}'");
#endif
                    dedicatedWorker.WorkPriorities[workType] = Settings.Settings.DedicatedWorkerPriority;
                    pawnScores.Remove(dedicatedWorker);
                }
            }
        }

        private void AssignDoctors()
        {
            if (!Settings.Settings.SpecialRulesForDoctors)
            {
                return;
            }
            var workType = _allWorkTypes.FirstOrDefault(workTypeDef =>
                "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null)
            {
                return;
            }
            if (!WorkManager.GetWorkTypeEnabled(workType))
            {
                return;
            }
#if DEBUG
            Logger.LogMessage("Assigning doctors...");
#endif
            var relevantPawns = _pawnCache.Values.Where(pc => pc.IsCapable && !pc.IsDisabledWork(workType)).ToList();
            if (!relevantPawns.Any())
            {
                return;
            }
            var assignedDoctors = relevantPawns.Where(pc => pc.IsActiveWork(workType)).ToList();
            var maxSkillValue = relevantPawns.Max(pc => pc.GetWorkSkillLevel(workType));
#if DEBUG
            Logger.LogMessage($"Max doctoring skill value = '{maxSkillValue}'");
#endif
            var assignEveryone =
                Settings.Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            var managedPawns = relevantPawns.Where(pc => pc.IsManaged && pc.IsManagedWork(workType))
                .OrderBy(pc => pc.IsBadWork(workType)).ThenByDescending(pc => pc.GetWorkSkillLevel(workType)).ToList();
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var pawnCache in managedPawns.Where(pc => !pc.IsRecovering))
                {
                    if (pawnCache.GetWorkSkillLevel(workType) >= maxSkillValue)
                    {
                        if (assignedDoctors.Count == 0 || !pawnCache.IsBadWork(workType))
                        {
#if DEBUG
                            Logger.LogMessage(
                                $"Assigning '{pawnCache.Pawn.LabelShort}' as primary doctor (highest skill value)");
#endif
                            pawnCache.WorkPriorities[workType] = Settings.Settings.DoctoringPriority;
                            assignedDoctors.Add(pawnCache);
                            continue;
                        }
                    }
                    if (assignedDoctors.Count == 0)
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Assigning '{pawnCache.Pawn.LabelShort}' as primary doctor (highest skill value)");
#endif
                        pawnCache.WorkPriorities[workType] = Settings.Settings.DoctoringPriority;
                        assignedDoctors.Add(pawnCache);
                        break;
                    }
                }
            }
            if (assignedDoctors.Count == 0)
            {
                var pawnCache = managedPawns.FirstOrDefault();
                if (pawnCache != null)
                {
#if DEBUG
                    Logger.LogMessage($"Assigning '{pawnCache.Pawn.LabelShort}' as primary doctor (fail-safe)");
#endif
                    pawnCache.WorkPriorities[workType] = assignEveryone == null || assignEveryone.AllowDedicated
                        ? Settings.Settings.DoctoringPriority
                        : assignEveryone.Priority;
                    assignedDoctors.Add(pawnCache);
                }
            }
            if (assignedDoctors.Count == 1)
            {
                var doctor = assignedDoctors.First();
                if (doctor.Pawn.health.HasHediffsNeedingTend() || doctor.Pawn.health.hediffSet.HasTendableHediff())
                {
                    foreach (var pawnCache in relevantPawns
                                 .Where(pc =>
                                     pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                                     !pc.IsActiveWork(workType)).OrderByDescending(pc => pc.GetWorkSkillLevel(workType))
                                 .ThenBy(pc => pc.IsBadWork(workType)))
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Assigning '{pawnCache.Pawn.LabelShort}' as secondary doctor (primary doctor needs tending)");
#endif
                        pawnCache.WorkPriorities[workType] = assignEveryone == null || assignEveryone.AllowDedicated
                            ? Settings.Settings.DoctoringPriority
                            : assignEveryone.Priority;
                        assignedDoctors.Add(pawnCache);
                        break;
                    }
                }
            }
            if (Settings.Settings.AssignMultipleDoctors && (assignEveryone == null || assignEveryone.AllowDedicated))
            {
                var patients = new List<Pawn>();
                if (Settings.Settings.CountDownedColonists)
                {
                    patients.AddRange(_allPawns.Where(pawn => pawn.Downed));
                }
                if (Settings.Settings.CountDownedGuests && map != null && map.IsPlayerHome)
                {
                    patients.AddRange(map.mapPawns.AllPawnsSpawned.Where(pawn =>
                        pawn?.guest != null && !pawn.IsColonist && !pawn.guest.IsPrisoner && !pawn.IsPrisoner &&
                        (pawn.Downed || pawn.health.HasHediffsNeedingTend() ||
                         pawn.health.hediffSet.HasTendableHediff())));
                }
                if (Settings.Settings.CountDownedPrisoners && map != null && map.IsPlayerHome)
                {
                    patients.AddRange(map.mapPawns.PrisonersOfColonySpawned.Where(pawn =>
                        pawn.Downed || pawn.health.HasHediffsNeedingTend() ||
                        pawn.health.hediffSet.HasTendableHediff()));
                }
                if (Settings.Settings.CountDownedAnimals && map != null && map.IsPlayerHome)
                {
                    patients.AddRange(map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal)
                        .Where(pawn => pawn.Downed || pawn.health.HasHediffsNeedingTend() ||
                                       pawn.health.hediffSet.HasTendableHediff()));
                }
#if DEBUG
                Logger.LogMessage(
                    $"Patient count = '{patients.Count}' ({string.Join(", ", patients.Select(pawn => pawn.LabelShort))})");
#endif
                while (assignedDoctors.Count < patients.Count)
                {
                    var pawnCache = relevantPawns
                        .Where(pc =>
                            pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                            !pc.IsActiveWork(workType)).OrderBy(pc => pc.IsBadWork(workType))
                        .ThenByDescending(pc => pc.GetWorkSkillLevel(workType)).FirstOrDefault();
                    if (pawnCache == null)
                    {
                        break;
                    }
#if DEBUG
                    Logger.LogMessage($"Assigning '{pawnCache.Pawn.LabelShort}' as backup doctor (multiple patients)");
#endif
                    pawnCache.WorkPriorities[workType] = Settings.Settings.DoctoringPriority;
                    assignedDoctors.Add(pawnCache);
                }
            }
        }

        private void AssignHunters()
        {
            if (!Settings.Settings.SpecialRulesForHunters)
            {
                return;
            }
            var workType = _allWorkTypes.FirstOrDefault(workTypeDef =>
                "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null)
            {
                return;
            }
            if (!_managedWorkTypes.Contains(workType))
            {
                return;
            }
#if DEBUG
            Logger.LogMessage("Assigning hunters...");
#endif
            var assignEveryone =
                Settings.Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            if (assignEveryone != null)
            {
                foreach (var pawnCache in _pawnCache.Values.Where(pc =>
                             pc.IsCapable && pc.IsManaged && pc.IsManagedWork(workType) && pc.IsActiveWork(workType) &&
                             !pc.IsHunter()))
                {
#if DEBUG
                    Logger.LogMessage($"Removing hunting assignment from '{pawnCache.Pawn.LabelShort}' (not a hunter)");
#endif
                    pawnCache.WorkPriorities[workType] = 0;
                }
            }
            var hunters = _pawnCache.Values.Where(pc => pc.IsCapable && (pc.IsHunter() || pc.IsActiveWork(workType)))
                .ToList();
            var maxSkillValue = hunters.Any() ? hunters.Max(pc => pc.GetWorkSkillLevel(workType)) : 0;
#if DEBUG
            Logger.LogMessage(
                $"Hunters are {string.Join(", ", hunters.Select(pc => $"{pc.Pawn.LabelShortCap} ({pc.GetWorkSkillLevel(workType):N2})"))}");
            Logger.LogMessage($"Max hunting skill value = '{maxSkillValue}'");
#endif
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var pawnCache in hunters
                             .Where(pc =>
                                 pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                                 !pc.IsBadWork(workType)).OrderByDescending(pc => pc.GetWorkSkillLevel(workType)))
                {
                    if (pawnCache.GetWorkSkillLevel(workType) >= maxSkillValue ||
                        _pawnCache.Values.Count(pc => pc.IsCapable && pc.IsActiveWork(workType)) == 0)
                    {
                        pawnCache.WorkPriorities[workType] = Settings.Settings.UseDedicatedWorkers
                            ? Settings.Settings.DedicatedWorkerPriority
                            : Settings.Settings.HighestSkillPriority;
                    }
                    else
                    {
                        if (Settings.Settings.UseLearningRates)
                        {
                            if (pawnCache.IsLearningRateAboveThreshold(workType, true))
                            {
                                pawnCache.WorkPriorities[workType] = Settings.Settings.MajorLearningRatePriority;
                            }
                            else if (pawnCache.IsLearningRateAboveThreshold(workType, false))
                            {
                                pawnCache.WorkPriorities[workType] = Settings.Settings.MinorLearningRatePriority;
                            }
                        }
                        else
                        {
                            switch (pawnCache.GetPassion(workType))
                            {
                                case Passion.Major:
                                    pawnCache.WorkPriorities[workType] = Settings.Settings.MajorPassionPriority;
                                    break;
                                case Passion.Minor:
                                    pawnCache.WorkPriorities[workType] = Settings.Settings.MinorPassionPriority;
                                    break;
                            }
                        }
                    }
                }
            }
            if (_pawnCache.Values.Count(pc => pc.IsCapable && pc.IsActiveWork(workType)) == 0)
            {
                var pawnCache = _pawnCache.Values
                    .Where(pc =>
                        pc.IsCapable && pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                        !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType))
                    .OrderByDescending(pc => pc.GetWorkSkillLevel(workType)).FirstOrDefault();
                {
                    if (pawnCache != null)
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {(assignEveryone == null || assignEveryone.AllowDedicated ? Settings.Settings.UseDedicatedWorkers ? Settings.Settings.DedicatedWorkerPriority : Settings.Settings.HighestSkillPriority : assignEveryone.Priority)} (fail-safe)");
#endif
                        pawnCache.WorkPriorities[workType] = assignEveryone == null || assignEveryone.AllowDedicated
                            ? Settings.Settings.UseDedicatedWorkers
                                ? Settings.Settings.DedicatedWorkerPriority
                                : Settings.Settings.HighestSkillPriority
                            : assignEveryone.Priority;
                    }
                }
            }
        }

        private void AssignLeftoverWorkTypes()
        {
#if DEBUG
            Logger.LogMessage("Assigning leftover work types...");
#endif
            if (!_pawnCache.Values.Any(pc => pc.IsCapable))
            {
                return;
            }
            var workTypes = _managedWorkTypes.Where(workType =>
                !Settings.Settings.AssignEveryoneWorkTypes.Any(a => a.WorkTypeDef == workType)).ToList();
            if (Settings.Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!Settings.Settings.UseDedicatedWorkers)
            {
                foreach (var workType in workTypes.Where(workType =>
                             !_pawnCache.Values.Where(pc => pc.IsCapable).Any(pc => pc.IsActiveWork(workType))))
                {
                    foreach (var pawnCache in _pawnCache.Values
                                 .Where(pc =>
                                     pc.IsCapable && pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                                     !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType))
                                 .OrderBy(pc => workTypes.Count(pc.IsActiveWork)))
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.Settings.HighestSkillPriority}");
#endif
                        pawnCache.WorkPriorities[workType] = Settings.Settings.HighestSkillPriority;
                        break;
                    }
                }
                foreach (var pawnCache in _pawnCache.Values.Where(pc =>
                             pc.IsCapable && pc.IsManaged && !pc.IsRecovering && workTypes.Count(pc.IsActiveWork) == 0))
                {
                    var workType = workTypes
                        .Where(wt =>
                            pawnCache.IsManagedWork(wt) && !pawnCache.IsDisabledWork(wt) && !pawnCache.IsBadWork(wt))
                        .OrderBy(wt => _pawnCache.Values.Where(pc => pc.IsCapable).Count(pc => pc.IsActiveWork(wt)))
                        .FirstOrDefault();
                    if (workType != null)
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.Settings.HighestSkillPriority}");
#endif
                        pawnCache.WorkPriorities[workType] = Settings.Settings.HighestSkillPriority;
                    }
                }
            }
            if (Settings.Settings.AssignAllWorkTypes)
            {
                foreach (var pawnCache in
                         _pawnCache.Values.Where(pc => pc.IsCapable && pc.IsManaged && !pc.IsRecovering))
                {
                    foreach (var workType in workTypes.Where(wt =>
                                 pawnCache.IsManagedWork(wt) && !pawnCache.IsBadWork(wt) &&
                                 !pawnCache.IsDisabledWork(wt) && !pawnCache.IsActiveWork(wt)))
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.Settings.LeftoverPriority}");
#endif
                        pawnCache.WorkPriorities[workType] = Settings.Settings.LeftoverPriority;
                    }
                }
            }
        }

        private void AssignWorkersByLearningRate()
        {
#if DEBUG
            Logger.LogMessage("Assigning workers by learning rate...");
#endif
            if (!_pawnCache.Values.Any(pc => pc.IsCapable))
            {
                return;
            }
            foreach (var pawnCache in _pawnCache.Values.Where(pc => pc.IsCapable && pc.IsManaged && !pc.IsRecovering))
            {
                var workTypes = _managedWorkTypes
                    .Except(Settings.Settings.AssignEveryoneWorkTypes.Select(wt => wt.WorkTypeDef)).Where(workType =>
                        pawnCache.IsManagedWork(workType) && !pawnCache.IsDisabledWork(workType) &&
                        !pawnCache.IsBadWork(workType) && !pawnCache.IsActiveWork(workType)).ToList();
                if (Settings.Settings.SpecialRulesForDoctors)
                {
                    workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                        "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                if (Settings.Settings.SpecialRulesForHunters)
                {
                    workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                        "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                foreach (var workType in workTypes)
                {
                    if (pawnCache.IsLearningRateAboveThreshold(workType, true))
                    {
                        pawnCache.WorkPriorities[workType] = Settings.Settings.MajorLearningRatePriority;
                        continue;
                    }
                    if (pawnCache.IsLearningRateAboveThreshold(workType, false))
                    {
                        pawnCache.WorkPriorities[workType] = Settings.Settings.MinorLearningRatePriority;
                    }
                }
            }
        }

        private void AssignWorkersByPassion()
        {
#if DEBUG
            Logger.LogMessage("Assigning workers by passion...");
#endif
            if (!_pawnCache.Values.Any(pc => pc.IsCapable))
            {
                return;
            }
            foreach (var pawnCache in _pawnCache.Values.Where(pc => pc.IsCapable && pc.IsManaged && !pc.IsRecovering))
            {
                var workTypes = _managedWorkTypes
                    .Except(Settings.Settings.AssignEveryoneWorkTypes.Select(wt => wt.WorkTypeDef)).Where(workType =>
                        pawnCache.IsManagedWork(workType) && !pawnCache.IsDisabledWork(workType) &&
                        !pawnCache.IsBadWork(workType) && !pawnCache.IsActiveWork(workType)).ToList();
                if (Settings.Settings.SpecialRulesForDoctors)
                {
                    workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                        "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                if (Settings.Settings.SpecialRulesForHunters)
                {
                    workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                        "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                foreach (var workType in workTypes)
                {
                    switch (pawnCache.GetPassion(workType))
                    {
                        case Passion.Major:
                            pawnCache.WorkPriorities[workType] = Settings.Settings.MajorPassionPriority;
                            break;
                        case Passion.Minor:
                            pawnCache.WorkPriorities[workType] = Settings.Settings.MinorPassionPriority;
                            break;
                    }
                }
            }
        }

        private void AssignWorkersBySkill()
        {
#if DEBUG
            Logger.LogMessage("Assigning workers by skill...");
#endif
            if (!_pawnCache.Values.Any(pc => pc.IsCapable))
            {
                return;
            }
            var workTypes = _managedWorkTypes.Where(w =>
                    !Settings.Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == w) && w.relevantSkills.Any())
                .ToList();
            if (Settings.Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var workType in workTypes)
            {
                var relevantPawns = _pawnCache.Values.Where(pc => pc.IsCapable && !pc.IsDisabledWork(workType))
                    .ToList();
                if (!relevantPawns.Any())
                {
                    continue;
                }
                var maxSkillValue = relevantPawns.Max(pc => pc.GetWorkSkillLevel(workType));
                foreach (var pawnCache in relevantPawns
                             .Where(pc =>
                                 pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                                 !pc.IsBadWork(workType)).OrderByDescending(pc => pc.GetWorkSkillLevel(workType)))
                {
                    if (pawnCache.GetWorkSkillLevel(workType) >= maxSkillValue || _pawnCache.Values
                            .Where(pc => pc.IsCapable).Count(pc => pc.IsActiveWork(workType)) == 0)
                    {
#if DEBUG
                        Logger.LogMessage(
                            $"Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.Settings.HighestSkillPriority} (skill = {pawnCache.GetWorkSkillLevel(workType)}, max = {maxSkillValue})");
#endif
                        pawnCache.WorkPriorities[workType] = Settings.Settings.HighestSkillPriority;
                    }
                }
            }
        }

        private void AssignWorkForRecoveringPawns()
        {
            if (!Settings.Settings.RecoveringPawnsUnfitForWork)
            {
                return;
            }
#if DEBUG
            Logger.LogMessage("Assigning work for recovering pawns...");
#endif
            var relevantWorkTypes = _allWorkTypes
                .Where(wt => new[] { "Patient", "PatientBedRest" }.Contains(wt.defName)).Intersect(_managedWorkTypes);
            foreach (var workType in relevantWorkTypes)
            {
                foreach (var pawnCache in _pawnCache.Values.Where(pc =>
                             pc.IsManaged && pc.IsCapable && !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType)))
                {
                    pawnCache.WorkPriorities[workType] = 1;
                }
            }
        }

        private void AssignWorkToIdlePawns()
        {
            var noLongerIdlePawns = _pawnCache.Values.Where(pc =>
                pc.IdleSince != null && (_workUpdateTime.Year - pc.IdleSince.Value.Year) * 60 * 24 +
                (_workUpdateTime.Day - pc.IdleSince.Value.Day) * 24 + _workUpdateTime.Hour - pc.IdleSince.Value.Hour >
                12).ToList();
            foreach (var pawnCache in noLongerIdlePawns)
            {
                pawnCache.IdleSince = null;
            }
            var idlePawns = _pawnCache.Values.Where(pc =>
                pc.IsCapable && pc.IsManaged && !pc.IsRecovering &&
                (pc.IdleSince != null || (!pc.Pawn.Drafted && pc.Pawn.mindState.IsIdle))).ToList();
            if (!idlePawns.Any())
            {
                return;
            }
            var workTypes = _managedWorkTypes.Where(o =>
                !Settings.Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == o)).ToList();
            if (Settings.Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var pawnCache in idlePawns)
            {
                foreach (var workType in workTypes.Where(wt =>
                             pawnCache.IsManagedWork(wt) && !pawnCache.IsDisabledWork(wt) && !pawnCache.IsBadWork(wt) &&
                             !pawnCache.IsActiveWork(wt)))
                {
                    pawnCache.WorkPriorities[workType] = Settings.Settings.IdlePriority;
                }
                if (pawnCache.IdleSince == null)
                {
                    pawnCache.IdleSince =
                        new RimWorldTime(_workUpdateTime.Year, _workUpdateTime.Day, _workUpdateTime.Hour);
                }
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!WorkManager.PriorityManagementEnabled)
            {
                return;
            }
            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused || Find.TickManager.TicksGame % 60 != 0)
            {
                return;
            }
            if (!Settings.Settings.Initialized)
            {
                Settings.Settings.Initialize();
            }
            var year = GenLocalDate.Year(map);
            var day = GenLocalDate.DayOfYear(map);
            var hourFloat = GenLocalDate.HourFloat(map);
            var hoursPassed = (year - _workUpdateTime.Year) * 60 * 24 + (day - _workUpdateTime.Day) * 24 + hourFloat -
                              _workUpdateTime.Hour;
            if (hoursPassed < 24f / Settings.Settings.UpdateFrequency)
            {
                return;
            }
            if (!Current.Game.playSettings.useWorkPriorities)
            {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                             pawn.Faction == Faction.OfPlayer))
                {
                    pawn.workSettings?.Notify_UseWorkPrioritiesChanged();
                }
            }
#if DEBUG
            Logger.LogMessage(
                $"Updating work priorities... (year = {year}, day = {day}, hour = {hourFloat:N1}, passed = {hoursPassed:N1})");
#endif
            _workUpdateTime = new RimWorldTime(year, day, hourFloat);
            UpdateCache();
            UpdateWorkPriorities();
            ApplyWorkPriorities();
        }

        private void UpdateCache()
        {
            if (!_allWorkTypes.Any())
            {
                _allWorkTypes.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible));
            }
            _managedWorkTypes.Clear();
            _managedWorkTypes.AddRange(_allWorkTypes.Where(w => WorkManager.GetWorkTypeEnabled(w)));
            _allPawns.Clear();
            _allPawns.AddRange(map.mapPawns.FreeColonistsSpawned);
            foreach (var pawn in _pawnCache.Keys.Where(pawn => !_allPawns.Contains(pawn)).ToList())
            {
                _pawnCache.Remove(pawn);
            }
            foreach (var pawn in _allPawns)
            {
                if (!_pawnCache.ContainsKey(pawn))
                {
                    _pawnCache.Add(pawn, new PawnCache(pawn));
                }
                _pawnCache[pawn].Update(_workUpdateTime);
            }
        }

        private void UpdateWorkPriorities()
        {
            AssignWorkForRecoveringPawns();
            AssignCommonWork();
            AssignDoctors();
            AssignHunters();
            if (Settings.Settings.UseDedicatedWorkers)
            {
                AssignDedicatedWorkers();
            }
            else
            {
                AssignWorkersBySkill();
            }
            if (Settings.Settings.UseLearningRates)
            {
                AssignWorkersByLearningRate();
            }
            else
            {
                AssignWorkersByPassion();
            }
            AssignLeftoverWorkTypes();
            if (Settings.Settings.AssignWorkToIdlePawns)
            {
                AssignWorkToIdlePawns();
            }
        }
    }
}