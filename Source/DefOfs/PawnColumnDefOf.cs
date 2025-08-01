using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld;

namespace LordKuper.WorkManager.DefOfs
{
    [DefOf]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "UnassignedField.Global")]
    public static class PawnColumnDefOf
    {
        public static PawnColumnDef AutoWorkPriorities;
        public static PawnColumnDef AutoWorkSchedule;
    }
}