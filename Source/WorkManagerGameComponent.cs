using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace WorkManager
{
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class WorkManagerGameComponent : GameComponent
    {
        public List<Pawn> DisabledPawns = new List<Pawn>();
        public List<WorkTypeDef> DisabledWorkTypes = new List<WorkTypeDef>();
        public bool Enabled = true;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Game API")]
        public WorkManagerGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            if (DisabledPawns != null)
            {
                foreach (var pawn in DisabledPawns.Where(pawn => pawn?.Destroyed ?? true))
                {
                    DisabledPawns.Remove(pawn);
                }
            }
            if (DisabledWorkTypes != null)
            {
                foreach (var workType in DisabledWorkTypes.Where(w =>
                    !DefDatabase<WorkTypeDef>.AllDefsListForReading.Contains(w)))
                {
                    DisabledWorkTypes.Remove(workType);
                }
            }
            Scribe_Values.Look(ref Enabled, nameof(Enabled), true);
            Scribe_Collections.Look(ref DisabledWorkTypes, nameof(DisabledWorkTypes), LookMode.Def);
            Scribe_Collections.Look(ref DisabledPawns, nameof(DisabledPawns), LookMode.Reference);
        }

        public void SetPawnEnabled(Pawn pawn, bool enabled)
        {
            if (enabled) { DisabledPawns.Remove(pawn); }
            else { DisabledPawns.Add(pawn); }
        }

        public void SetWorkTypeEnabled(WorkTypeDef workType, bool enabled)
        {
            if (enabled) { DisabledWorkTypes.Remove(workType); }
            else { DisabledWorkTypes.Add(workType); }
        }
    }
}