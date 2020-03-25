using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld;

namespace WorkManager.DefOfs
{
    [DefOf]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "UnassignedField.Global")]
    public static class PawnColumnDefOf
    {
        public static PawnColumnDef AutoWorkPriorities;
    }
}