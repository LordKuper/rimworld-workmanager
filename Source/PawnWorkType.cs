using Verse;

namespace LordKuper.WorkManager
{
    public class PawnWorkType : IExposable
    {
        public Pawn Pawn;
        public WorkTypeDef WorkType;

        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, nameof(Pawn), true);
            Scribe_Defs.Look(ref WorkType, nameof(WorkType));
        }
    }
}