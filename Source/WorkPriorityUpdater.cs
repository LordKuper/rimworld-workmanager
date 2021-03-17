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
        private readonly Dictionary<Pawn, DayTime> _idlePawns = new Dictionary<Pawn, DayTime>();

        private readonly DayTime _updateDayTime = new DayTime(-1, -1);

        private readonly Dictionary<Pawn, Dictionary<WorkTypeDef, int>> _workPriorities =
            new Dictionary<Pawn, Dictionary<WorkTypeDef, int>>();

        public WorkPriorityUpdater(Map map) : base(map) { }
        private IEnumerable<Pawn> AllPawns => map.mapPawns.FreeColonistsSpawned;

        private static IEnumerable<WorkTypeDef> AllWorkTypes =>
            DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible);

        private static IEnumerable<WorkTypeDef> ManagedWorkTypes =>
            AllWorkTypes.Where(w => WorkManager.GetWorkTypeEnabled(w));

        private static WorkManagerGameComponent WorkManager { get; } =
            Current.Game.GetComponent<WorkManagerGameComponent>();

        private void ApplyWorkPriorities()
        {
            foreach (var pawn in AllPawns.Where(IsManaged))
            {
                foreach (var workType in ManagedWorkTypes.Where(workType => IsManaged(pawn, workType)))
                {
                    pawn.workSettings.SetPriority(workType, _workPriorities[pawn][workType]);
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
                .Select(wt => wt.WorkTypeDef).Intersect(ManagedWorkTypes);
            foreach (var workType in relevantWorkTypes)
            {
                foreach (var pawn in AllPawns.Where(pawn =>
                    IsManaged(pawn) && IsCapable(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                    !pawn.WorkTypeIsDisabled(workType) && !IsBadWork(pawn, workType)))
                {
                    _workPriorities[pawn][workType] = Settings.AssignEveryoneWorkTypes
                        .First(wt => wt.WorkTypeDef == workType).Priority;
                }
            }
        }

        private void AssignDedicatedWorkers()
        {
            if (!Settings.UseDedicatedWorkers) { return; }
            var capablePawns = AllPawns.Where(IsCapable).ToList();
            if (!capablePawns.Any()) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning dedicated workers --");
            }
            var workTypes = AllWorkTypes.Intersect(ManagedWorkTypes).Where(wt =>
                    Settings.AssignEveryoneWorkTypes.FirstOrDefault(a => a.WorkTypeDef == wt)?.AllowDedicated ?? true)
                .ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!workTypes.Any()) { return; }
            var targetWorkers = (int) Math.Ceiling((float) capablePawns.Count / workTypes.Count);
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"-- Work Manager: Target dedicated workers by work type = {targetWorkers} --");
            }
            foreach (var workType in workTypes.OrderByDescending(wt => wt.relevantSkills.Count)
                .ThenByDescending(wt => wt.naturalPriority))
            {
                if (capablePawns.Count(pawn => _workPriorities[pawn][workType] == 1) >= targetWorkers) { continue; }
                var relevantPawns = capablePawns.Where(pawn => IsManaged(pawn) &&
                                                               !IsRecovering(pawn) &&
                                                               !pawn.WorkTypeIsDisabled(workType) &&
                                                               !IsBadWork(pawn, workType)).ToList();
                if (!relevantPawns.Any()) { continue; }
                var pawnSkills = relevantPawns.ToDictionary(pawn => pawn,
                    pawn => workType.relevantSkills.Any()
                        ? (int) Math.Floor(workType.relevantSkills.Select(skill => pawn.skills.GetSkill(skill).Level)
                            .Average())
                        : 0);
                var skillRange = pawnSkills.Max(pair => pair.Value) - pawnSkills.Min(pair => pair.Value);
                var pawnDedicationsCounts = relevantPawns.ToDictionary(pawn => pawn,
                    pawn => workTypes.Count(wt => _workPriorities[pawn][wt] == 1));
                var dedicationsCountRange = pawnDedicationsCounts.Max(pair => pair.Value) -
                                            pawnDedicationsCounts.Min(pair => pair.Value);
                var pawnScores = new Dictionary<Pawn, float>();
                foreach (var pawn in relevantPawns)
                {
                    var skill = pawnSkills[pawn];
                    var normalizedSkill = skillRange == 0 ? 0 : skill / skillRange;
                    var normalizedLearnRate = IsLearningRateAboveThreshold(pawn, workType, true) ? 1f :
                        IsLearningRateAboveThreshold(pawn, workType, false) ? 0.5f : 0f;
                    var normalizedDedications = dedicationsCountRange == 0
                        ? 0
                        : pawnDedicationsCounts[pawn] / dedicationsCountRange;
                    var score = (float) normalizedSkill - normalizedDedications;
                    score += skill < 20 ? 0.75f * normalizedLearnRate : 0.25f * normalizedLearnRate;
                    pawnScores.Add(pawn, score);
                }
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"-- Work Manager: {string.Join(", ", pawnScores.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key.LabelShort}({pair.Value:N2})"))} --");
                }
                while (capablePawns.Count(pawn => _workPriorities[pawn][workType] == 1) < targetWorkers)
                {
                    var dedicatedWorker = pawnScores.Any()
                        ? pawnScores.OrderByDescending(pair => pair.Value).First().Key
                        : null;
                    if (dedicatedWorker == null) { break; }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{dedicatedWorker.LabelShort}' as dedicated worker for '{workType.labelShort}'");
                    }
                    _workPriorities[dedicatedWorker][workType] = 1;
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
            var workType = AllWorkTypes.FirstOrDefault(workTypeDef =>
                "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null) { return; }
            if (!WorkManager.GetWorkTypeEnabled(workType)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("-- Work Manager: Assigning doctors... --"); }
            var doctors = AllPawns.Where(pawn => IsCapable(pawn) && !pawn.WorkTypeIsDisabled(workType)).ToList();
            if (!doctors.Any()) { return; }
            var doctorsCount = doctors.Count(pawn => IsActive(pawn, workType));
            var maxSkillValue = doctors.Max(pawn => GetWorkSkillLevel(pawn, workType));
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message($"Work Manager: Max doctoring skill value = '{maxSkillValue}'");
            }
            var assignEveryone = Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            var managedDoctors = doctors.Where(pawn => IsManaged(pawn) && IsManaged(pawn, workType))
                .OrderBy(pawn => IsBadWork(pawn, workType)).ThenByDescending(pawn => GetWorkSkillLevel(pawn, workType))
                .ToList();
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var pawn in managedDoctors.Where(pawn => !IsRecovering(pawn)))
                {
                    if (GetWorkSkillLevel(pawn, workType) >= maxSkillValue)
                    {
                        if (doctorsCount == 0 || !IsBadWork(pawn, workType))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (highest skill value)");
                            }
                            _workPriorities[pawn][workType] = 1;
                            doctorsCount++;
                            continue;
                        }
                    }
                    if (doctorsCount == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (highest skill value)");
                        }
                        _workPriorities[pawn][workType] = 1;
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
                        Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as primary doctor (fail-safe)");
                    }
                    _workPriorities[pawn][workType] = assignEveryone == null || assignEveryone.AllowDedicated
                        ? 1
                        : assignEveryone.Priority;
                    doctorsCount++;
                }
            }
            if (doctorsCount == 1)
            {
                var doctor = doctors.First(pawn => IsActive(pawn, workType));
                if (doctor.health.HasHediffsNeedingTend() || doctor.health.hediffSet.HasTendableInjury() ||
                    doctor.health.hediffSet.HasTendableHediff())
                {
                    foreach (var pawn in doctors
                        .Where(pawn =>
                            IsManaged(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                            !IsActive(pawn, workType)).OrderByDescending(pawn => GetWorkSkillLevel(pawn, workType))
                        .ThenBy(pawn => IsBadWork(pawn, workType)))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{pawn.LabelShort}' as secondary doctor (primary doctor needs tending)");
                        }
                        _workPriorities[pawn][workType] = assignEveryone == null || assignEveryone.AllowDedicated
                            ? 1
                            : assignEveryone.Priority;
                        doctorsCount++;
                        break;
                    }
                }
            }
            if (Settings.AssignMultipleDoctors && (assignEveryone == null || assignEveryone.AllowDedicated))
            {
                var patients = new List<Pawn>();
                if (Settings.CountDownedColonists) { patients.AddRange(AllPawns.Where(pawn => pawn.Downed)); }
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
                while (doctorsCount < patients.Count)
                {
                    var doctor = doctors
                        .Where(pawn =>
                            IsManaged(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                            !IsActive(pawn, workType)).OrderByDescending(pawn => GetWorkSkillLevel(pawn, workType))
                        .ThenBy(pawn => IsBadWork(pawn, workType)).FirstOrDefault();
                    if (doctor == null) { break; }
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Assigning '{doctor.LabelShort}' as backup doctor (multiple patients)");
                    }
                    _workPriorities[doctor][workType] = 1;
                    doctorsCount++;
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        private void AssignHunters()
        {
            if (!Settings.SpecialRulesForHunters) { return; }
            var workType = AllWorkTypes.FirstOrDefault(workTypeDef =>
                "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase));
            if (workType == null) { return; }
            if (!ManagedWorkTypes.Contains(workType)) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("-- Work Manager: Assigning hunters... --"); }
            var assignEveryone = Settings.AssignEveryoneWorkTypes.FirstOrDefault(wt => wt.WorkTypeDef == workType);
            if (assignEveryone != null)
            {
                foreach (var pawn in AllPawns.Where(pawn =>
                    IsCapable(pawn) && IsManaged(pawn) && IsManaged(pawn, workType) && IsActive(pawn, workType) &&
                    !IsHunter(pawn)))
                {
                    if (Prefs.DevMode && Settings.VerboseLogging)
                    {
                        Log.Message(
                            $"Work Manager: Removing hunting assignment from '{pawn.LabelShort}' (not a hunter)");
                    }
                    _workPriorities[pawn][workType] = 0;
                }
                if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
            }
            var hunters = AllPawns.Where(pawn => IsCapable(pawn) && (IsHunter(pawn) || IsActive(pawn, workType)))
                .ToList();
            var maxSkillValue = hunters.Any() ? hunters.Max(pawn => GetWorkSkillLevel(pawn, workType)) : 0;
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"Work Manager: Hunters are {string.Join(", ", hunters.Select(pawn => $"{pawn.LabelShortCap} ({GetWorkSkillLevel(pawn, workType):N2})"))}");
                Log.Message($"Work Manager: Max hunting skill value = '{maxSkillValue}'");
            }
            if (assignEveryone == null || assignEveryone.AllowDedicated)
            {
                foreach (var hunter in hunters
                    .Where(pawn =>
                        IsManaged(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                        !IsBadWork(pawn, workType)).OrderByDescending(pawn => GetWorkSkillLevel(pawn, workType)))
                {
                    if (GetWorkSkillLevel(hunter, workType) >= maxSkillValue ||
                        AllPawns.Count(pawn => IsCapable(pawn) && IsActive(pawn, workType)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Assigning '{hunter.LabelShort}' as a hunter with priority 1 (highest skill value)");
                        }
                        _workPriorities[hunter][workType] = 1;
                    }
                    else
                    {
                        if (IsLearningRateAboveThreshold(hunter, workType, true))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{hunter.LabelShort}' as a hunter with priority 2 (major learning rate)");
                            }
                            _workPriorities[hunter][workType] = 2;
                        }
                        else if (IsLearningRateAboveThreshold(hunter, workType, false))
                        {
                            if (Prefs.DevMode && Settings.VerboseLogging)
                            {
                                Log.Message(
                                    $"Work Manager: Assigning '{hunter.LabelShort}' as a hunter with priority 3 (minor learning rate)");
                            }
                            _workPriorities[hunter][workType] = 3;
                        }
                    }
                }
            }
            if (AllPawns.Count(pawn => IsCapable(pawn) && IsActive(pawn, workType)) == 0)
            {
                var hunter = AllPawns
                    .Where(pawn =>
                        IsCapable(pawn) && IsManaged(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                        !pawn.WorkTypeIsDisabled(workType) && !IsBadWork(pawn, workType))
                    .OrderByDescending(pawn => GetWorkSkillLevel(pawn, workType)).FirstOrDefault();
                {
                    if (hunter != null)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {hunter.LabelShort}'s priority of '{workType.labelShort}' to {(assignEveryone == null || assignEveryone.AllowDedicated ? 1 : assignEveryone.Priority)} (fail-safe)");
                        }
                        _workPriorities[hunter][workType] = assignEveryone == null || assignEveryone.AllowDedicated
                            ? 1
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
            if (!AllPawns.Any(IsCapable)) { return; }
            var workTypes = ManagedWorkTypes.Where(workType =>
                !Settings.AssignEveryoneWorkTypes.Any(a => a.WorkTypeDef == workType)).ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (!Settings.UseDedicatedWorkers)
            {
                foreach (var workType in workTypes.Where(workType =>
                    !AllPawns.Where(IsCapable).Any(pawn => IsActive(pawn, workType))))
                {
                    foreach (var pawn in AllPawns
                        .Where(pawn =>
                            IsCapable(pawn) && IsManaged(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                            !pawn.WorkTypeIsDisabled(workType) && !IsBadWork(pawn, workType))
                        .OrderBy(pawn => workTypes.Count(wt => IsActive(pawn, wt))))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to 1");
                        }
                        _workPriorities[pawn][workType] = 1;
                        break;
                    }
                }
                foreach (var pawn in AllPawns.Where(pawn =>
                    IsCapable(pawn) && IsManaged(pawn) && !IsRecovering(pawn) &&
                    workTypes.Count(wt => IsActive(pawn, wt)) == 0))
                {
                    var workType = workTypes
                        .Where(wt => IsManaged(pawn, wt) && !pawn.WorkTypeIsDisabled(wt) && !IsBadWork(pawn, wt))
                        .OrderBy(wt => AllPawns.Where(IsCapable).Count(p => IsActive(p, wt))).FirstOrDefault();
                    if (workType != null)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to 1");
                        }
                        _workPriorities[pawn][workType] = 1;
                    }
                }
            }
            if (Settings.AssignAllWorkTypes)
            {
                foreach (var pawn in AllPawns.Where(pawn => IsCapable(pawn) && IsManaged(pawn) && !IsRecovering(pawn)))
                {
                    foreach (var workType in workTypes.Where(wt =>
                        IsManaged(pawn, wt) && !IsBadWork(pawn, wt) && !pawn.WorkTypeIsDisabled(wt) &&
                        !IsActive(pawn, wt)))
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to 4");
                        }
                        _workPriorities[pawn][workType] = 4;
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
            if (!AllPawns.Any(IsCapable)) { return; }
            foreach (var pawn in AllPawns.Where(pawn => IsCapable(pawn) && IsManaged(pawn) && !IsRecovering(pawn)))
            {
                var workTypes = ManagedWorkTypes.Except(Settings.AssignEveryoneWorkTypes.Select(wt => wt.WorkTypeDef))
                    .Where(workType => IsManaged(pawn, workType) && !pawn.WorkTypeIsDisabled(workType) &&
                                       !IsBadWork(pawn, workType) && !IsActive(pawn, workType)).ToList();
                if (Settings.SpecialRulesForDoctors)
                {
                    workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                        "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                if (Settings.SpecialRulesForHunters)
                {
                    workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                        "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
                }
                foreach (var workType in workTypes)
                {
                    if (IsLearningRateAboveThreshold(pawn, workType, true))
                    {
                        _workPriorities[pawn][workType] = 2;
                        continue;
                    }
                    if (IsLearningRateAboveThreshold(pawn, workType, false)) { _workPriorities[pawn][workType] = 3; }
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
            if (!AllPawns.Any(IsCapable)) { return; }
            var workTypes = ManagedWorkTypes.Where(w =>
                !Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == w) && w.relevantSkills.Any()).ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var workType in workTypes)
            {
                var relevantPawns = AllPawns.Where(pawn => IsCapable(pawn) && !pawn.WorkTypeIsDisabled(workType))
                    .ToList();
                if (!relevantPawns.Any()) { continue; }
                var maxSkillValue = relevantPawns.Max(pawn => GetWorkSkillLevel(pawn, workType));
                foreach (var pawn in relevantPawns
                    .Where(pawn =>
                        IsManaged(pawn) && !IsRecovering(pawn) && IsManaged(pawn, workType) &&
                        !IsBadWork(pawn, workType)).OrderByDescending(pawn => GetWorkSkillLevel(pawn, workType)))
                {
                    if (GetWorkSkillLevel(pawn, workType) >= maxSkillValue || AllPawns.Where(IsCapable)
                        .Count(p => IsActive(p, workType)) == 0)
                    {
                        if (Prefs.DevMode && Settings.VerboseLogging)
                        {
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to 1 (skill = {GetWorkSkillLevel(pawn, workType)}, max = {maxSkillValue})");
                        }
                        _workPriorities[pawn][workType] = 1;
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
            var relevantWorkTypes = AllWorkTypes.Where(wt => new[] {"Patient", "PatientBedRest"}.Contains(wt.defName))
                .Intersect(ManagedWorkTypes);
            foreach (var workType in relevantWorkTypes)
            {
                foreach (var pawn in AllPawns.Where(pawn =>
                    IsManaged(pawn) && IsCapable(pawn) && !pawn.WorkTypeIsDisabled(workType) &&
                    !IsBadWork(pawn, workType))) { _workPriorities[pawn][workType] = 1; }
            }
        }

        private void AssignWorkPriorities()
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

        private void AssignWorkToIdlePawns()
        {
            if (!Settings.AssignWorkToIdlePawns) { return; }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("-- Work Manager: Assigning work for idle pawns... --");
                foreach (var idlePawn in _idlePawns)
                {
                    Log.Message(
                        $"{idlePawn.Key.LabelShort} is registered as idle ({idlePawn.Value.Day}, {idlePawn.Value.Hour:N1})");
                }
            }
            var noLongerIdlePawns = _idlePawns.Where(ip =>
                (_updateDayTime.Day - ip.Value.Day) * 24 + _updateDayTime.Hour - ip.Value.Hour > 12).ToList();
            foreach (var ip in noLongerIdlePawns) { _idlePawns.Remove(ip.Key); }
            var idlePawns = AllPawns.Where(pawn =>
                IsCapable(pawn) && IsManaged(pawn) && !IsRecovering(pawn) &&
                (_idlePawns.ContainsKey(pawn) || !pawn.Drafted && pawn.mindState.IsIdle)).ToList();
            if (!idlePawns.Any()) { return; }
            var workTypes = ManagedWorkTypes
                .Where(o => !Settings.AssignEveryoneWorkTypes.Any(wt => wt.WorkTypeDef == o)).ToList();
            if (Settings.SpecialRulesForDoctors)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Doctor".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            if (Settings.SpecialRulesForHunters)
            {
                workTypes.Remove(AllWorkTypes.FirstOrDefault(workTypeDef =>
                    "Hunting".Equals(workTypeDef.defName, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var pawn in idlePawns)
            {
                foreach (var workType in workTypes.Where(wt =>
                    IsManaged(pawn, wt) && !pawn.WorkTypeIsDisabled(wt) && !IsBadWork(pawn, wt) && !IsActive(pawn, wt)))
                {
                    _workPriorities[pawn][workType] = 4;
                }
                if (!_idlePawns.ContainsKey(pawn))
                {
                    _idlePawns.Add(pawn, new DayTime(_updateDayTime.Day, _updateDayTime.Hour));
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging) { Log.Message("---------------------"); }
        }

        private int GetWorkSkillLevel([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            return workType.relevantSkills.Any()
                ? (int) Math.Floor(workType.relevantSkills.Select(skill => pawn.skills.GetSkill(skill).Level).Average())
                : 0;
        }

        private bool IsActive([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            return _workPriorities[pawn][workType] > 0;
        }

        private static bool IsBadWork([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            return Settings.IsBadWorkMethod != null &&
                   (bool) Settings.IsBadWorkMethod.Invoke(null, new object[] {pawn, workType});
        }

        private static bool IsCapable([NotNull] Pawn pawn)
        {
            return !pawn.Dead && !pawn.Downed && !pawn.InMentalState;
        }

        private bool IsHunter([NotNull] Pawn pawn)
        {
            return !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hunting) && !IsBadWork(pawn, WorkTypeDefOf.Hunting) &&
                   (Settings.AllowMeleeHunters || !pawn.story.traits.HasTrait(TraitDefOf.Brawler)) &&
                   (Settings.AllowMeleeHunters ||
                    pawn.equipment.Primary != null && !pawn.equipment.Primary.def.IsMeleeWeapon);
        }

        private static bool IsLearningRateAboveThreshold([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType,
            bool majorThreshold)
        {
            var threshold = majorThreshold ? Settings.MajorLearningRateThreshold : Settings.MinorLearningRateThreshold;
            var learnRate = workType.relevantSkills.Any()
                ? workType.relevantSkills.Select(skill => pawn.skills.GetSkill(skill).LearnRateFactor()).Average()
                : 0;
            if (!Settings.UsePawnLearningRateThresholds) { return learnRate >= threshold; }
            var learningRates = DefDatabase<SkillDef>.AllDefsListForReading
                .Select(skill => pawn.skills.GetSkill(skill).LearnRateFactor()).ToList();
            var range = learningRates.Max() - learningRates.Min();
            if (range < 0.01) { return false; }
            return learnRate >= learningRates.Min() + range * threshold;
        }

        private static bool IsManaged(Pawn pawn)
        {
            return WorkManager.GetPawnEnabled(pawn);
        }

        private static bool IsManaged([NotNull] Pawn pawn, WorkTypeDef workType)
        {
            return WorkManager.GetPawnEnabled(pawn) && WorkManager.GetWorkTypeEnabled(workType) &&
                   WorkManager.GetPawnWorkTypeEnabled(pawn, workType);
        }

        private static bool IsRecovering([NotNull] Pawn pawn)
        {
            return IsCapable(pawn) && Settings.RecoveringPawnsUnfitForWork &&
                   HealthAIUtility.ShouldSeekMedicalRest(pawn);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!WorkManager.Enabled) { return; }
            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused) { return; }
            if ((Find.TickManager.TicksGame + GetHashCode()) % 60 != 0) { return; }
            var day = GenLocalDate.DayOfYear(map);
            var hourFloat = GenLocalDate.HourFloat(map);
            var hoursPassed = (day - _updateDayTime.Day) * 24 + hourFloat - _updateDayTime.Hour;
            if (Settings.UpdateFrequency == 0) { Settings.UpdateFrequency = 24; }
            if (hoursPassed < 24f / Settings.UpdateFrequency) { return; }
            if (!Current.Game.playSettings.useWorkPriorities)
            {
                Current.Game.playSettings.useWorkPriorities = true;
                foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                    pawn.Faction == Faction.OfPlayer)) { pawn.workSettings?.Notify_UseWorkPrioritiesChanged(); }
            }
            if (Settings.AssignEveryoneWorkTypes == null)
            {
                Settings.AssignEveryoneWorkTypes =
                    new List<AssignEveryoneWorkType>(Settings.DefaultAssignEveryoneWorkTypes);
            }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"----- Work Manager: Updating work priorities... (day = {day}, hour = {hourFloat}, passed = {hoursPassed:N1}) -----");
            }
            _updateDayTime.Day = day;
            _updateDayTime.Hour = hourFloat;
            ResetWorkPriorities();
            AssignWorkPriorities();
            ApplyWorkPriorities();
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----------------------------------------------------");
            }
        }

        private void ResetWorkPriorities()
        {
            _workPriorities.Clear();
            foreach (var pawn in AllPawns)
            {
                _workPriorities.Add(pawn, new Dictionary<WorkTypeDef, int>());
                foreach (var workType in AllWorkTypes)
                {
                    _workPriorities[pawn].Add(workType,
                        IsManaged(pawn, workType) ? 0 : pawn.workSettings.GetPriority(workType));
                }
            }
        }
    }
}