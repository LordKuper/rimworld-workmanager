using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly HashSet<Pawn> _capablePawns = new HashSet<Pawn>();

        private readonly Dictionary<Pawn, DayTime> _idlePawns = new Dictionary<Pawn, DayTime>();

        private readonly HashSet<Pawn> _managedPawns = new HashSet<Pawn>();
        private readonly HashSet<WorkTypeDef> _managedWorkTypes = new HashSet<WorkTypeDef>();

        private readonly Dictionary<Pawn, List<WorkPriority>> _pawnWorkPriorities =
            new Dictionary<Pawn, List<WorkPriority>>();

        private int _currentDay = -1;
        private float _currentTime = -1;

        public WorkPriorityUpdater(Map map) : base(map) { }

        private static WorkManagerGameComponent WorkManager => Current.Game.GetComponent<WorkManagerGameComponent>();

        private void ApplyWorkPriorities()
        {
            foreach (var pawnWorkPriority in _pawnWorkPriorities.Where(pawnWorkPriority =>
                _managedPawns.Contains(pawnWorkPriority.Key)))
            {
                foreach (var workPriority in pawnWorkPriority.Value.Where(workPriority =>
                    _managedWorkTypes.Contains(workPriority.WorkType)))
                {
                    pawnWorkPriority.Key.workSettings.SetPriority(workPriority.WorkType, workPriority.Priority);
                }
            }
        }

        private void AssignCommonWork()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"-- Work Manager: Assigning common work types ({string.Join(", ", Settings.AssignEveryoneWorkTypes.Select(workType => $"{workType.Label}[{workType.Priority}]"))}) --",
                    true);
            }
            var relevantWorkTypes = Settings.AssignEveryoneWorkTypes.Where(wt => wt.IsWorkTypeLoaded)
                .Select(wt => wt.WorkTypeDef).Intersect(_managedWorkTypes);
            foreach (var workTypeDef in relevantWorkTypes)
            {
                foreach (var pawn in _managedPawns.Intersect(_capablePawns).Where(pawn =>
                    WorkManager.GetPawnWorkTypeEnabled(pawn, workTypeDef) && !pawn.WorkTypeIsDisabled(workTypeDef) &&
                    !IsBadWork(pawn, workTypeDef)))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    SetPawnWorkTypePriority(pawn, workTypeDef,
                        Settings.AssignEveryoneWorkTypes.First(wt => wt.WorkTypeDef == workTypeDef).Priority);
                }
            }
        }

        private void AssignDedicatedWorkers()
        {
            if (!Settings.UseDedicatedWorkers) { return; }
            if (!_capablePawns.Any()) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning dedicated workers --", true);
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
            var targetWorkers = (int) Math.Ceiling((float) _capablePawns.Count / workTypes.Count);
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"-- Work Manager: Target dedicated workers by work type = {targetWorkers} --", true);
            }
            foreach (var workType in workTypes.OrderByDescending(wt => wt.naturalPriority))
            {
                var pawnSkills = new Dictionary<Pawn, float>(_capablePawns.ToDictionary(pawn => pawn,
                    pawn => pawn.skills.AverageOfRelevantSkillsFor(workType)));
                var skillRange = pawnSkills.Max(pair => pair.Value) - pawnSkills.Min(pair => pair.Value);
                var pawnLearnRates =
                    new Dictionary<Pawn, float>(_capablePawns.ToDictionary(pawn => pawn,
                        pawn => GetPawnLearningRate(pawn, workType)));
                var learnRateRange = pawnLearnRates.Max(pair => pair.Value) - pawnLearnRates.Min(pair => pair.Value);
                var pawnDedicationsCounts = new Dictionary<Pawn, int>(_capablePawns.ToDictionary(pawn => pawn,
                    pawn => workTypes.Count(wt => GetPawnWorkTypePriority(pawn, wt) == 1)));
                var dedicationsCountRange = pawnDedicationsCounts.Max(pair => pair.Value) -
                                            pawnDedicationsCounts.Min(pair => pair.Value);
                var pawnScores = new Dictionary<Pawn, float>();
                foreach (var pawn in _capablePawns)
                {
                    if (pawn.WorkTypeIsDisabled(workType) || IsBadWork(pawn, workType)) { continue; }
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    var normalizedSkill = skillRange == 0 ? 0 : pawnSkills[pawn] / skillRange;
                    var normalizedLearnRate = learnRateRange == 0 ? 0 : pawnLearnRates[pawn] / learnRateRange;
                    var normalizedDedications = dedicationsCountRange == 0
                        ? 0
                        : pawnDedicationsCounts[pawn] / dedicationsCountRange;
                    var score = normalizedSkill + 0.75f * normalizedLearnRate - normalizedDedications;
                    pawnScores.Add(pawn, score);
                }
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"-- Work Manager: {string.Join(", ", pawnScores.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key.LabelShort}({pair.Value:N2})"))} --",
                        true);
                }
                while (_capablePawns.Count(pawn => GetPawnWorkTypePriority(pawn, workType) == 1) < targetWorkers)
                {
                    var dedicatedWorker = pawnScores.Any()
                        ? pawnScores.OrderByDescending(pair => pair.Value).First().Key
                        : null;
                    if (dedicatedWorker == null) { break; }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{dedicatedWorker.LabelShort}' as dedicated worker for '{workType.labelShort}'",
                            true);
                    }
                    SetPawnWorkTypePriority(dedicatedWorker, workType, 1);
                    pawnScores.Remove(dedicatedWorker);
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----------------------------------------------------", true);
            }
        }

        private void AssignDoctors()
        {
            if (!Settings.SpecialRulesForDoctors) { return; }
            var workType = _allWorkTypes.FirstOrDefault(workTypeDef =>
                "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null) { return; }
            if (!WorkManager.GetWorkTypeEnabled(workType)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning doctors... --", true);
            }
            var doctors = _capablePawns.Where(pawn => !pawn.WorkTypeIsDisabled(workType)).ToList();
            if (!doctors.Any()) { return; }
            var doctorsCount = doctors.Count(p => IsPawnWorkTypeActive(p, workType));
            var maxSkillValue = (int) Math.Floor(doctors.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(workType)));
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"Work Manager: Max doctoring skill value = '{maxSkillValue}'", true);
            }
            var assignEveryone = Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            var managedDoctors = doctors.Intersect(_managedPawns)
                .Where(pawn => WorkManager.GetPawnWorkTypeEnabled(pawn, workType)).OrderBy(p => IsBadWork(p, workType))
                .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)).ToList();
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var pawn in managedDoctors)
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    if (pawn.skills.AverageOfRelevantSkillsFor(workType) >= maxSkillValue)
                    {
                        if (doctorsCount == 0 || !IsBadWork(pawn, workType))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (highest skill value)",
                                    true);
                            }
                            SetPawnWorkTypePriority(pawn, workType, 1);
                            doctorsCount++;
                            continue;
                        }
                    }
                    if (doctorsCount == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (highest skill value)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, 1);
                        doctorsCount++;
                        break;
                    }
                }
            }
            if (doctorsCount == 0)
            {
                var pawn = managedDoctors.FirstOrDefault();
                if (pawn != null)
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (fail-safe)", true);
                    }
                    SetPawnWorkTypePriority(pawn, workType,
                        assignEveryone == null || assignEveryone.AllowDedicated ? 1 : assignEveryone.Priority);
                    doctorsCount++;
                }
            }
            if (doctorsCount == 1)
            {
                var doctor = doctors.First(p => IsPawnWorkTypeActive(p, workType));
                if (doctor.health.HasHediffsNeedingTend() || doctor.health.hediffSet.HasTendableInjury() ||
                    doctor.health.hediffSet.HasTendableHediff())
                {
                    foreach (var pawn in doctors.Intersect(_managedPawns)
                        .Where(pawn =>
                            WorkManager.GetPawnWorkTypeEnabled(pawn, workType) && !IsPawnWorkTypeActive(pawn, workType))
                        .OrderByDescending(pawn => pawn.skills.AverageOfRelevantSkillsFor(workType))
                        .ThenBy(pawn => IsBadWork(pawn, workType)))
                    {
                        if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                        {
                            continue;
                        }
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as secondary doctor (primary doctor needs tending)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType,
                            assignEveryone == null || assignEveryone.AllowDedicated ? 1 : assignEveryone.Priority);
                        doctorsCount++;
                        break;
                    }
                }
            }
            if (Settings.AssignMultipleDoctors && (assignEveryone == null || assignEveryone.AllowDedicated))
            {
                var patientCount = 0;
                if (Settings.CountDownedColonists) { patientCount += _allPawns.Count(pawn => pawn.Downed); }
                if (Settings.CountDownedGuests && map.IsPlayerHome)
                {
                    patientCount += map.mapPawns.AllPawnsSpawned.Count(pawn =>
                        pawn.guest != null && !pawn.IsPrisoner && pawn.Downed);
                }
                if (Settings.CountDownedPrisoners && map.IsPlayerHome)
                {
                    patientCount += map.mapPawns.PrisonersOfColonySpawned.Count(pawn => pawn.Downed);
                }
                if (Settings.CountDownedAnimals && map.IsPlayerHome)
                {
                    patientCount += map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.RaceProps.Animal)
                        .Count(pawn => pawn.Downed);
                }
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message($"Work Manager: Patient count = '{patientCount}'", true);
                }
                while (doctorsCount < patientCount)
                {
                    var pawn = doctors.Intersect(_managedPawns)
                        .Where(p => WorkManager.GetPawnWorkTypeEnabled(p, workType) &&
                                    !IsPawnWorkTypeActive(p, workType) && (!Settings.RecoveringPawnsUnfitForWork ||
                                                                           !HealthAIUtility.ShouldSeekMedicalRest(p)))
                        .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType))
                        .ThenBy(p => IsBadWork(p, workType)).FirstOrDefault();
                    if (pawn == null) { break; }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as backup doctor (multiple patients)",
                            true);
                    }
                    SetPawnWorkTypePriority(pawn, workType, 1);
                    doctorsCount++;
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignHunters()
        {
            if (!Settings.SpecialRulesForHunters) { return; }
            var workType = _allWorkTypes.FirstOrDefault(workTypeDef =>
                "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null) { return; }
            if (!WorkManager.GetWorkTypeEnabled(workType)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning hunters... --", true);
            }
            var assignEveryone = Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            if (assignEveryone != null)
            {
                foreach (var pawn in _capablePawns.Intersect(_managedPawns).Where(pawn =>
                    WorkManager.GetPawnWorkTypeEnabled(pawn, workType) && IsPawnWorkTypeActive(pawn, workType) &&
                    !IsHunter(pawn)))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Removing hunting assignment from '{pawn.LabelShort}' (not a hunter)", true);
                    }
                    SetPawnWorkTypePriority(pawn, workType, 0);
                }
                if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
            }
            var hunters = _capablePawns.Where(pawn => IsHunter(pawn) || IsPawnWorkTypeActive(pawn, workType)).ToList();
            var maxSkillValue = hunters.Any()
                ? (int) Math.Floor(hunters.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(workType)))
                : 0;
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"Work Manager: Hunters are {string.Join(", ", hunters.Select(p => $"{p.LabelShortCap} ({p.skills.AverageOfRelevantSkillsFor(workType):N2})"))}",
                    true);
                Log.Message($"Work Manager: Max hunting skill value = '{maxSkillValue}'", true);
            }
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var pawn in hunters.Intersect(_managedPawns)
                    .Where(pawn => WorkManager.GetPawnWorkTypeEnabled(pawn, workType))
                    .OrderBy(p => IsBadWork(p, workType))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    if (pawn.skills.AverageOfRelevantSkillsFor(workType) >= maxSkillValue ||
                        _capablePawns.Count(p => IsPawnWorkTypeActive(p, workType)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 1 (highest skill value)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, 1);
                    }
                    else
                    {
                        if (IsPawnLearningRateAboveMajorThreshold(pawn, workType))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 2 (major learning rate)",
                                    true);
                            }
                            SetPawnWorkTypePriority(pawn, workType, 2);
                        }
                        else if (IsPawnLearningRateAboveMinorThreshold(pawn, workType))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 3 (minor learning rate)",
                                    true);
                            }
                            SetPawnWorkTypePriority(pawn, workType, 3);
                        }
                    }
                }
            }
            if (_capablePawns.Count(p => IsPawnWorkTypeActive(p, workType)) == 0)
            {
                var pawn = _capablePawns.Intersect(_managedPawns)
                    .Where(p => WorkManager.GetPawnWorkTypeEnabled(p, workType) && !p.WorkTypeIsDisabled(workType))
                    .OrderBy(p => IsBadWork(p, workType))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)).FirstOrDefault();
                {
                    if (pawn != null)
                    {
                        if (!Settings.RecoveringPawnsUnfitForWork || !HealthAIUtility.ShouldSeekMedicalRest(pawn))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {(assignEveryone == null || assignEveryone.AllowDedicated ? 1 : assignEveryone.Priority)} (fail-safe)",
                                    true);
                            }
                            SetPawnWorkTypePriority(pawn, workType,
                                assignEveryone == null || assignEveryone.AllowDedicated ? 1 : assignEveryone.Priority);
                        }
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignLeftoverWorkTypes()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning leftover work types... --", true);
            }
            if (!_capablePawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(workType =>
                !Settings.AssignEveryoneWorkTypes.Any(a => a.WorkTypeDef == workType) &&
                workType != WorkTypeDefOf.Doctor).ToList();
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
                const int priority = 1;
                var leftoverWorkTypes = workTypes.Where(w => !_capablePawns.Any(p => IsPawnWorkTypeActive(p, w)));
                foreach (var workType in leftoverWorkTypes)
                {
                    var pawns = _capablePawns.Intersect(_managedPawns)
                        .Where(p => WorkManager.GetPawnWorkTypeEnabled(p, workType) &&
                                    !p.WorkTypeIsDisabled(workType) && (!Settings.RecoveringPawnsUnfitForWork ||
                                                                        !HealthAIUtility.ShouldSeekMedicalRest(p)))
                        .OrderBy(p => IsBadWork(p, workType))
                        .ThenBy(p => workTypes.Count(w => IsPawnWorkTypeActive(p, w)));
                    foreach (var pawn in pawns)
                    {
                        if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                        {
                            continue;
                        }
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority}",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, priority);
                        break;
                    }
                }
                foreach (var pawn in _capablePawns.Intersect(_managedPawns).Where(p =>
                    workTypes.Count(w => IsPawnWorkTypeActive(p, w)) == 0))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    var workType = workTypes
                        .Where(wt => WorkManager.GetPawnWorkTypeEnabled(pawn, wt) && !pawn.WorkTypeIsDisabled(wt))
                        .OrderBy(w => IsBadWork(pawn, w))
                        .ThenBy(w => _capablePawns.Count(p => IsPawnWorkTypeActive(p, w))).FirstOrDefault();
                    if (workType != null)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority}",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, priority);
                    }
                }
            }
            if (Settings.AssignAllWorkTypes)
            {
                foreach (var pawn in _capablePawns.Intersect(_managedPawns))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    foreach (var workType in workTypes.Where(wt => WorkManager.GetPawnWorkTypeEnabled(pawn, wt) &&
                                                                   !IsBadWork(pawn, wt) &&
                                                                   !pawn.WorkTypeIsDisabled(wt) &&
                                                                   !IsPawnWorkTypeActive(pawn, wt)))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {4}",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, 4);
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignWorkersByLearningRate()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning workers by learning rate... --", true);
            }
            if (!_capablePawns.Any()) { return; }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns))
            {
                if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn)) { continue; }
                var workTypes = _managedWorkTypes.Where(workType =>
                    WorkManager.GetPawnWorkTypeEnabled(pawn, workType) &&
                    !Settings.AssignEveryoneWorkTypes.Any(a => a.WorkTypeDef == workType) &&
                    !pawn.WorkTypeIsDisabled(workType) && !IsBadWork(pawn, workType) &&
                    !IsPawnWorkTypeActive(pawn, workType)).ToList();
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
                    if (IsPawnLearningRateAboveMajorThreshold(pawn, workType))
                    {
                        SetPawnWorkTypePriority(pawn, workType, 2);
                        continue;
                    }
                    if (IsPawnLearningRateAboveMinorThreshold(pawn, workType))
                    {
                        SetPawnWorkTypePriority(pawn, workType, 3);
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignWorkersBySkill()
        {
            if (Settings.UseDedicatedWorkers) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning workers by skill... --", true);
            }
            if (!_capablePawns.Any()) { return; }
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
            const int priority = 1;
            foreach (var workType in workTypes)
            {
                var relevantPawns = _capablePawns.Where(p => !p.WorkTypeIsDisabled(workType)).ToList();
                if (!relevantPawns.Any()) { continue; }
                var maxSkillValue =
                    (int) Math.Floor(relevantPawns.Max(p => p.skills.AverageOfRelevantSkillsFor(workType)));
                foreach (var pawn in relevantPawns.Intersect(_managedPawns)
                    .Where(pawn => WorkManager.GetPawnWorkTypeEnabled(pawn, workType))
                    .OrderBy(p => IsBadWork(p, workType))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        continue;
                    }
                    if (!IsBadWork(pawn, workType) &&
                        pawn.skills.AverageOfRelevantSkillsFor(workType) >= maxSkillValue ||
                        _capablePawns.Count(p => IsPawnWorkTypeActive(p, workType)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (skill = {pawn.skills.AverageOfRelevantSkillsFor(workType)}, max = {maxSkillValue})",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, priority);
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignWorkForRecoveringPawns()
        {
            if (!Settings.RecoveringPawnsUnfitForWork) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning work for recovering pawns --", true);
            }
            var relevantWorkTypes = _allWorkTypes.Where(wt => new[] {"Patient", "PatientBedRest"}.Contains(wt.defName))
                .Intersect(_managedWorkTypes);
            foreach (var workTypeDef in relevantWorkTypes)
            {
                foreach (var pawn in _managedPawns.Intersect(_capablePawns).Where(pawn =>
                    WorkManager.GetPawnWorkTypeEnabled(pawn, workTypeDef) && !pawn.WorkTypeIsDisabled(workTypeDef) &&
                    !IsBadWork(pawn, workTypeDef))) { SetPawnWorkTypePriority(pawn, workTypeDef, 1); }
            }
        }

        private void AssignWorkPriorities()
        {
            ResetWorkPriorities();
            AssignWorkForRecoveringPawns();
            AssignCommonWork();
            AssignDoctors();
            AssignHunters();
            AssignDedicatedWorkers();
            AssignWorkersBySkill();
            AssignWorkersByLearningRate();
            AssignLeftoverWorkTypes();
            AssignWorkToIdlePawns();
            ApplyWorkPriorities();
        }

        private void AssignWorkToIdlePawns()
        {
            if (!Settings.AssignWorkToIdlePawns) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning work for idle pawns... --", true);
                foreach (var idlePawn in _idlePawns)
                {
                    Log.Message(
                        $"{idlePawn.Key.LabelShort} is registered as idle ({idlePawn.Value.Day}, {idlePawn.Value.Hour:N1})",
                        true);
                }
            }
            var noLongerIdlePawns = (from idlePawn in _idlePawns
                let hoursPassed =
                    _currentDay != idlePawn.Value.Day
                        ? 24 + (_currentTime - idlePawn.Value.Hour)
                        : _currentTime - idlePawn.Value.Hour
                where hoursPassed > 12
                select idlePawn.Key).ToList();
            foreach (var pawn in noLongerIdlePawns) { _idlePawns.Remove(pawn); }
            var pawns = _capablePawns.Intersect(_managedPawns)
                .Where(p => _idlePawns.ContainsKey(p) || !p.Drafted && p.mindState.IsIdle).ToList();
            if (!pawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(o =>
                !Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == o) && o != WorkTypeDefOf.Doctor).ToList();
            if (Settings.SpecialRulesForHunters)
            {
                workTypes = workTypes.Except(new[] {WorkTypeDefOf.Hunting}).ToList();
            }
            const int priority = 4;
            foreach (var pawn in pawns)
            {
                if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn)) { continue; }
                foreach (var workType in workTypes.Where(wt =>
                    WorkManager.GetPawnWorkTypeEnabled(pawn, wt) && !pawn.WorkTypeIsDisabled(wt) &&
                    !IsBadWork(pawn, wt) && !IsPawnWorkTypeActive(pawn, wt)))
                {
                    SetPawnWorkTypePriority(pawn, workType, priority);
                }
                if (!_idlePawns.ContainsKey(pawn)) { _idlePawns.Add(pawn, new DayTime(_currentDay, _currentTime)); }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private static float GetPawnLearningRate([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return workType.relevantSkills.Any()
                ? workType.relevantSkills.Average(s => pawn.skills.GetSkill(s).LearnRateFactor())
                : 0;
        }

        private int GetPawnWorkTypePriority(Pawn pawn, Def workType)
        {
            return _pawnWorkPriorities[pawn].FirstOrDefault(w => w.WorkType == workType)?.Priority ?? 0;
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        private static bool IsBadWork(Pawn pawn, WorkTypeDef workType)
        {
            if (Settings.IsBadWorkMethod == null) { return false; }
            return (bool) Settings.IsBadWorkMethod.Invoke(null, new object[] {pawn, workType});
        }

        private static bool IsHunter([NotNull] Pawn pawn)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hunting)) { return false; }
            if (IsBadWork(pawn, WorkTypeDefOf.Hunting)) { return false; }
            if (pawn.story.traits.HasTrait(TraitDefOf.Brawler) && !Settings.AllowMeleeHunters) { return false; }
            if ((pawn.equipment.Primary == null || pawn.equipment.Primary.def.IsMeleeWeapon) &&
                !Settings.AllowMeleeHunters) { return false; }
            return true;
        }

        private bool IsPawnLearningRateAboveMajorThreshold([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            var learnRate = GetPawnLearningRate(pawn, workType);
            if (!Settings.UsePawnLearningRateThresholds) { return learnRate >= Settings.MajorLearningRateThreshold; }
            var minLearningRate = pawn.skills.skills.Min(s => s.LearnRateFactor());
            var maxLearningRate = pawn.skills.skills.Max(s => s.LearnRateFactor());
            var learningRateRange = maxLearningRate - minLearningRate;
            if (learningRateRange < 0.01) { return false; }
            return learnRate >= minLearningRate + learningRateRange * Settings.MajorLearningRateThreshold;
        }

        private bool IsPawnLearningRateAboveMinorThreshold([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            var learnRate = GetPawnLearningRate(pawn, workType);
            if (!Settings.UsePawnLearningRateThresholds) { return learnRate >= Settings.MinorLearningRateThreshold; }
            var minLearningRate = pawn.skills.skills.Min(s => s.LearnRateFactor());
            var maxLearningRate = pawn.skills.skills.Max(s => s.LearnRateFactor());
            var learningRateRange = maxLearningRate - minLearningRate;
            if (learningRateRange < 0.01) { return false; }
            return learnRate >= minLearningRate + learningRateRange * Settings.MinorLearningRateThreshold;
        }

        private bool IsPawnWorkTypeActive(Pawn pawn, WorkTypeDef workType)
        {
            return _pawnWorkPriorities[pawn].First(w => w.WorkType == workType).Priority > 0;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!WorkManager.Enabled) { return; }
            var day = GenLocalDate.DayOfYear(map);
            var hourFloat = GenLocalDate.HourFloat(map);
            if ((Find.TickManager.TicksGame + GetHashCode()) % 60 != 0 || Math.Abs(day - _currentDay) * 24 +
                Math.Abs(hourFloat - _currentTime) < 24f / Settings.UpdateFrequency) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"----- Work Manager: Updating work priorities... (day = {day}, hour = {hourFloat}) -----",
                    true);
            }
            if (!Current.Game.playSettings.useWorkPriorities)
            {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                    pawn.Faction == Faction.OfPlayer)) { pawn.workSettings?.Notify_UseWorkPrioritiesChanged(); }
            }
            UpdateCache();
            if (_allPawns.Any()) { AssignWorkPriorities(); }
            _currentDay = day;
            _currentTime = hourFloat;
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----------------------------------------------------", true);
            }
        }

        private void ResetWorkPriorities()
        {
            foreach (var pawn in _allPawns)
            {
                var workPriorities = new List<WorkPriority>();
                if (_managedPawns.Contains(pawn))
                {
                    workPriorities.AddRange(_allWorkTypes.Select(workType => _managedWorkTypes.Contains(workType)
                        ? WorkManager.GetPawnWorkTypeEnabled(pawn, workType) ? new WorkPriority(workType, 0) :
                        new WorkPriority(workType, pawn.workSettings.GetPriority(workType))
                        : new WorkPriority(workType, pawn.workSettings.GetPriority(workType))));
                }
                else
                {
                    workPriorities.AddRange(_allWorkTypes.Select(workType =>
                        new WorkPriority(workType, pawn.workSettings.GetPriority(workType))));
                }
                _pawnWorkPriorities.Add(pawn, workPriorities);
            }
        }

        private void SetPawnWorkTypePriority(Pawn pawn, Def workType, int priority)
        {
            var workPriority = _pawnWorkPriorities[pawn].FirstOrDefault(w => w.WorkType == workType);
            if (workPriority != null) { workPriority.Priority = priority; }
            else
            {
                Log.Error(
                    $"----- Work Manager: Trying to set uncached work priority - {pawn.LabelShort}, {workType.defName} -----",
                    true);
            }
        }

        private void UpdateCache()
        {
            if (Settings.AssignEveryoneWorkTypes == null)
            {
                Settings.AssignEveryoneWorkTypes =
                    new List<AssignEveryoneWorkType>(Settings.DefaultAssignEveryoneWorkTypes);
            }
            _allPawns.Clear();
            _allPawns.AddRange(map.mapPawns.FreeColonistsSpawned);
            _capablePawns.Clear();
            _capablePawns.AddRange(_allPawns.Where(p => !p.Dead && !p.Downed && !p.InMentalState));
            _managedPawns.Clear();
            _managedPawns.AddRange(_allPawns.Where(p => WorkManager.GetPawnEnabled(p)));
            if (!_allWorkTypes.Any())
            {
                _allWorkTypes.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible));
            }
            _managedWorkTypes.Clear();
            _managedWorkTypes.AddRange(_allWorkTypes.Where(w => WorkManager.GetWorkTypeEnabled(w)));
            _pawnWorkPriorities.Clear();
        }
    }
}