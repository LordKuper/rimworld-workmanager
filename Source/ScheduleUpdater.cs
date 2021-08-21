using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace WorkManager
{
    [UsedImplicitly]
    public class ScheduleUpdater : MapComponent
    {
        private readonly RimworldTime _scheduleUpdateTime = new RimworldTime(-1, -1, -1);

        private readonly Dictionary<Pawn, WorkShift> _workers = new Dictionary<Pawn, WorkShift>();

        public ScheduleUpdater(Map map) : base(map) { }

        private static WorkManagerGameComponent WorkManager { get; } =
            Current.Game.GetComponent<WorkManagerGameComponent>();

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!Settings.ManageWorkSchedule) { return; }
            if (!WorkManager.ScheduleManagementEnabled) { return; }
            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused || Find.TickManager.TicksGame % 60 != 0) { return; }
            if (!Settings.Initialized) { Settings.Initialize(); }
            var year = GenLocalDate.Year(map);
            var day = GenLocalDate.DayOfYear(map);
            var hourFloat = GenLocalDate.HourFloat(map);
            var hoursPassed = (year - _scheduleUpdateTime.Year) * 60 * 24 + (day - _scheduleUpdateTime.Day) * 24 +
                hourFloat - _scheduleUpdateTime.Hour;
            if (hoursPassed < 24f / Settings.UpdateFrequency) { return; }
            _scheduleUpdateTime.Year = year;
            _scheduleUpdateTime.Day = day;
            _scheduleUpdateTime.Hour = hourFloat;
            UpdateSchedule();
        }

        private void UpdateSchedule()
        {
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----- Work Manager: Updating work schedule... -----");
            }
            _workers.Clear();
            var allPawns = map.mapPawns.FreeColonistsSpawned;
            var colonists = allPawns.Where(pawn => !pawn.story.traits.HasTrait(TraitDef.Named("NightOwl")));
            var nightOwls = allPawns.Where(pawn => pawn.story.traits.HasTrait(TraitDef.Named("NightOwl")));
            foreach (var pawn in allPawns.Where(pawn => WorkManager.GetPawnScheduleEnabled(pawn)))
            {
                var lovers = pawn.relations.DirectRelations.Where(relation =>
                    new[] { PawnRelationDefOf.Fiance, PawnRelationDefOf.Lover, PawnRelationDefOf.Spouse }.Contains(
                        relation.def)).Select(relation => relation.otherPawn).Distinct().ToList();
                var workShifts = (pawn.story.traits.HasTrait(TraitDef.Named("NightOwl"))
                    ? Settings.NightOwlWorkShifts.Where(shift => shift.PawnThreshold <= nightOwls.Count())
                    : Settings.ColonistWorkShifts.Where(shift => shift.PawnThreshold <= colonists.Count())).ToList();
                var scores = workShifts.ToDictionary(shift => shift, shift => 0f);
                foreach (var shift in workShifts)
                {
                    var shiftWorkers = _workers.Where(pair => pair.Value == shift).Select(pair => pair.Key).ToList();
                    scores[shift] += shiftWorkers.Intersect(lovers).Count() * 10f;
                    foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible))
                    {
                        var priority = PawnCache.GetWorkTypePriority(pawn, workType);
                        if (priority == 0) { continue; }
                        scores[shift] += 1f / priority;
                        foreach (var workerPriority in shiftWorkers
                            .Select(worker => PawnCache.GetWorkTypePriority(worker, workType))
                            .Where(workerPriority => workerPriority != 0)) { scores[shift] -= 1f / workerPriority; }
                    }
                }
                var workShift = scores.OrderByDescending(shiftScore => shiftScore.Value).First().Key;
                _workers.Add(pawn, workShift);
            }
            foreach (var worker in _workers)
            {
                for (var hour = 0; hour < 24; hour++)
                {
                    worker.Key.timetable.SetAssignment(hour, worker.Value.GetTimeAssignment(hour));
                }
            }
            if (Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message("----------------------------------------------------");
            }
        }
    }
}