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
        private readonly HashSet<Pawn> _capablePawns = new HashSet<Pawn>();

        private readonly IEnumerable<WorkTypeDef> _commonWorkTypes = new[]
        {
            DefDatabase<WorkTypeDef>.GetNamed("Firefighter"), DefDatabase<WorkTypeDef>.GetNamed("Patient"),
            DefDatabase<WorkTypeDef>.GetNamed("PatientBedRest"), DefDatabase<WorkTypeDef>.GetNamed("BasicWorker")
        };

        private readonly HashSet<Pawn> _managedPawns = new HashSet<Pawn>();
        private readonly HashSet<WorkTypeDef> _managedWorkTypes = new HashSet<WorkTypeDef>();

        private int _currentDay = -1;
        private float _currentTime = -1;

        public WorkPriorityUpdater(Map map) : base(map) { }

        private static WorkManagerGameComponent WorkManager => Current.Game.GetComponent<WorkManagerGameComponent>();

        private void AssignCommonWork()
        {
            foreach (var pawn in _managedPawns.Intersect(_capablePawns))
            {
                foreach (var workType in _managedWorkTypes.Intersect(_commonWorkTypes)
                    .Where(w => !pawn.WorkTypeIsDisabled(w))) { pawn.workSettings.SetPriority(workType, 1); }
            }
        }

        private void AssignDoctors()
        {
            if (WorkManager.DisabledWorkTypes.Contains(WorkTypeDefOf.Doctor)) { return; }
            var doctors = _capablePawns.Where(pawn => !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)).ToList();
            if (!doctors.Any()) { return; }
            var doctorCount = doctors.Count(p => p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor));
            var maxSkillValue =
                (int) Math.Floor(doctors.Max(pawn => pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor)));
            #if DEBUG
            Log.Message($"Work Manager: Max doctoring skill value = '{maxSkillValue}'", true);
            #endif
            foreach (var pawn in doctors.Intersect(_managedPawns).Where(p =>
                !p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor) &&
                p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor) >= maxSkillValue))
            {
                #if DEBUG
                Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a doctor", true);
                #endif
                pawn.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
                doctorCount++;
            }
            if (doctorCount == 1)
            {
                var doctor = doctors.First(o => o.workSettings.WorkIsActive(WorkTypeDefOf.Doctor));
                if (doctor.health.HasHediffsNeedingTend() || doctor.health.hediffSet.HasTendableInjury() ||
                    doctor.health.hediffSet.HasTendableHediff())
                {
                    var pawn = doctors.Intersect(_managedPawns)
                        .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor))
                        .FirstOrDefault(p => !p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor));
                    if (pawn != null)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Assigning '{pawn.LabelShort}' as a doctor, because primary doctor needs tending",
                            true);
                        #endif
                        pawn.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
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
                            .FirstOrDefault(p => !p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor));
                        if (pawn == null) { break; }
                        #if DEBUG
                        Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a doctor", true);
                        #endif
                        pawn.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
                        doctorCount++;
                    }
                }
            }
        }

        private void AssignHunters()
        {
            if (WorkManager.DisabledWorkTypes.Contains(WorkTypeDefOf.Hunting)) { return; }
            var hunters = _capablePawns.Where(pawn =>
                    !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hunting) &&
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
                    _capablePawns.Count(p => p.workSettings.WorkIsActive(WorkTypeDefOf.Hunting)) == 0)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 1", true);
                    #endif
                    pawn.workSettings.SetPriority(WorkTypeDefOf.Hunting, 1);
                }
                else if (pawn.skills.MaxPassionOfRelevantSkillsFor(WorkTypeDefOf.Hunting) == Passion.Major)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 2", true);
                    #endif
                    pawn.workSettings.SetPriority(WorkTypeDefOf.Hunting, 2);
                }
                else if (pawn.skills.MaxPassionOfRelevantSkillsFor(WorkTypeDefOf.Hunting) == Passion.Minor)
                {
                    #if DEBUG
                    Log.Message($"Work Manager: Assigning '{pawn.LabelShort}' as a hunter with priority 3", true);
                    #endif
                    pawn.workSettings.SetPriority(WorkTypeDefOf.Hunting, 3);
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
            if (Settings.AssignAllWorkTypes)
            {
                foreach (var pawn in _capablePawns.Intersect(_managedPawns))
                {
                    foreach (var workType in workTypes.Where(w =>
                        !pawn.WorkTypeIsDisabled(w) && !pawn.workSettings.WorkIsActive(w)))
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {4}",
                            true);
                        #endif
                        pawn.workSettings.SetPriority(workType, 4);
                    }
                }
            }
            else
            {
                var leftoverWorkTypes = workTypes.Where(w => !_capablePawns.Any(p => p.workSettings.WorkIsActive(w)));
                foreach (var workType in leftoverWorkTypes)
                {
                    var pawn = _capablePawns.Intersect(_managedPawns).Where(p => !p.WorkTypeIsDisabled(workType))
                        .OrderBy(p => workTypes.Count(w => p.workSettings.WorkIsActive(w))).FirstOrDefault();
                    if (pawn != null)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {1}",
                            true);
                        #endif
                        pawn.workSettings.SetPriority(workType, 1);
                    }
                }
                foreach (var pawn in _capablePawns.Intersect(_managedPawns)
                    .Where(p => workTypes.Count(w => p.workSettings.WorkIsActive(w)) == 0))
                {
                    var workType = workTypes.Where(w => !pawn.WorkTypeIsDisabled(w))
                        .OrderBy(w => _capablePawns.Count(p => p.workSettings.WorkIsActive(w))).FirstOrDefault();
                    if (workType != null)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {1}",
                            true);
                        #endif
                        pawn.workSettings.SetPriority(workType, 1);
                    }
                }
            }
            foreach (var pawn in _capablePawns.Intersect(_managedPawns))
            {
                if (Settings.AllHaulers)
                {
                    var hauling = DefDatabase<WorkTypeDef>.GetNamed("Hauling");
                    if (!pawn.WorkTypeIsDisabled(hauling) && !pawn.workSettings.WorkIsActive(hauling))
                    {
                        pawn.workSettings.SetPriority(hauling, Settings.AssignAllWorkTypes ? 3 : 4);
                    }
                }
                if (Settings.AllCleaners)
                {
                    var cleaning = DefDatabase<WorkTypeDef>.GetNamed("Cleaning");
                    if (!pawn.WorkTypeIsDisabled(cleaning) && !pawn.workSettings.WorkIsActive(cleaning))
                    {
                        pawn.workSettings.SetPriority(cleaning, Settings.AssignAllWorkTypes ? 3 : 4);
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
                    !pawn.WorkTypeIsDisabled(w) && !pawn.workSettings.WorkIsActive(w)))
                {
                    int priority;
                    switch (pawn.skills.MaxPassionOfRelevantSkillsFor(workType))
                    {
                        case Passion.Major:
                            priority = 2;
                            #if DEBUG
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (passion = {Passion.Major.ToString()})",
                                true);
                            #endif
                            pawn.workSettings.SetPriority(workType, priority);
                            break;
                        case Passion.Minor:
                            priority = 3;
                            #if DEBUG
                            Log.Message(
                                $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (passion = {Passion.Minor.ToString()})",
                                true);
                            #endif
                            pawn.workSettings.SetPriority(workType, priority);
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
                foreach (var pawn in relevantPawns.Intersect(_managedPawns)
                    .OrderByDescending(p => p.skills.AverageOfRelevantSkillsFor(workType)))
                {
                    if (pawn.skills.AverageOfRelevantSkillsFor(workType) >= maxSkillValue ||
                        _capablePawns.Count(p => p.workSettings.WorkIsActive(workType)) == 0)
                    {
                        #if DEBUG
                        Log.Message(
                            $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority} (skill = {pawn.skills.AverageOfRelevantSkillsFor(workType)}, max = {maxSkillValue})",
                            true);
                        #endif
                        pawn.workSettings.SetPriority(workType, priority);
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
            #endif
            var pawns = _capablePawns.Intersect(_managedPawns).Where(pawn => !pawn.Drafted && pawn.mindState.IsIdle)
                .ToList();
            if (!pawns.Any()) { return; }
            var workTypes = _managedWorkTypes.Where(o =>
                !_commonWorkTypes.Contains(o) && o != WorkTypeDefOf.Doctor && o != WorkTypeDefOf.Hunting).ToList();
            const int priority = 4;
            foreach (var pawn in pawns)
            {
                foreach (var workType in workTypes.Where(w =>
                    !pawn.WorkTypeIsDisabled(w) && !pawn.workSettings.WorkIsActive(w)))
                {
                    #if DEBUG
                    Log.Message(
                        $"Work Manager: Setting {pawn.LabelShort}'s priority of '{workType.labelShort}' to {priority}",
                        true);
                    #endif
                    pawn.workSettings.SetPriority(workType, priority);
                }
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
            if (_allPawns.Any())
            {
                AssignWorkPriorities();
                foreach (var pawn in _allPawns) { pawn.workSettings.Notify_UseWorkPrioritiesChanged(); }
            }
            _currentDay = day;
            _currentTime = hourFloat;
            #if DEBUG
            Log.Message("----------------------------------------------------", true);
            #endif
        }

        private void ResetWorkPriorities()
        {
            foreach (var pawn in _managedPawns)
            {
                #if DEBUG
                Log.Message($"Work Manager: Resetting work priorities for '{pawn.LabelShort}'", true);
                #endif
                foreach (var workType in _managedWorkTypes) { pawn.workSettings.Disable(workType); }
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
        }
    }
}