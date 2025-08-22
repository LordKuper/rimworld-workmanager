using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.PawnColumnWorkers;

[UsedImplicitly]
public class AutoWorkPriorities : PawnColumnWorker
{
    public override void DoCell(Rect rect, [NotNull] Pawn pawn, PawnTable table)
    {
        if (pawn.Dead || pawn.workSettings is not { EverWork: true }) return;
        var component = WorkManagerGameComponent.Instance;
        if (component.PriorityManagementEnabled)
        {
            Buttons.DoIconButtonToggle(new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16),
                () => component.GetPawnEnabled(pawn), newValue => component.SetPawnEnabled(pawn, newValue),
                Resources.Strings.PawnDisableTooltip, Resources.Textures.PawnToggleButtonEnabled,
                Resources.Strings.PawnEnableTooltip, Resources.Textures.PawnToggleButtonDisabled);
        }
        else
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16),
                Resources.Textures.PawnToggleButtonInactive);
        }
    }

    public override int GetMinWidth(PawnTable table)
    {
        return 20;
    }
}