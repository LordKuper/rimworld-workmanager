using System.Collections.Generic;
using HarmonyLib;
using LordKuper.WorkManager.Patches;
using Verse;

namespace LordKuper.WorkManager.Compatibility
{
    internal static class WorkTab
    {
        internal static GetPriorityDelegate GetPriority;
        internal static SetPriorityDelegate SetPriority;

        internal static void Initialize(Harmony harmony)
        {
#if DEBUG
            Logger.LogMessage("WorkTab detected.");
#endif
            WorkTabPatch.Apply(harmony);
            GetPriority = AccessTools.MethodDelegate<GetPriorityDelegate>(AccessTools.Method(
                AccessTools.TypeByName("WorkTab.Pawn_Extensions"), "GetPriority",
                new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(int) }));
            SetPriority = AccessTools.MethodDelegate<SetPriorityDelegate>(AccessTools.Method(
                AccessTools.TypeByName("WorkTab.Pawn_Extensions"), "SetPriority",
                new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(int), typeof(List<int>) }));
            Settings.Settings.MaxPriority = Traverse.Create(AccessTools.TypeByName("WorkTab.Settings"))
                .Field<int>("maxPriority").Value;
#if DEBUG
            Logger.LogMessage($"Max priority is {Settings.Settings.MaxPriority}.");
#endif
        }

        internal delegate int GetPriorityDelegate(Pawn pawn, WorkTypeDef workType, int hour);

        internal delegate void SetPriorityDelegate(Pawn pawn, WorkTypeDef workType, int priority, List<int> hours);
    }
}