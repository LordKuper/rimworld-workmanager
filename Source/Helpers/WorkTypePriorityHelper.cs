using LordKuper.WorkManager.Compatibility;
using Verse;

namespace LordKuper.WorkManager.Helpers;

internal static class WorkTypePriorityHelper
{
    internal static int GetPriority(Pawn pawn, WorkTypeDef workType)
    {
        return WorkTab.WorkTabActive
            ? WorkTab.GetPriority(pawn, workType, -1)
            : pawn.workSettings.GetPriority(workType);
    }

    internal static void SetPriority(Pawn pawn, WorkTypeDef workType, int priority)
    {
        if (WorkTab.WorkTabActive)
        {
            WorkTab.SetPriority(pawn, workType, priority, null);
            return;
        }
        pawn.workSettings.SetPriority(workType, priority);
    }
}