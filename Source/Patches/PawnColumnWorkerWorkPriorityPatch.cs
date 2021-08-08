using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkManager.Patches
{
    [HarmonyPatch(typeof(PawnColumnWorker_WorkPriority))]
    [UsedImplicitly]
    public static class PawnColumnWorkerWorkPriorityPatch
    {
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [HarmonyPatch(nameof(PawnColumnWorker_WorkPriority.DoHeader))]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        private static void DoHeaderPostfix(PawnColumnWorker_WorkPriority __instance, Rect rect)
        {
            const int iconSize = 16;
            var buttonRect = new Rect(rect.center.x - iconSize / 2 + 1, rect.yMax - iconSize - 4, iconSize, iconSize);
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (component.PriorityManagementEnabled)
            {
                CustomWidgets.ButtonImageToggle(() => component.GetWorkTypeEnabled(__instance.def.workType),
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

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [HarmonyPatch(nameof(PawnColumnWorker_WorkPriority.GetMinHeaderHeight))]
        [HarmonyPostfix]
        private static void GetMinHeaderHeightPostfix(ref int __result)
        {
            __result += 30;
        }
    }
}