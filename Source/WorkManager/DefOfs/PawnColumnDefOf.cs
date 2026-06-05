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
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global",
        Justification = "Populated by RimWorld [DefOf] reflection after construction; must remain writable.")]
    [SuppressMessage("ReSharper", "MemberCanBeInternal",
        Justification = "Public surface required by RimWorld [DefOf] reflection; changing visibility breaks def injection.")]
    public static PawnColumnDef AutoWorkPriorities = null!;

    /// <summary>
    ///     The pawn column definition for displaying and editing automatic work schedules.
    /// </summary>
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global",
        Justification = "Populated by RimWorld [DefOf] reflection after construction; must remain writable.")]
    [SuppressMessage("ReSharper", "MemberCanBeInternal",
        Justification = "Public surface required by RimWorld [DefOf] reflection; changing visibility breaks def injection.")]
    public static PawnColumnDef AutoWorkSchedule = null!;
}