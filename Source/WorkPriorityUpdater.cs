using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace WorkManager
{
    [UsedImplicitly]
    public class WorkPriorityUpdater : MapComponent
    {
        private readonly HashSet<Pawn> _allPawns = new HashSet<Pawn>();
        private readonly HashSet<WorkTypeDef> _allWorkTypes = new HashSet<WorkTypeDef>();
        private readonly HashSet<WorkTypeDef> _managedWorkTypes = new HashSet<WorkTypeDef>();
        private readonly Dictionary<Pawn, PawnCache> _pawnCache = new Dictionary<Pawn, PawnCache>();

        private readonly RimworldTime _workUpdateTime = new RimworldTime(-1, -1, -1);

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
                    if (Settings.SetPriorityMethod == null)
                    {
                        pawnCache.Pawn.workSettings.SetPriority(workType, priority);
                    }
                    else
                    {
                        Settings.SetPriorityMethod.Invoke(null,
                            new object[] { pawnCache.Pawn, workType, priority, null });
                    }
                }
            }
        }

        private void AssignCommonWork()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"-- Work Manager: Assigning common work types ({string.Join(", ", Settings.AssignEveryoneWorkTypes.Select(workType => $"{workType.Label}[{workType.Priority}]"))}) --");
            }
            var relevantWorkTypes = Settings.AssignEveryoneWorkTypes.Where(workType => workType.IsWorkTypeLoaded)
                .Select(wt => wt.WorkTypeDef).Intersect(_managedWorkTypes);
            foreach (var workType in relevantWorkTypes)
            {
                foreach (var pawnCache in _pawnCache.Values.Where(pc =>
                    pc.IsManaged && pc.IsCapable && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                    !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType)))
                {
                    pawnCache.WorkPriorities[workType] = Settings.AssignEveryoneWorkTypes
                        .First(wt => wt.WorkTypeDef == workType).Priority;
                }
            }
        }

        private void AssignDedicatedWorkers()
        {
            if (!Settings.UseDedicatedWorkers) { return; }
            var capablePawns = _pawnCache.Values.Where(pc => pc.IsCapable).ToList();
            if (!capablePawns.Any()) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning dedicated workers --");
            }
            var workTypes = _allWorkTypes.Intersect(_managedWorkTypes).Where(wt =>
                    Settings.AssignEveryoneWorkTypes.FirstOrDefault(a => a.WorkTypeDef == wt)?.AllowDedicated ?? true)
                .ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!workTypes.Any()) { return; }
            var targetWorkers = (int)Math.Ceiling((float)capablePawns.Count / workTypes.Count);
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"-- Work Manager: Target dedicated workers by work type = {targetWorkers} --");
            }
            foreach (var workType in workTypes.OrderByDescending(wt => wt.relevantSkills.Count)
                .ThenByDescending(wt => wt.naturalPriority))
            {
                var relevantPawns = capablePawns.Where(pc =>
                    !pc.IsRecovering && !pc.IsDisabledWork(workType) && !pc.IsBadWork(workType)).ToList();
                if (!relevantPawns.Any()) { continue; }
                var dedicatedWorkers = relevantPawns.Where(pc =>
                    pc.IsActiveWork(workType) && pc.WorkPriorities[workType] <= Settings.DedicatedWorkerPriority);
                if (dedicatedWorkers.Count() >= targetWorkers) { continue; }
                var pawnSkills = relevantPawns.ToDictionary(pc => pc, pc => pc.GetWorkSkillLevel(workType));
                var maxSkill = pawnSkills.Max(pair => pair.Value);
                var minSkill = pawnSkills.Min(pair => pair.Value);
                var skillRange = maxSkill - minSkill;
                var pawnDedicationsCounts = relevantPawns.ToDictionary(pc => pc,
                    pc => workTypes.Count(wt =>
                        pc.IsActiveWork(wt) && pc.WorkPriorities[wt] <= Settings.DedicatedWorkerPriority));
                var maxDedications = pawnDedicationsCounts.Max(pair => pair.Value);
                var minDedications = pawnDedicationsCounts.Min(pair => pair.Value);
                var dedicationsCountRange = maxDedications - minDedications;
                var pawnScores = new Dictionary<PawnCache, float>();
                foreach (var pawnCache in relevantPawns.Where(pc => pc.IsManagedWork(workType)))
                {
                    var skill = pawnSkills[pawnCache];
                    var normalizedSkill = skillRange == 0f ? 0f : (float)skill / skillRange;
                    var normalizedLearnRate = pawnCache.IsLearningRateAboveThreshold(workType, true) ? 1f :
                        pawnCache.IsLearningRateAboveThreshold(workType, false) ? 0.5f : 0f;
                    var normalizedDedications = dedicationsCountRange == 0
                        ? 0f
                        : (float)pawnDedicationsCounts[pawnCache] / dedicationsCountRange;
                    var score = normalizedSkill - normalizedDedications;
                    score += 1.25f * normalizedLearnRate;
                    pawnScores.Add(pawnCache, score);
                }
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"-- Work Manager: Skill range = {skillRange} [{minSkill};{maxSkill}]. Dedication range = {dedicationsCountRange} [{minDedications}; {maxDedications}] --");
                    Log.Message(
                        $"-- Work Manager: {string.Join(", ", pawnScores.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key.Pawn.LabelShort}({pair.Value:N2})"))} --");
                }
                while (capablePawns.Count(pc =>
                           pc.IsActiveWork(workType) &&
                           pc.WorkPriorities[workType] <= Settings.DedicatedWorkerPriority) <
                       targetWorkers)
                {
                    var dedicatedWorker = pawnScores.Any()
                        ? pawnScores.OrderByDescending(pair => pair.Value).First().Key
                        : null;
                    if (dedicatedWorker == null) { break; }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{dedicatedWorker.Pawn.LabelShort}' as dedicated worker for '{workType.labelShort}'");
                    }
                    dedicatedWorker.WorkPriorities[workType] = Settings.DedicatedWorkerPriority;
                    pawnScores.Remove(dedicatedWorker);
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----------------------------------------------------");
            }
        }

        private void AssignDoctors()
        {
            if (!Settings.SpecialRulesForDoctors) { return; }
            var workType = _allWorkTypes.FirstOrDefault(workTypeDef =>
                "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null) { return; }
            if (!WorkManager.GetWorkTypeEnabled(workType)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("-- Work Manager: Assigning doctors... --"); }
            var relevantPawns = _pawnCache.Values.Where(pc => pc.IsCapable && !pc.IsDisabledWork(workType)).ToList();
            if (!relevantPawns.Any()) { return; }
            var assignedDoctors = relevantPawns.Where(pc => pc.IsActiveWork(workType)).ToList();
            var maxSkillValue = relevantPawns.Max(pc => pc.GetWorkSkillLevel(workType));
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"Work Manager: Max doctoring skill value = '{maxSkillValue}'");
            }
            var assignEveryone = Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
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
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as primary doctor (highest skill value)");
                            }
                            pawnCache.WorkPriorities[workType] = Settings.DoctoringPriority;
                            assignedDoctors.Add(pawnCache);
                            continue;
                        }
                    }
                    if (assignedDoctors.Count == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as primary doctor (highest skill value)");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.DoctoringPriority;
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
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as primary doctor (fail-safe)");
                    }
                    pawnCache.WorkPriorities[workType] = assignEveryone == null || assignEveryone.AllowDedicated
                        ? Settings.DoctoringPriority
                        : assignEveryone.Priority;
                    assignedDoctors.Add(pawnCache);
                }
            }
            if (assignedDoctors.Count == 1)
            {
                var doctor = assignedDoctors.First();
                if (doctor.Pawn.health.HasHediffsNeedingTend() || doctor.Pawn.health.hediffSet.HasTendableInjury() ||
                    doctor.Pawn.health.hediffSet.HasTendableHediff())
                {
                    foreach (var pawnCache in relevantPawns
                        .Where(pc =>
                            pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                            !pc.IsActiveWork(workType)).OrderByDescending(pc => pc.GetWorkSkillLevel(workType))
                        .ThenBy(pc => pc.IsBadWork(workType)))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as secondary doctor (primary doctor needs tending)");
                        }
                        pawnCache.WorkPriorities[workType] = assignEveryone == null || assignEveryone.AllowDedicated
                            ? Settings.DoctoringPriority
                            : assignEveryone.Priority;
                        assignedDoctors.Add(pawnCache);
                        break;
                    }
                }
            }
            if (Settings.AssignMultipleDoctors && (assignEveryone == null || assignEveryone.AllowDedicated))
            {
                var patients = new List<Pawn>();
                if (Settings.CountDownedColonists) { patients.AddRange(_allPawns.Where(pawn => pawn.Downed)); }
                if (Settings.CountDownedGuests && map != null && map.IsPlayerHome)
                {
                    patients.AddRange(map.mapPawns.AllPawnsSpawned.Where(pawn =>
                        pawn?.guest != null && !pawn.IsColonist && !pawn.guest.IsPrisoner && !pawn.IsPrisoner &&
                        (pawn.Downed || pawn.health.HasHediffsNeedingTend() ||
                         pawn.health.hediffSet.HasTendableInjury() || pawn.health.hediffSet.HasTendableHediff())));
                }
                if (Settings.CountDownedPrisoners && map != null && map.IsPlayerHome)
                {
                    patients.AddRange(map.mapPawns.PrisonersOfColonySpawned.Where(pawn =>
                        pawn.Downed || pawn.health.HasHediffsNeedingTend() ||
                        pawn.health.hediffSet.HasTendableInjury() || pawn.health.hediffSet.HasTendableHediff()));
                }
                if (Settings.CountDownedAnimals && map != null && map.IsPlayerHome)
                {
                    patients.AddRange(map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal)
                        .Where(pawn => pawn.Downed || pawn.health.HasHediffsNeedingTend() ||
                                       pawn.health.hediffSet.HasTendableInjury() ||
                                       pawn.health.hediffSet.HasTendableHediff()));
                }
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"Work Manager: Patient count = '{patients.Count}' ({string.Join(", ", patients.Select(pawn => pawn.LabelShort))})");
                }
                while (assignedDoctors.Count < patients.Count)
                {
                    var pawnCache = relevantPawns
                        .Where(pc =>
                            pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) &&
                            !pc.IsActiveWork(workType)).OrderBy(pc => pc.IsBadWork(workType))
                        .ThenByDescending(pc => pc.GetWorkSkillLevel(workType)).FirstOrDefault();
                    if (pawnCache == null) { break; }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as backup doctor (multiple patients)");
                    }
                    pawnCache.WorkPriorities[workType] = Settings.DoctoringPriority;
                    assignedDoctors.Add(pawnCache);
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        private void AssignHunters()
        {
            if (!Settings.SpecialRulesForHunters) { return; }
            var workType = _allWorkTypes.FirstOrDefault(workTypeDef =>
                "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null) { return; }
            if (!_managedWorkTypes.Contains(workType)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("-- Work Manager: Assigning hunters... --"); }
            var assignEveryone = Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            if (assignEveryone != null)
            {
                foreach (var pawnCache in _pawnCache.Values.Where(pc =>
                    pc.IsCapable && pc.IsManaged && pc.IsManagedWork(workType) && pc.IsActiveWork(workType) &&
                    !pc.IsHunter()))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Removing hunting assignment from '{pawnCache.Pawn.LabelShort}' (not a hunter)");
                    }
                    pawnCache.WorkPriorities[workType] = 0;
                }
                if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
            }
            var hunters = _pawnCache.Values.Where(pc => pc.IsCapable && (pc.IsHunter() || pc.IsActiveWork(workType)))
                .ToList();
            var maxSkillValue = hunters.Any() ? hunters.Max(pc => pc.GetWorkSkillLevel(workType)) : 0;
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"Work Manager: Hunters are {string.Join(", ", hunters.Select(pc => $"{pc.Pawn.LabelShortCap} ({pc.GetWorkSkillLevel(workType):N2})"))}");
                Log.Message($"Work Manager: Max hunting skill value = '{maxSkillValue}'");
            }
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var pawnCache in hunters
                    .Where(pc =>
                        pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) && !pc.IsBadWork(workType))
                    .OrderByDescending(pc => pc.GetWorkSkillLevel(workType)))
                {
                    if (pawnCache.GetWorkSkillLevel(workType) >= maxSkillValue ||
                        _pawnCache.Values.Count(pc => pc.IsCapable && pc.IsActiveWork(workType)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as a hunter with priority 1 (highest skill value)");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.UseDedicatedWorkers
                            ? Settings.DedicatedWorkerPriority
                            : Settings.HighestSkillPriority;
                    }
                    else
                    {
                        if (pawnCache.IsLearningRateAboveThreshold(workType, true))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as a hunter with priority 2 (major learning rate)");
                            }
                            pawnCache.WorkPriorities[workType] = Settings.MajorLearningRatePriority;
                        }
                        else if (pawnCache.IsLearningRateAboveThreshold(workType, false))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawnCache.Pawn.LabelShort}' as a hunter with priority 3 (minor learning rate)");
                            }
                            pawnCache.WorkPriorities[workType] = Settings.MinorLearningRatePriority;
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
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {(assignEveryone == null || assignEveryone.AllowDedicated ? Settings.UseDedicatedWorkers ? Settings.DedicatedWorkerPriority : Settings.HighestSkillPriority : assignEveryone.Priority)} (fail-safe)");
                        }
                        pawnCache.WorkPriorities[workType] = assignEveryone == null || assignEveryone.AllowDedicated
                            ? Settings.UseDedicatedWorkers ? Settings.DedicatedWorkerPriority :
                            Settings.HighestSkillPriority
                            : assignEveryone.Priority;
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        private void AssignLeftoverWorkTypes()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning leftover work types... --");
            }
            if (!_pawnCache.Values.Any(pc => pc.IsCapable)) { return; }
            var workTypes = _managedWorkTypes.Where(workType =>
                !Settings.AssignEveryoneWorkTypes.Any(a => a.WorkTypeDef == workType)).ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!Settings.UseDedicatedWorkers)
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
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.HighestSkillPriority}");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.HighestSkillPriority;
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
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.HighestSkillPriority}");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.HighestSkillPriority;
                    }
                }
            }
            if (Settings.AssignAllWorkTypes)
            {
                foreach (var pawnCache in _pawnCache.Values.Where(
                    pc => pc.IsCapable && pc.IsManaged && !pc.IsRecovering))
                {
                    foreach (var workType in workTypes.Where(wt =>
                        pawnCache.IsManagedWork(wt) && !pawnCache.IsBadWork(wt) && !pawnCache.IsDisabledWork(wt) &&
                        !pawnCache.IsActiveWork(wt)))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.LeftoverPriority}");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.LeftoverPriority;
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        private void AssignWorkersByLearningRate()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning workers by learning rate... --");
            }
            if (!_pawnCache.Values.Any(pc => pc.IsCapable)) { return; }
            foreach (var pawnCache in _pawnCache.Values.Where(pc => pc.IsCapable && pc.IsManaged && !pc.IsRecovering))
            {
                var workTypes = _managedWorkTypes.Except(Settings.AssignEveryoneWorkTypes.Select(wt => wt.WorkTypeDef))
                    .Where(workType => pawnCache.IsManagedWork(workType) && !pawnCache.IsDisabledWork(workType) &&
                                       !pawnCache.IsBadWork(workType) && !pawnCache.IsActiveWork(workType)).ToList();
                if (Settings.SpecialRulesForDoctors)
                {
                    workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                        "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                if (Settings.SpecialRulesForHunters)
                {
                    workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                        "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                foreach (var workType in workTypes)
                {
                    if (pawnCache.IsLearningRateAboveThreshold(workType, true))
                    {
                        pawnCache.WorkPriorities[workType] = Settings.MajorLearningRatePriority;
                        continue;
                    }
                    if (pawnCache.IsLearningRateAboveThreshold(workType, false))
                    {
                        pawnCache.WorkPriorities[workType] = Settings.MinorLearningRatePriority;
                    }
                }
            }
        }

        private void AssignWorkersBySkill()
        {
            if (Settings.UseDedicatedWorkers) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning workers by skill... --");
            }
            if (!_pawnCache.Values.Any(pc => pc.IsCapable)) { return; }
            var workTypes = _managedWorkTypes.Where(w =>
                !Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == w) && w.relevantSkills.Any()).ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var workType in workTypes)
            {
                var relevantPawns = _pawnCache.Values.Where(pc => pc.IsCapable && !pc.IsDisabledWork(workType))
                    .ToList();
                if (!relevantPawns.Any()) { continue; }
                var maxSkillValue = relevantPawns.Max(pc => pc.GetWorkSkillLevel(workType));
                foreach (var pawnCache in relevantPawns
                    .Where(pc =>
                        pc.IsManaged && !pc.IsRecovering && pc.IsManagedWork(workType) && !pc.IsBadWork(workType))
                    .OrderByDescending(pc => pc.GetWorkSkillLevel(workType)))
                {
                    if (pawnCache.GetWorkSkillLevel(workType) >= maxSkillValue || _pawnCache.Values
                        .Where(pc => pc.IsCapable).Count(pc => pc.IsActiveWork(workType)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.HighestSkillPriority} (skill = {pawnCache.GetWorkSkillLevel(workType)}, max = {maxSkillValue})");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.HighestSkillPriority;
                    } else if(pawnCache.GetWorkSkillLevel(workType) >= maxSkillValue - Settings.HighSkillGapThresholdBelowHighest)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.HighSkillPriority} (skill = {pawnCache.GetWorkSkillLevel(workType)}, max = {maxSkillValue}, high threshold = {maxSkillValue - Settings.HighSkillGapThresholdBelowHighest})");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.HighSkillPriority;
                    } else if (pawnCache.GetWorkSkillLevel(workType) >= Settings.MediumSkillThreshold)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawnCache.Pawn.LabelShort}'s priority of '{workType.labelShort}' to {Settings.MediumSkillPriority} (skill = {pawnCache.GetWorkSkillLevel(workType)}, max = {maxSkillValue}, medium threshold = {Settings.MediumSkillThreshold})");
                        }
                        pawnCache.WorkPriorities[workType] = Settings.MediumSkillPriority;
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        private void AssignWorkForRecoveringPawns()
        {
            if (!Settings.RecoveringPawnsUnfitForWork) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning work for recovering pawns --");
            }
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
            if (!Settings.AssignWorkToIdlePawns) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning work for idle pawns... --");
                foreach (var idlePawn in _pawnCache.Values.Where(pc => pc.IdleSince != null))
                {
                    Log.Message(
                        $"{idlePawn.Pawn.LabelShort} is registered as idle ({idlePawn.IdleSince.Day}, {idlePawn.IdleSince.Hour:N1})");
                }
            }
            var noLongerIdlePawns = _pawnCache.Values.Where(pc =>
                pc.IdleSince != null && (_workUpdateTime.Year - pc.IdleSince.Year) * 60 * 24 +
                (_workUpdateTime.Day - pc.IdleSince.Day) * 24 + _workUpdateTime.Hour - pc.IdleSince.Hour > 12).ToList();
            foreach (var pawnCache in noLongerIdlePawns) { pawnCache.IdleSince = null; }
            var idlePawns = _pawnCache.Values.Where(pc =>
                pc.IsCapable && pc.IsManaged && !pc.IsRecovering &&
                (pc.IdleSince != null || !pc.Pawn.Drafted && pc.Pawn.mindState.IsIdle)).ToList();
            if (!idlePawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(o =>
                !Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == o)).ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(_allWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var pawnCache in idlePawns)
            {
                foreach (var workType in workTypes.Where(wt =>
                    pawnCache.IsManagedWork(wt) && !pawnCache.IsDisabledWork(wt) && !pawnCache.IsBadWork(wt) &&
                    !pawnCache.IsActiveWork(wt))) { pawnCache.WorkPriorities[workType] = Settings.IdlePriority; }
                if (pawnCache.IdleSince == null)
                {
                    pawnCache.IdleSince =
                        new RimworldTime(_workUpdateTime.Year, _workUpdateTime.Day, _workUpdateTime.Hour);
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!WorkManager.PriorityManagementEnabled) { return; }
            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused || Find.TickManager.TicksGame % 60 != 0) { return; }
            if (!Settings.Initialized) { Settings.Initialize(); }
            var year = GenLocalDate.Year(map);
            var day = GenLocalDate.DayOfYear(map);
            var hourFloat = GenLocalDate.HourFloat(map);
            var hoursPassed = (year - _workUpdateTime.Year) * 60 * 24 + (day - _workUpdateTime.Day) * 24 + hourFloat -
                              _workUpdateTime.Hour;
            if (hoursPassed < 24f / Settings.UpdateFrequency) { return; }
            if (!Current.Game.playSettings.useWorkPriorities)
            {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                    pawn.Faction == Faction.OfPlayer)) { pawn.workSettings?.Notify_UseWorkPrioritiesChanged(); }
            }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"----- Work Manager: Updating work priorities... (year = {year}, day = {day}, hour = {hourFloat:N1}, passed = {hoursPassed:N1}) -----");
            }
            _workUpdateTime.Year = year;
            _workUpdateTime.Day = day;
            _workUpdateTime.Hour = hourFloat;
            UpdateCache();
            UpdateWorkPriorities();
            ApplyWorkPriorities();
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----------------------------------------------------");
            }
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
                if (!_pawnCache.ContainsKey(pawn)) { _pawnCache.Add(pawn, new PawnCache(pawn)); }
                _pawnCache[pawn].Update(_workUpdateTime);
            }
        }

        private void UpdateWorkPriorities()
        {
            AssignWorkForRecoveringPawns();
            AssignCommonWork();
            AssignDoctors();
            AssignHunters();
            AssignDedicatedWorkers();
            AssignWorkersBySkill();
            AssignWorkersByLearningRate();
            AssignLeftoverWorkTypes();
            AssignWorkToIdlePawns();
        }
    }
}