using System;
using HarmonyLib;
using JetBrains.Annotations;
using LordKuper.WorkManager.DefOfs;
using RimWorld;

namespace LordKuper.WorkManager.Patches;

/// <summary>
///     Harmony patch for <see cref="DefGenerator.GenerateImpliedDefs_PreResolve" /> to inject custom pawn columns.
/// </summary>
[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
[HarmonyBefore("fluffy.worktab")]
[UsedImplicitly]
public static class DefGeneratorPatch
{
    /// <summary>
    ///     Inserts <paramref name="column" /> immediately after the "Label" column of <paramref name="table" />.
    ///     If no "Label" column exists, the column is appended to the end instead of the start.
    /// </summary>
    /// <param name="table">The pawn table to modify.</param>
    /// <param name="column">The column to insert.</param>
    private static void InsertAfterLabel([NotNull] PawnTableDef table, PawnColumnDef column)
    {
        var labelIndex = table.columns.FindIndex(x =>
            x.defName.Equals("Label", StringComparison.Ordinal));
        if (labelIndex < 0)
        {
            Logger.LogWarning(
                $"'Label' column not found in '{table.defName}' table; appending '{column.defName}' to the end.");
            table.columns.Add(column);
            return;
        }
        table.columns.Insert(labelIndex + 1, column);
    }

    /// <summary>
    ///     Postfix method that inserts custom pawn columns into the Work and Restrict tables after the "Label" column.
    /// </summary>
    [UsedImplicitly]
    public static void Postfix()
    {
        InsertAfterLabel(PawnTableDefOf.Work, PawnColumnDefOf.AutoWorkPriorities);
        InsertAfterLabel(PawnTableDefOf.Restrict, PawnColumnDefOf.AutoWorkSchedule);
    }
}