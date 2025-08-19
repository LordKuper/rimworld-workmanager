using JetBrains.Annotations;
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
        var component = Current.Game.GetComponent<WorkManagerGameComponent>();
        if (component.PriorityManagementEnabled)
        {
            CustomWidgets.ButtonImageToggle(() => component.GetPawnEnabled(pawn),
                newValue => component.SetPawnEnabled(pawn, newValue),
                new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16), Resources.Strings.PawnDisableTooltip,
                Resources.Textures.PawnToggleButtonEnabled, Resources.Strings.PawnEnableTooltip,
                Resources.Textures.PawnToggleButtonDisabled);
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