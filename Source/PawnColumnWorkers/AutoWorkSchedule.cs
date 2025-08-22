using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.PawnColumnWorkers;

[UsedImplicitly]
public class AutoWorkSchedule : PawnColumnWorker
{
    public override void DoCell(Rect rect, [NotNull] Pawn pawn, PawnTable table)
    {
        if (pawn.Dead) return;
        var component = WorkManagerGameComponent.Instance;
        var buttonRect = new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16);
        if (component.PriorityManagementEnabled)
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

    public override int GetMinWidth(PawnTable table)
    {
        return 20;
    }
}