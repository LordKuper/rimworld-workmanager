using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace WorkManager.Patches
{
    [HarmonyPatch(typeof(PawnColumnWorker_WorkPriority), nameof(PawnColumnWorker_WorkPriority.GetMinHeaderHeight))]
    [UsedImplicitly]
    public static class PawnColumnWorkerWorkPriorityHeaderHeightPatch
    {
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void Postfix(ref int __result)
        {
            __result += 30;
        }
    }
}