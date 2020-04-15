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

        private readonly IEnumerable<WorkTypeDef> _commonWorkTypes = new[]
        {
            DefDatabase<WorkTypeDef>.GetNamed("Firefighter"), DefDatabase<WorkTypeDef>.GetNamed("Patient"),
            DefDatabase<WorkTypeDef>.GetNamed("PatientBedRest"), DefDatabase<WorkTypeDef>.GetNamed("BasicWorker")
        };

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
            foreach (var pawn in _managedPawns.Intersect(_capablePawns))
            {
                foreach (var workType in _managedWorkTypes.Intersect(_commonWorkTypes)
                    .Where(w => !pawn.WorkTypeIsDisabled(w) && !IsBadWork(pawn, w)))
                {
                    SetPawnWorkTypePriority(pawn, workType, 1);
                }
            }
        }

        private void AssignDoctors()
        {
            if (WorkManager.DisabledWorkTypes.Contains(WorkTypeDefOf.Doctor)) { return; }
            var doctors = _capablePawns.Where(pawn => !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)).ToList();
            if (!doctors.Any()) { return; }
            var doctorCount = doctors.Count(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor));
            var maxSkillValue =
                (int) Math.Floor(doctors.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor)));
            #if DEBUG
            Log.Message($"Work Manager: Max doctoring skill value = '{maxSkillValue}'", true);
            #endif
            foreach (var pawn in doctors.Intersect(_managedPawns).Where(p =>
                    !IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor) &&
                    p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor) >= maxSkillValue)
                .OrderBy(p => IsBadWork(p, WorkTypeDefOf.Doctor)))
            {
                #if DEBUG
                Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a doctor", true);
                #endif
                if (doctorCount == 0 || !IsBadWork(pawn, WorkTypeDefOf.Doctor))
                {
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                }
                doctorCount++;
            }
            if (doctorCount == 1)
            {
                var doctor = doctors.First(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor));
                if (doctor.health.HasHediffsNeedingTend() || doctor.health.hediffSet.HasTendableInjury() ||
                    doctor.health.hediffSet.HasTendableHediff())
                {
                    var pawn = doctors.Intersect(_managedPawns)
                        .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor))
                        .FirstOrDefault(p => !IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor));
                    if (pawn != null)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Assigning '{pawn.LabelShort}' as a doctor, because primary doctor needs tending",
                            true);
                        #endif
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                        doctorCount++;
                    }
                }
            }
            if (Settings.AssignMultipleDoctors)
            {
                {
                    var patientCount = _allPawns.Count(pawn => pawn.Downed);
                    #if DEBUG
                    Log.Message($"Work Manager: Patient count = '{patientCount}'", true);
                    #endif
                    while (doctorCount < patientCount)
                    {
                        var pawn = doctors.Intersect(_managedPawns)
                            .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor))
                            .FirstOrDefault(p => !IsPawnWorkTypeActive(p, WorkTypeDefOf.Doctor));
                        if (pawn == null) { break; }
                        #if DEBUG
                        Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a doctor", true);
                        #endif
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Doctor, 1);
                        doctorCount++;
                    }
                }
            }
        }

        private void AssignHunters()
        {
            if (WorkManager.DisabledWorkTypes.Contains(WorkTypeDefOf.Hunting)) { return; }
            var hunters = _capablePawns.Where(pawn =>
                    !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hunting) && !IsBadWork(pawn, WorkTypeDefOf.Hunting) &&
                    !pawn.story.traits.HasTrait(TraitDefOf.Brawler) &&
                    (pawn.skills.GetSkill(SkillDefOf.Shooting).passion >
                     pawn.skills.GetSkill(SkillDefOf.Melee).passion ||
                     pawn.skills.GetSkill(SkillDefOf.Shooting).passion ==
                     pawn.skills.GetSkill(SkillDefOf.Melee).passion &&
                     pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= pawn.skills.GetSkill(SkillDefOf.Melee).Level))
                .ToList();
            if (!hunters.Any()) { return; }
            var maxSkillValue =
                (int) Math.Floor(hunters.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting)));
            #if DEBUG
            Log.Message($"Work Manager: Max hunting skill value = '{maxSkillValue}'", true);
            #endif
            foreach (var pawn in hunters.Intersect(_managedPawns)
                .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting)))
            {
                if (pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting) >= maxSkillValue ||
                    _capablePawns.Count(p => IsPawnWorkTypeActive(p, WorkTypeDefOf.Hunting)) == 0)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 1", true);
                    #endif
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 1);
                }
                else if (pawn.skills.MaxPassionOfRelevantSkillsFor(WorkTypeDefOf.Hunting) == Passion.Major)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 2", true);
                    #endif
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 2);
                }
                else if (pawn.skills.MaxPassionOfRelevantSkillsFor(WorkTypeDefOf.Hunting) == Passion.Minor)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 3", true);
                    #endif
                    SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 3);
                }
            }
            if (hunters.Count(p => p.workSettings.WorkIsActive(WorkTypeDefOf.Hunting)) == 0)
            {
                var pawn = _capablePawns.Intersect(_managedPawns)
                    .Where(p => !p.WorkTypeIsDisabled(WorkTypeDefOf.Hunting))
                    .OrderBy(p => IsBadWork(p, WorkTypeDefOf.Hunting))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting)).FirstOrDefault();
                {
                    if (pawn != null)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{WorkTypeDefOf.Hunting.labelShort}' to {1} (skill = {pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting)}, max = {maxSkillValue})",
                            true);
                        #endif
                        SetPawnWorkTypePriority(pawn, WorkTypeDefOf.Hunting, 1);
                    }
                }
            }
        }

        private void AssignLeftoverWorkTypes()
        {
            #if DEBUG
            Log.Message("-- Work Manager: Assigning leftover work types... --", true);
            #endif
            if (!_capablePawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(o =>
                !_commonWorkTypes.Contains(o) && o != WorkTypeDefOf.Doctor && o != WorkTypeDefOf.Hunting).ToList();
            var leftoverWorkTypes = workTypes.Where(w => !_capablePawns.Any(p => IsPawnWorkTypeActive(p, w)));
            foreach (var workType in leftoverWorkTypes)
            {
                var pawn = _capablePawns.Intersect(_managedPawns).Where(p => !p.WorkTypeIsDisabled(workType))
                    .OrderBy(p => IsBadWork(p, workType)).ThenBy(p => workTypes.Count(w => IsPawnWorkTypeActive(p, w)))
                    .FirstOrDefault();
                if (pawn != null)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {1}",
                        true);
                    #endif
                    SetPawnWorkTypePriority(pawn, workType, 1);
                }
            }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns)
                .Where(p => workTypes.Count(w => IsPawnWorkTypeActive(p, w)) == 0))
            {
                var workType = workTypes.Where(w => !pawn.WorkTypeIsDisabled(w)).OrderBy(w => IsBadWork(pawn, w))
                    .ThenBy(w => _capablePawns.Count(p => IsPawnWorkTypeActive(p, w))).FirstOrDefault();
                if (workType != null)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {1}",
                        true);
                    #endif
                    SetPawnWorkTypePriority(pawn, workType, 1);
                }
            }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns))
            {
                if (Settings.AllHaulers)
                {
                    var hauling = DefDatabase<WorkTypeDef>.GetNamed("Hauling");
                    if (_managedWorkTypes.Contains(hauling) && !pawn.WorkTypeIsDisabled(hauling) &&
                        !IsBadWork(pawn, hauling) && !IsPawnWorkTypeActive(pawn, hauling))
                    {
                        SetPawnWorkTypePriority(pawn, hauling, Settings.AssignAllWorkTypes ? 3 : 4);
                    }
                }
                if (Settings.AllCleaners)
                {
                    var cleaning = DefDatabase<WorkTypeDef>.GetNamed("Cleaning");
                    if (_managedWorkTypes.Contains(cleaning) && !pawn.WorkTypeIsDisabled(cleaning) &&
                        !IsBadWork(pawn, cleaning) && !IsPawnWorkTypeActive(pawn, cleaning))
                    {
                        SetPawnWorkTypePriority(pawn, cleaning, Settings.AssignAllWorkTypes ? 3 : 4);
                    }
                }
            }
            if (Settings.AssignAllWorkTypes)
            {
                foreach (var pawn in _capablePawns.Intersect(_managedPawns))
                {
                    foreach (var workType in workTypes.Where(w => !IsBadWork(pawn, w) &&
                                                                  !pawn.WorkTypeIsDisabled(w) &&
                                                                  !IsPawnWorkTypeActive(pawn, w)))
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {4}",
                            true);
                        #endif
                        SetPawnWorkTypePriority(pawn, workType, 4);
                    }
                }
            }
            #if DEBUG
            Log.Message("---------------------", true);
            #endif
        }

        private void AssignWorkersByPassion()
        {
            #if DEBUG
            Log.Message("-- Work Manager: Assigning workers by passion... --", true);
            #endif
            if (!_capablePawns.Any()) { return; }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns))
            {
                foreach (var workType in _managedWorkTypes.Where(w =>
                    !_commonWorkTypes.Contains(w) && w != WorkTypeDefOf.Doctor && w != WorkTypeDefOf.Hunting &&
                    !pawn.WorkTypeIsDisabled(w) && !IsBadWork(pawn, w) && !IsPawnWorkTypeActive(pawn, w)))
                {
                    int priority;
                    switch (pawn.skills.MaxPassionOfRelevantSkillsFor(workType))
                    {
                        case Passion.Major:
                            priority = 2;
                            #if DEBUG
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (passion = {Passion.Major})",
                                true);
                            #endif
                            SetPawnWorkTypePriority(pawn, workType, priority);
                            break;
                        case Passion.Minor:
                            priority = 3;
                            #if DEBUG
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (passion = {Passion.Minor})",
                                true);
                            #endif
                            SetPawnWorkTypePriority(pawn, workType, priority);
                            break;
                    }
                }
            }
            #if DEBUG
            Log.Message("---------------------", true);
            #endif
        }

        private void AssignWorkersBySkill()
        {
            #if DEBUG
            Log.Message("-- Work Manager: Assigning workers by skill... --", true);
            #endif
            if (!_capablePawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(w =>
                !_commonWorkTypes.Contains(w) && w != WorkTypeDefOf.Doctor && w != WorkTypeDefOf.Hunting &&
                w.relevantSkills.Any());
            const int priority = 1;
            foreach (var workType in workTypes)
            {
                var relevantPawns = _capablePawns.Where(p => !p.WorkTypeIsDisabled(workType)).ToList();
                var maxSkillValue =
                    (int) Math.Floor(relevantPawns.Max(p => p.skills.AverageOfRelevantSkillsFor(workType)));
                foreach (var pawn in relevantPawns.Intersect(_managedPawns).OrderBy(p => IsBadWork(p, workType))
                    .ThenByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)))
                {
                    if (!IsBadWork(pawn, workType) &&
                        pawn.skills.AverageOfRelevantSkillsFor(workType) >= maxSkillValue ||
                        _capablePawns.Count(p => IsPawnWorkTypeActive(p, workType)) == 0)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (skill = {pawn.skills.AverageOfRelevantSkillsFor(workType)}, max = {maxSkillValue})",
                            true);
                        #endif
                        SetPawnWorkTypePriority(pawn, workType, priority);
                    }
                }
            }
            #if DEBUG
            Log.Message("---------------------", true);
            #endif
        }

        private void AssignWorkForIdlePawns()
        {
            #if DEBUG
            Log.Message("-- Work Manager: Assigning work for idle pawns... --", true);
            foreach (var idlePawn in _idlePawns)
            {
                Log.Message(
                    $"{idlePawn.Key.LabelShort} is registered as idle ({idlePawn.Value.Day}, {idlePawn.Value.Hour:N1})",
                    true);
            }
            #endif
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
                !_commonWorkTypes.Contains(o) && o != WorkTypeDefOf.Doctor && o != WorkTypeDefOf.Hunting).ToList();
            const int priority = 4;
            foreach (var pawn in pawns)
            {
                foreach (var workType in workTypes.Where(w => !pawn.WorkTypeIsDisabled(w) &&
                                                              !IsBadWork(pawn, w) && !IsPawnWorkTypeActive(pawn, w)))
                {
                    #if DEBUG
                    Log.Message(
                        $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (idle)",
                        true);
                    #endif
                    SetPawnWorkTypePriority(pawn, workType, priority);
                }
                if (!_idlePawns.ContainsKey(pawn)) { _idlePawns.Add(pawn, new DayTime(_currentDay, _currentTime)); }
            }
            #if DEBUG
            Log.Message("---------------------", true);
            #endif
        }

        private void AssignWorkPriorities()
        {
            ResetWorkPriorities();
            AssignCommonWork();
            AssignDoctors();
            AssignHunters();
            AssignWorkersBySkill();
            AssignWorkersByPassion();
            AssignLeftoverWorkTypes();
            AssignWorkForIdlePawns();
            ApplyWorkPriorities();
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        private static bool IsBadWork(Pawn pawn, WorkTypeDef workType)
        {
            if (Settings.IsBadWorkMethod == null) { return false; }
            return (bool) Settings.IsBadWorkMethod.Invoke(null, new object[] {pawn, workType});
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
            #if DEBUG
            Log.Message($"----- Work Manager: Updating work priorities... (day = {day}, hour = {hourFloat}) -----",
                true);
            #endif
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
            #if DEBUG
            Log.Message("----------------------------------------------------", true);
            #endif
        }

        private void ResetWorkPriorities()
        {
            foreach (var pawn in _allPawns)
            {
                var workPriorities = new List<WorkPriority>();
                if (_managedPawns.Contains(pawn))
                {
                    workPriorities.AddRange(_allWorkTypes.Select(workType =>
                        _managedWorkTypes.Contains(workType)
                            ? new WorkPriority(workType, 0)
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
            _managedPawns.AddRange(_allPawns.Where(p => !WorkManager.DisabledPawns.Contains(p)));
            if (!_allWorkTypes.Any())
            {
                _allWorkTypes.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible));
            }
            _managedWorkTypes.Clear();
            _managedWorkTypes.AddRange(_allWorkTypes.Where(w => !WorkManager.DisabledWorkTypes.Contains(w)));
            _pawnWorkPriorities.Clear();
        }
    }
}