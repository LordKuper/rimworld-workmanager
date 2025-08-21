using LordKuper.WorkManager.Compatibility;
using Verse;

namespace LordKuper.WorkManager.Helpers;

/// <summary>
///     Provides helper methods for getting and setting work type priorities for pawns,
///     supporting compatibility with WorkTab if active.
/// </summary>
internal static class WorkTypePriorityHelper
{
    /// <summary>
    ///     Gets the priority of the specified work type for the given pawn.
    ///     Uses WorkTab if it is active; otherwise, uses the pawn's work settings.
    /// </summary>
    /// <param name="pawn">The pawn whose work priority is being queried.</param>
    /// <param name="workType">The work type definition.</param>
    /// <returns>The priority value for the specified work type.</returns>
    internal static int GetPriority(Pawn pawn, WorkTypeDef workType)
    {
        return WorkTab.WorkTabActive
            ? WorkTab.GetPriority(pawn, workType, -1)
            : pawn.workSettings.GetPriority(workType);
    }

    /// <summary>
    ///     Sets the priority of the specified work type for the given pawn.
    ///     Uses WorkTab if it is active; otherwise, sets the priority in the pawn's work settings.
    /// </summary>
    /// <param name="pawn">The pawn whose work priority is being set.</param>
    /// <param name="workType">The work type definition.</param>
    /// <param name="priority">The priority value to set.</param>
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