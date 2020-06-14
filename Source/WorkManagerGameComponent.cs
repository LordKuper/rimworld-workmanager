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
        public List<Pawn> DisabledPawns = new List<Pawn>();
        public List<PawnWorkType> DisabledPawnWorkTypes = new List<PawnWorkType>();
        public List<WorkTypeDef> DisabledWorkTypes = new List<WorkTypeDef>();
        public bool Enabled = true;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Game API")]
        public WorkManagerGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            DisabledPawns?.RemoveAll(pawn => pawn?.Destroyed ?? true);
            DisabledWorkTypes?.RemoveAll(workType =>
                !DefDatabase<WorkTypeDef>.AllDefsListForReading.Contains(workType));
            Scribe_Values.Look(ref Enabled, nameof(Enabled), true);
            Scribe_Collections.Look(ref DisabledWorkTypes, nameof(DisabledWorkTypes), LookMode.Def);
            Scribe_Collections.Look(ref DisabledPawns, nameof(DisabledPawns), LookMode.Reference);
            Scribe_Collections.Look(ref DisabledPawnWorkTypes, nameof(DisabledPawnWorkTypes), LookMode.Deep);
        }

        public bool GetPawnEnabled(Pawn pawn)
        {
            if (DisabledPawns == null) { DisabledPawns = new List<Pawn>(); }
            return !DisabledPawns.Contains(pawn);
        }

        public bool GetPawnWorkTypeEnabled(Pawn pawn, WorkTypeDef workType)
        {
            if (DisabledPawnWorkTypes == null) { DisabledPawnWorkTypes = new List<PawnWorkType>(); }
            return !DisabledPawnWorkTypes.Any(pwt => pwt.Pawn == pawn && pwt.WorkType == workType);
        }

        public bool GetWorkTypeEnabled(WorkTypeDef workType)
        {
            if (DisabledWorkTypes == null) { DisabledWorkTypes = new List<WorkTypeDef>(); }
            return !DisabledWorkTypes.Contains(workType);
        }

        public void SetPawnEnabled(Pawn pawn, bool enabled)
        {
            if (DisabledPawns == null) { DisabledPawns = new List<Pawn>(); }
            if (enabled) { DisabledPawns.RemoveAll(p => p == pawn); }
            else
            {
                if (!DisabledPawns.Contains(pawn)) { DisabledPawns.Add(pawn); }
            }
        }

        public void SetPawnWorkTypeEnabled(Pawn pawn, WorkTypeDef workType, bool enabled)
        {
            if (DisabledPawnWorkTypes == null) { DisabledPawnWorkTypes = new List<PawnWorkType>(); }
            if (enabled) { DisabledPawnWorkTypes.RemoveAll(pwt => pwt.Pawn == pawn && pwt.WorkType == workType); }
            else
            {
                if (!DisabledPawnWorkTypes.Any(pwt => pwt.Pawn == pawn && pwt.WorkType == workType))
                {
                    DisabledPawnWorkTypes.Add(new PawnWorkType {Pawn = pawn, WorkType = workType});
                }
            }
        }

        public void SetWorkTypeEnabled(WorkTypeDef workType, bool enabled)
        {
            if (DisabledWorkTypes == null) { DisabledWorkTypes = new List<WorkTypeDef>(); }
            if (enabled) { DisabledWorkTypes.RemoveAll(wt => wt == workType); }
            else
            {
                if (!DisabledWorkTypes.Contains(workType)) { DisabledWorkTypes.Add(workType); }
            }
        }
    }
}