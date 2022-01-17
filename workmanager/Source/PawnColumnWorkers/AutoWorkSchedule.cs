using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkManager.PawnColumnWorkers
{
    [UsedImplicitly]
    public class AutoWorkSchedule : PawnColumnWorker
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.Dead) { return; }
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (component.PriorityManagementEnabled)
            {
                CustomWidgets.ButtonImageToggle(() => component.GetPawnScheduleEnabled(pawn),
                    newValue => component.SetPawnScheduleEnabled(pawn, newValue),
                    new Rect(rect.center.x - 8, rect.center.y - 8, 16, 16),
                    Resources.Strings.PawnScheduleDisableTooltip, Resources.Textures.PawnToggleButtonEnabled,
                    Resources.Strings.PawnScheduleEnableTooltip, Resources.Textures.PawnToggleButtonDisabled);
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
}