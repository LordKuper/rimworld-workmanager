using System;
using System.Linq;
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
    ///     Inserts <paramref name="column" /> immediately before the first column of <paramref name="table" /> matched by
    ///     <paramref name="target" />. If no column matches, the column is appended to the end instead.
    /// </summary>
    /// <param name="table">The pawn table to modify.</param>
    /// <param name="column">The column to insert.</param>
    /// <param name="target">Predicate identifying the column to insert before.</param>
    private static void InsertBefore(PawnTableDef table, PawnColumnDef column, Func<PawnColumnDef, bool> target)
    {
        var targetIndex = table.columns.FindIndex(x => target(x));
        if (targetIndex < 0)
        {
            Logger.LogWarning(
                $"Target column not found in '{table.defName}' table; appending '{column.defName}' to the end." +
                Environment.NewLine +
                $"Registered columns: {string.Join(", ", table.columns.Select(c => c.defName))}");
            table.columns.Add(column);
            return;
        }
        table.columns.Insert(targetIndex, column);
    }

    /// <summary>
    ///     Postfix method that inserts custom pawn columns into the Work and Restrict tables.
    ///     <see cref="PawnColumnDefOf.AutoWorkPriorities" /> is inserted before the first column whose defName starts with
    ///     "WorkPriority"; <see cref="PawnColumnDefOf.AutoWorkSchedule" /> is inserted before the "Timetable" column.
    /// </summary>
    [UsedImplicitly]
    public static void Postfix()
    {
        InsertBefore(PawnTableDefOf.Work, PawnColumnDefOf.AutoWorkPriorities,
            x => x.defName.StartsWith("WorkPriority", StringComparison.Ordinal));
        InsertBefore(PawnTableDefOf.Restrict, PawnColumnDefOf.AutoWorkSchedule,
            x => x.defName.Equals("Timetable", StringComparison.Ordinal));
    }
}