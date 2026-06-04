using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.PawnColumnWorkers;

/// <summary>
///     Pawn table column worker that toggles per-pawn work schedule management.
/// </summary>
[UsedImplicitly]
public class AutoWorkSchedule : PawnColumnWorker
{
    /// <summary>
    ///     Draws the toggle button cell for the given pawn.
    /// </summary>
    /// <param name="rect">The cell rectangle.</param>
    /// <param name="pawn">The pawn for the row.</param>
    /// <param name="table">The owning pawn table.</param>
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (pawn.Dead) return;
        if (!WorkManagerGameComponent.IsInitialized) return;
        var component = WorkManagerGameComponent.Instance;
        var buttonRect = new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16);
        if (component.ScheduleManagementEnabled)
        {
            Buttons.DoIconButtonToggle(buttonRect, () => component.GetPawnScheduleEnabled(pawn),
                newValue => component.SetPawnScheduleEnabled(pawn, newValue),
                Resources.Strings.PawnScheduleDisableTooltip, Resources.Textures.ScheduleToggleButtonEnabled,
                Resources.Strings.PawnScheduleEnableTooltip, Resources.Textures.ScheduleToggleButtonDisabled);
        }
        else
        {
            var color = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(buttonRect, Resources.Textures.PawnToggleButtonInactive);
            GUI.color = color;
        }
    }

    /// <summary>
    ///     Gets the minimum width of the column.
    /// </summary>
    /// <param name="table">The owning pawn table.</param>
    /// <returns>The minimum column width.</returns>
    public override int GetMinWidth(PawnTable table)
    {
        return 20;
    }
}