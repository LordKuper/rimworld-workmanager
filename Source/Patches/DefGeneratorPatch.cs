using System;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using WorkManager.DefOfs;

namespace WorkManager.Patches
{
    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    [UsedImplicitly]
    public static class DefGeneratorPatch
    {
        [UsedImplicitly]
        public static void Postfix()
        {
            var workTable = PawnTableDefOf.Work;
            var labelIndex = workTable.columns.FindIndex(x => x.defName.Equals("Label", StringComparison.Ordinal));
            workTable.columns.Insert(labelIndex + 1, PawnColumnDefOf.AutoWorkPriorities);
        }
    }
}