using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkManager.Patches
{
    [HarmonyPatch(typeof(PawnColumnWorker_WorkPriority), nameof(PawnColumnWorker_WorkPriority.DoHeader))]
    [UsedImplicitly]
    public static class PawnColumnWorkerWorkPriorityHeaderContentPatch
    {
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void Postfix(PawnColumnWorker_WorkPriority __instance, Rect rect)
        {
            var buttonRect = new Rect(rect.center.x - 7, rect.y + 55, 16, 16);
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (component.Enabled)
            {
                CustomWidgets.ButtonImageToggle(() => !component.DisabledWorkTypes.Contains(__instance.def.workType),
                    newValue => component.SetWorkTypeEnabled(__instance.def.workType, newValue), buttonRect,
                    Resources.Strings.WorkTypeDisableTooltip, Resources.Textures.WorkTypeToggleButtonEnabled,
                    Resources.Strings.WorkTypeEnableTooltip, Resources.Textures.WorkTypeToggleButtonDisabled);
            }
            else
            {
                GUI.color = Color.white;
                GUI.DrawTexture(buttonRect, Resources.Textures.WorkTypeToggleButtonInactive);
            }
        }
    }
}