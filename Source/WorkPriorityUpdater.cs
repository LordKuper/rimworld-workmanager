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

        private readonly Dictionary<WorkTypeDef, int> _commonWorkTypes = new Dictionary<WorkTypeDef, int>();

        private readonly Dictionary<Pawn, DayTime> _idlePawns = new Dictionary<Pawn, DayTime>();

        private readonly HashSet<Pawn> _managedPawns = new HashSet<Pawn>();
        private readonly HashSet<WorkTypeDef> _managedWorkTypes = new HashSet<WorkTypeDef>();

        private readonly Dictionary<Pawn, List<WorkPriority>> _pawnWorkPriorities =
            new Dictionary<Pawn, List<WorkPriority>>();

        private int _currentDay = -1;
        private float _currentTime = -1;

        public WorkPriorityUpdater(Map map) : base(map) { }

        private Dictionary<WorkTypeDef, int> CommonWorkTypes
        {
            get
            {
                if (!_commonWorkTypes.Any())
                {
                    var workTypes = new Dictionary<string, int>
                    {
                        {"Firefighter", 1},
                        {"Patient", 1},
                        {"PatientBedRest", 1},
                        {"BasicWorker", 1},
                        {"VBE_Writing", 1},
                        {"Training", 3}
                    };
                    foreach (var workType in workTypes)
                    {
                        var workTypeDef = DefDatabase<WorkTypeDef>.GetNamed(workType.Key, false);
                        if (workTypeDef != null) { _commonWorkTypes.Add(workTypeDef, workType.Value); }
                    }
                }
                return _commonWorkTypes;
            }
        }

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
            foreach (var pawn in _managedPawns.Intersect(_capablePawns))
            {
                foreach (var workType in _managedWorkTypes.Intersect(CommonWorkTypes.Keys)
                    .Except(
                        WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.Pawn == pawn).Select(pwt => pwt.WorkType))
                    .Where(w => !pawn.WorkTypeIsDisabled(w) && !IsBadWork(pawn, w)))
                {
                    SetPawnWorkTypePriority(pawn, workType, CommonWorkTypes[workType]);
                }
            }
        }

        private void AssignDoctors()
        {
            if (!WorkManager.GetWorkTypeEnabled(WorkTypeDefOf.Doctor)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning doctors... --", true);
            }
            var doctors = _capablePawns.Where(pawn => !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)).ToList();
            if (!doctors.Any()) { return; }
            var doctorCount = doctors.Count(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor));
            var maxSkillValue =
                (int) Math.Floor(doctors.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor)));
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"Work Manager: Max doctoring skill value = '{maxSkillValue}'", true);
            }
            var managedDoctors = doctors.Intersect(_managedPawns)
                .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == WorkTypeDefOf.Doctor)
                    .Select(pwt => pwt.Pawn)).OrderBy(p => IsBadWork(p, WorkTypeDefOf.Doctor))
                .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor)).ToList();
            foreach (var pawn in managedDoctors)
            {
                if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message($"Work Manager: Not assigning '{pawn.LabelShort}' as primary doctor (recovering)",
                            true);
                    }
                    continue;
                }
                if (pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor) >= maxSkillValue)
                {
                    if (doctorCount == 0 || !IsBadWork(pawn, WorkTypeDefOf.Doctor))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (highest skill value)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                        doctorCount++;
                        continue;
                    }
                }
                if (doctorCount == 0)
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (highest skill value)",
                            true);
                    }
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                    doctorCount++;
                    break;
                }
            }
            if (doctorCount == 0)
            {
                var pawn = managedDoctors.FirstOrDefault();
                if (pawn != null)
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (fail-safe)", true);
                    }
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                    doctorCount++;
                }
            }
            if (doctorCount == 1)
            {
                var doctor = doctors.First(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor));
                if (doctor.health.HasHediffsNeedingTend() || doctor.health.hediffSet.HasTendableInjury() ||
                    doctor.health.hediffSet.HasTendableHediff())
                {
                    foreach (var pawn in doctors.Intersect(_managedPawns)
                        .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == WorkTypeDefOf.Doctor)
                            .Select(pwt => pwt.Pawn)).Where(p => !IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor))
                        .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor))
                        .ThenBy(p => IsBadWork(p, WorkTypeDefOf.Doctor)))
                    {
                        if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Not assigning '{pawn.LabelShort}' as secondary doctor (recovering)",
                                    true);
                            }
                            continue;
                        }
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as secondary doctor (primary doctor needs tending)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                        doctorCount++;
                        break;
                    }
                }
            }
            if (Settings.AssignMultipleDoctors)
            {
                {
                    var patientCount = _allPawns.Count(pawn => pawn.Downed);
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message($"Work Manager: Patient count = '{patientCount}'", true);
                    }
                    while (doctorCount < patientCount)
                    {
                        var pawn = doctors.Intersect(_managedPawns)
                            .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == WorkTypeDefOf.Doctor)
                                .Select(pwt => pwt.Pawn))
                            .Where(p => !IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor) &&
                                        (!Settings.RecoveringPawnsUnfitForWork ||
                                         !HealthAIUtility.ShouldSeekMedicalRest(p)))
                            .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor))
                            .ThenBy(p => IsBadWork(p, WorkTypeDefOf.Doctor)).FirstOrDefault();
                        if (pawn == null) { break; }
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as backup doctor (multiple patients)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                        doctorCount++;
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignHunters()
        {
            if (!WorkManager.GetWorkTypeEnabled(WorkTypeDefOf.Hunting)) { return; }
            if (Settings.AllHunters)
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message("-- Work Manager: Assigning hunters... --", true);
                }
                foreach (var pawn in _capablePawns.Intersect(_managedPawns).Where(IsHunter))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message($"Work Manager: Not assigning '{pawn.LabelShort}' as a hunter (recovering)",
                                true);
                        }
                        continue;
                    }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 1 (all hunters)",
                            true);
                    }
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 1);
                }
                if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
            }
            else
            {
                if (!Settings.SpecialRulesForHunters) { return; }
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message("-- Work Manager: Assigning hunters... --", true);
                }
                var hunters = _capablePawns
                    .Where(pawn => IsHunter(pawn) || IsPawnWorkTypeActive(pawn, WorkTypeDefOf.Hunting)).ToList();
                var maxSkillValue = hunters.Any()
                    ? (int) Math.Floor(
                        hunters.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting)))
                    : 0;
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"Work Manager: Hunters are {string.Join(", ", hunters.Select(p => $"{p.LabelShortCap} ({p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting):N2})"))}",
                        true);
                    Log.Message($"Work Manager: Max hunting skill value = '{maxSkillValue}'", true);
                }
                foreach (var pawn in hunters.Intersect(_managedPawns)
                    .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == WorkTypeDefOf.Hunting)
                        .Select(pwt => pwt.Pawn)).OrderBy(p => IsBadWork(p, WorkTypeDefOf.Hunting))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting)))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message($"Work Manager: Not assigning '{pawn.LabelShort}' as a hunter (recovering)",
                                true);
                        }
                        continue;
                    }
                    if (pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting) >= maxSkillValue ||
                        _capablePawns.Count(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Hunting)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 1 (highest skill value)",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 1);
                    }
                    else
                    {
                        var minLearningRate = pawn.skills.skills.Min(s => s.LearnRateFactor());
                        var maxLearningRate = pawn.skills.skills.Max(s => s.LearnRateFactor());
                        var learningRateRange = maxLearningRate - minLearningRate;
                        if (learningRateRange < 0.1) { continue; }
                        var relevantSkills = WorkTypeDefOf.Hunting.relevantSkills;
                        if (!relevantSkills.Any()) { continue; }
                        var avgLearnRate = relevantSkills.Average(s => pawn.skills.GetSkill(s).LearnRateFactor());
                        if (avgLearnRate >= minLearningRate + learningRateRange * 0.6)
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 2 (major learning rate)",
                                    true);
                            }
                            SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 2);
                        }
                        else if (avgLearnRate >= minLearningRate + learningRateRange * 0.3)
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 3 (minor learning rate)",
                                    true);
                            }
                            SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 3);
                        }
                    }
                }
                if (_capablePawns.Count(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Hunting)) == 0)
                {
                    var pawn = _capablePawns.Intersect(_managedPawns)
                        .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == WorkTypeDefOf.Hunting)
                            .Select(pwt => pwt.Pawn)).Where(p => !p.WorkTypeIsDisabled(WorkTypeDefOf.Hunting))
                        .OrderBy(p => IsBadWork(p, WorkTypeDefOf.Hunting))
                        .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting))
                        .FirstOrDefault();
                    {
                        if (pawn != null)
                        {
                            if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                            {
                                if (Prefs.DevMode && Settings.VerboseLogging)
                                {
                                    Log.Message(
                                        $"Work Manager: Not assigning '{pawn.LabelShort}' as a hunter (recovering)",
                                        true);
                                }
                            }
                            else
                            {
                                if (Prefs.DevMode && Settings.VerboseLogging)
                                {
                                    Log.Message(
                                        $"Work Manager: Setting {pawn.LabelShort}'s priority of '{WorkTypeDefOf.Hunting.labelShort}' to {1} (fail-safe)",
                                        true);
                                }
                                SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 1);
                            }
                        }
                    }
                }
                if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
            }
        }

        private void AssignLeftoverWorkTypes()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning leftover work types... --", true);
            }
            if (!_capablePawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(o => !CommonWorkTypes.ContainsKey(o) && o != WorkTypeDefOf.Doctor)
                .ToList();
            if (Settings.SpecialRulesForHunters)
            {
                workTypes = workTypes.Except(new[] {WorkTypeDefOf.Hunting}).ToList();
            }
            var leftoverWorkTypes = workTypes.Where(w => !_capablePawns.Any(p => IsPawnWorkTypeActive(p, w)));
            foreach (var workType in leftoverWorkTypes)
            {
                var pawn = _capablePawns.Intersect(_managedPawns)
                    .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == workType)
                        .Select(pwt => pwt.Pawn))
                    .Where(p => !p.WorkTypeIsDisabled(workType) && (!Settings.RecoveringPawnsUnfitForWork ||
                                                                    !HealthAIUtility.ShouldSeekMedicalRest(p)))
                    .OrderBy(p => IsBadWork(p, workType)).ThenBy(p => workTypes.Count(w => IsPawnWorkTypeActive(p, w)))
                    .FirstOrDefault();
                if (pawn != null)
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {1}",
                            true);
                    }
                    SetPawnWorkTypePriority(pawn, workType, 1);
                }
            }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns).Where(p =>
                (!Settings.RecoveringPawnsUnfitForWork || !HealthAIUtility.ShouldSeekMedicalRest(p)) &&
                workTypes.Count(w => IsPawnWorkTypeActive(p, w)) == 0))
            {
                var workType = workTypes
                    .Except(
                        WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.Pawn == pawn).Select(pwt => pwt.WorkType))
                    .Where(w => !pawn.WorkTypeIsDisabled(w)).OrderBy(w => IsBadWork(pawn, w))
                    .ThenBy(w => _capablePawns.Count(p => IsPawnWorkTypeActive(p, w))).FirstOrDefault();
                if (workType != null)
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {1}",
                            true);
                    }
                    SetPawnWorkTypePriority(pawn, workType, 1);
                }
            }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns))
            {
                if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn)) { continue; }
                if (Settings.AllHaulers)
                {
                    var hauling = DefDatabase<WorkTypeDef>.GetNamed("Hauling");
                    if (_managedWorkTypes.Contains(hauling) && WorkManager.GetPawnWorkTypeEnabled(pawn, hauling) &&
                        !pawn.WorkTypeIsDisabled(hauling) && !IsBadWork(pawn, hauling) &&
                        !IsPawnWorkTypeActive(pawn, hauling))
                    {
                        SetPawnWorkTypePriority(pawn, hauling, Settings.AssignAllWorkTypes ? 3 : 4);
                    }
                }
                if (Settings.AllCleaners)
                {
                    var cleaning = DefDatabase<WorkTypeDef>.GetNamed("Cleaning");
                    if (_managedWorkTypes.Contains(cleaning) && WorkManager.GetPawnWorkTypeEnabled(pawn, cleaning) &&
                        !pawn.WorkTypeIsDisabled(cleaning) && !IsBadWork(pawn, cleaning) &&
                        !IsPawnWorkTypeActive(pawn, cleaning))
                    {
                        SetPawnWorkTypePriority(pawn, cleaning, Settings.AssignAllWorkTypes ? 3 : 4);
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
                    foreach (var workType in workTypes
                        .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.Pawn == pawn)
                            .Select(pwt => pwt.WorkType)).Where(w =>
                            !IsBadWork(pawn, w) && !pawn.WorkTypeIsDisabled(w) && !IsPawnWorkTypeActive(pawn, w)))
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
                if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Not assigning work by learning rates to '{pawn.LabelShort}' (recovering)",
                            true);
                    }
                    continue;
                }
                var minLearningRate = pawn.skills.skills.Min(s => s.LearnRateFactor());
                var maxLearningRate = pawn.skills.skills.Max(s => s.LearnRateFactor());
                var learningRateRange = maxLearningRate - minLearningRate;
                if (learningRateRange < 0.1) { continue; }
                var workTypes = _managedWorkTypes
                    .Except(
                        WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.Pawn == pawn).Select(pwt => pwt.WorkType))
                    .Where(w => !CommonWorkTypes.ContainsKey(w) && w != WorkTypeDefOf.Doctor &&
                                !pawn.WorkTypeIsDisabled(w) && !IsBadWork(pawn, w) && !IsPawnWorkTypeActive(pawn, w));
                if (Settings.SpecialRulesForHunters) { workTypes = workTypes.Except(new[] {WorkTypeDefOf.Hunting}); }
                foreach (var workType in workTypes)
                {
                    var relevantSkills = workType.relevantSkills;
                    if (!relevantSkills.Any()) { continue; }
                    var avgLearnRate = relevantSkills.Average(s => pawn.skills.GetSkill(s).LearnRateFactor());
                    int priority;
                    if (avgLearnRate >= minLearningRate + learningRateRange * 0.6)
                    {
                        priority = 2;
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (rate = {avgLearnRate} [{minLearningRate}, {maxLearningRate}])",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, priority);
                        continue;
                    }
                    if (avgLearnRate >= minLearningRate + learningRateRange * 0.3)
                    {
                        priority = 3;
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (rate = {avgLearnRate} [{minLearningRate}, {maxLearningRate}])",
                                true);
                        }
                        SetPawnWorkTypePriority(pawn, workType, priority);
                    }
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
        }

        private void AssignWorkersBySkill()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning workers by skill... --", true);
            }
            if (!_capablePawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(w =>
                !CommonWorkTypes.ContainsKey(w) && w != WorkTypeDefOf.Doctor && w.relevantSkills.Any());
            if (Settings.SpecialRulesForHunters) { workTypes = workTypes.Except(new[] {WorkTypeDefOf.Hunting}); }
            const int priority = 1;
            foreach (var workType in workTypes)
            {
                var relevantPawns = _capablePawns.Where(p => !p.WorkTypeIsDisabled(workType)).ToList();
                var maxSkillValue =
                    (int) Math.Floor(relevantPawns.Max(p => p.skills.AverageOfRelevantSkillsFor(workType)));
                foreach (var pawn in relevantPawns.Intersect(_managedPawns)
                    .Except(WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.WorkType == workType)
                        .Select(pwt => pwt.Pawn)).OrderBy(p => IsBadWork(p, workType))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)))
                {
                    if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Not assigning work by skill value to '{pawn.LabelShort}' (recovering)",
                                true);
                        }
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

        private void AssignWorkPriorities()
        {
            ResetWorkPriorities();
            AssignCommonWork();
            AssignDoctors();
            AssignHunters();
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
            var workTypes = _managedWorkTypes.Where(o => !CommonWorkTypes.ContainsKey(o) && o != WorkTypeDefOf.Doctor)
                .ToList();
            if (Settings.SpecialRulesForHunters)
            {
                workTypes = workTypes.Except(new[] {WorkTypeDefOf.Hunting}).ToList();
            }
            const int priority = 4;
            foreach (var pawn in pawns)
            {
                if (Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message($"Work Manager: Not assigning work to idle '{pawn.LabelShort}' (recovering)", true);
                    }
                    continue;
                }
                foreach (var workType in workTypes
                    .Except(
                        WorkManager.DisabledPawnWorkTypes.Where(pwt => pwt.Pawn == pawn).Select(pwt => pwt.WorkType))
                    .Where(w => !pawn.WorkTypeIsDisabled(w) && !IsBadWork(pawn, w) && !IsPawnWorkTypeActive(pawn, w)))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (idle)",
                            true);
                    }
                    SetPawnWorkTypePriority(pawn, workType, priority);
                }
                if (!_idlePawns.ContainsKey(pawn)) { _idlePawns.Add(pawn, new DayTime(_currentDay, _currentTime)); }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------", true); }
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
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hunting))
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message($"-- Work Manager: {pawn.LabelShort} is not a hunter (can not do hunting) --", true);
                }
                return false;
            }
            if (IsBadWork(pawn, WorkTypeDefOf.Hunting))
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message($"-- Work Manager: {pawn.LabelShort} is not a hunter (bad work type) --", true);
                }
                return false;
            }
            if (pawn.story.traits.HasTrait(TraitDefOf.Brawler) && !Settings.AllowMeleeHunters)
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message($"-- Work Manager: {pawn.LabelShort} is not a hunter (brawler trait) --", true);
                }
                return false;
            }
            if ((pawn.equipment.Primary == null || pawn.equipment.Primary.def.IsMeleeWeapon) &&
                !Settings.AllowMeleeHunters)
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message($"-- Work Manager: {pawn.LabelShort} is not a hunter (has melee weapon) --", true);
                }
                return false;
            }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"-- Work Manager: {pawn.LabelShort} is a hunter --", true);
            }
            return true;
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