using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld;

namespace LordKuper.WorkManager.DefOfs;

/// <summary>
///     Contains references to custom <see cref="PawnColumnDef" /> definitions used by the Work Manager mod.
/// </summary>
[DefOf]
[UsedImplicitly]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public static class PawnColumnDefOf
{
    /// <summary>
    ///     The pawn column definition for displaying and editing automatic work priorities.
    /// </summary>
    public static PawnColumnDef AutoWorkPriorities;

    /// <summary>
    ///     The pawn column definition for displaying and editing automatic work schedules.
    /// </summary>
    public static PawnColumnDef AutoWorkSchedule;
}