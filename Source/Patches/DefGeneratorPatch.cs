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
    ///     Postfix method that inserts custom pawn columns into the Work and Restrict tables after the "Label" column.
    /// </summary>
    [UsedImplicitly]
    public static void Postfix()
    {
        PawnTableDefOf.Work.columns.Insert(
            PawnTableDefOf.Work.columns.FindIndex(x => x.defName.Equals("Label", StringComparison.Ordinal)) + 1,
            PawnColumnDefOf.AutoWorkPriorities);
        PawnTableDefOf.Restrict.columns.Insert(
            PawnTableDefOf.Restrict.columns.FindIndex(x => x.defName.Equals("Label", StringComparison.Ordinal)) + 1,
            PawnColumnDefOf.AutoWorkSchedule);
    }
}