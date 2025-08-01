using System;
using HarmonyLib;
using JetBrains.Annotations;
using LordKuper.WorkManager.DefOfs;
using RimWorld;

namespace LordKuper.WorkManager.Patches
{
    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    [HarmonyBefore("fluffy.worktab")]
    [UsedImplicitly]
    public static class DefGeneratorPatch
    {
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
}