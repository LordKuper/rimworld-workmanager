using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Verse;

namespace WorkManager
{
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class WorkManagerGameComponent : GameComponent
    {
        private List<Pawn> _disabledPawns = new List<Pawn>();
        private List<Pawn> _disabledPawnSchedules = new List<Pawn>();
        private List<PawnWorkType> _disabledPawnWorkTypes = new List<PawnWorkType>();
        private List<WorkTypeDef> _disabledWorkTypes = new List<WorkTypeDef>();
        public bool PriorityManagementEnabled = true;
        public bool ScheduleManagementEnabled = true;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Game API")]
        public WorkManagerGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            _disabledPawns?.RemoveAll(pawn => pawn?.Destroyed ?? true);
            _disabledPawnSchedules?.RemoveAll(pawn => pawn?.Destroyed ?? true);
            _disabledWorkTypes?.RemoveAll(
                workType => !DefDatabase<WorkTypeDef>.AllDefsListForReading.Contains(workType));
            Scribe_Values.Look(ref PriorityManagementEnabled, nameof(PriorityManagementEnabled), true);
            Scribe_Values.Look(ref ScheduleManagementEnabled, nameof(ScheduleManagementEnabled), true);
            Scribe_Collections.Look(ref _disabledWorkTypes, "DisabledWorkTypes", LookMode.Def);
            Scribe_Collections.Look(ref _disabledPawns, "DisabledPawns", LookMode.Reference);
            Scribe_Collections.Look(ref _disabledPawnWorkTypes, "DisabledPawnWorkTypes", LookMode.Deep);
            Scribe_Collections.Look(ref _disabledPawnSchedules, "DisabledPawnSchedules", LookMode.Reference);
        }

        public bool GetPawnEnabled([NotNull] Pawn pawn)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (_disabledPawns == null) { _disabledPawns = new List<Pawn>(); }
            return !_disabledPawns.Contains(pawn);
        }

        public bool GetPawnScheduleEnabled([NotNull] Pawn pawn)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (_disabledPawnSchedules == null) { _disabledPawnSchedules = new List<Pawn>(); }
            return !_disabledPawnSchedules.Contains(pawn);
        }

        public bool GetPawnWorkTypeEnabled([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_disabledPawnWorkTypes == null) { _disabledPawnWorkTypes = new List<PawnWorkType>(); }
            return !_disabledPawnWorkTypes.Any(pwt => pwt.Pawn == pawn && pwt.WorkType == workType);
        }

        public bool GetWorkTypeEnabled([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_disabledWorkTypes == null) { _disabledWorkTypes = new List<WorkTypeDef>(); }
            return !_disabledWorkTypes.Contains(workType);
        }

        public void SetPawnEnabled([NotNull] Pawn pawn, bool enabled)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (_disabledPawns == null) { _disabledPawns = new List<Pawn>(); }
            if (enabled) { _disabledPawns.RemoveAll(p => p == pawn); }
            else
            {
                if (!_disabledPawns.Contains(pawn)) { _disabledPawns.Add(pawn); }
            }
        }

        public void SetPawnScheduleEnabled([NotNull] Pawn pawn, bool enabled)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (_disabledPawnSchedules == null) { _disabledPawnSchedules = new List<Pawn>(); }
            if (enabled) { _disabledPawnSchedules.RemoveAll(p => p == pawn); }
            else
            {
                if (!_disabledPawnSchedules.Contains(pawn)) { _disabledPawnSchedules.Add(pawn); }
            }
        }

        public void SetPawnWorkTypeEnabled([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType, bool enabled)
        {
            if (pawn == null) { throw new ArgumentNullException(nameof(pawn)); }
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_disabledPawnWorkTypes == null) { _disabledPawnWorkTypes = new List<PawnWorkType>(); }
            if (enabled) { _disabledPawnWorkTypes.RemoveAll(pwt => pwt.Pawn == pawn && pwt.WorkType == workType); }
            else
            {
                if (!_disabledPawnWorkTypes.Any(pwt => pwt.Pawn == pawn && pwt.WorkType == workType))
                {
                    _disabledPawnWorkTypes.Add(new PawnWorkType { Pawn = pawn, WorkType = workType });
                }
            }
        }

        public void SetWorkTypeEnabled([NotNull] WorkTypeDef workType, bool enabled)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_disabledWorkTypes == null) { _disabledWorkTypes = new List<WorkTypeDef>(); }
            if (enabled) { _disabledWorkTypes.RemoveAll(wt => wt == workType); }
            else
            {
                if (!_disabledWorkTypes.Contains(workType)) { _disabledWorkTypes.Add(workType); }
            }
        }
    }
}