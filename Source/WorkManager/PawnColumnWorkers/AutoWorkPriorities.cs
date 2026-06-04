using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.PawnColumnWorkers;

/// <summary>
///     Pawn table column worker that toggles per-pawn work priority management.
/// </summary>
[UsedImplicitly]
public class AutoWorkPriorities : PawnColumnWorker
{
    /// <summary>
    ///     Draws the toggle button cell for the given pawn.
    /// </summary>
    /// <param name="rect">The cell rectangle.</param>
    /// <param name="pawn">The pawn for the row.</param>
    /// <param name="table">The owning pawn table.</param>
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (pawn.Dead || pawn.workSettings is not { EverWork: true }) return;
        if (!WorkManagerGameComponent.IsInitialized) return;
        var component = WorkManagerGameComponent.Instance;
        Buttons.DoIconButtonToggle(new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16),
            () => component.GetPawnEnabled(pawn),
            newValue => component.SetPawnEnabled(pawn, newValue),
            Resources.Strings.PawnDisableTooltip, Resources.Textures.PawnToggleButtonEnabled,
            Resources.Strings.PawnEnableTooltip, Resources.Textures.PawnToggleButtonDisabled);
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