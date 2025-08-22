using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.WorkManager.Helpers;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager;

[UsedImplicitly]
public class ScheduleUpdater(Map map) : MapComponent(map)
{
    private readonly Dictionary<Pawn, WorkShift> _workers = [];
    private RimWorldTime _scheduleUpdateTime = new(0);

    private static WorkManagerGameComponent WorkManager { get; } =
        Current.Game.GetComponent<WorkManagerGameComponent>();

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (!WorkManagerMod.Settings.ManageWorkSchedule) return;
        if (!WorkManager.ScheduleManagementEnabled) return;
        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused || Find.TickManager.TicksGame % 60 != 0) return;
        var year = GenLocalDate.Year(map);
        var day = GenLocalDate.DayOfYear(map);
        var hourFloat = GenLocalDate.HourFloat(map);
        var hoursPassed = (year - _scheduleUpdateTime.Year) * 60 * 24 + (day - _scheduleUpdateTime.Day) * 24 +
            hourFloat - _scheduleUpdateTime.Hour;
        if (hoursPassed < 24f / WorkManagerMod.Settings.ScheduleUpdateFrequency) return;
        _scheduleUpdateTime = new RimWorldTime(year, day, hourFloat);
        UpdateSchedule();
    }

    private void UpdateSchedule()
    {
#if DEBUG
        Logger.LogMessage("Updating work schedule...");
#endif
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
                    ? WorkManagerMod.Settings.NightOwlWorkShifts.Where(shift =>
                        shift.PawnThreshold <= nightOwls.Count())
                    : WorkManagerMod.Settings.ColonistWorkShifts.Where(shift =>
                        shift.PawnThreshold <= colonists.Count()))
                .ToList();
            var scores = workShifts.ToDictionary(shift => shift, _ => 0f);
            foreach (var shift in workShifts)
            {
                var shiftWorkers = _workers.Where(pair => pair.Value == shift).Select(pair => pair.Key).ToList();
                scores[shift] += shiftWorkers.Intersect(lovers).Count() * 10f;
                foreach (var workType in WorkManagerGameComponent.Instance.AllWorkTypes)
                {
                    var priority = WorkTypePriorityHelper.GetPriority(pawn, workType);
                    if (priority == 0) continue;
                    scores[shift] += 1f / priority;
                    foreach (var workerPriority in shiftWorkers
                                 .Select(worker => WorkTypePriorityHelper.GetPriority(worker, workType))
                                 .Where(workerPriority => workerPriority != 0))
                    {
                        scores[shift] -= 1f / workerPriority;
                    }
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
    }
}