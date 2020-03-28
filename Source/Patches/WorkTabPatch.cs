using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkManager.Patches
{
    public static class WorkTabPatch
    {
        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        public static void DoHeaderPrefix(PawnColumnWorker __instance, ref Rect rect)
        {
            const int iconSize = 16;
            var buttonRect = new Rect(rect.center.x - iconSize / 2, rect.yMax - iconSize - 4, iconSize, iconSize);
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
            rect = new Rect(rect.x, rect.y, rect.width, rect.height - 30);
        }

        [UsedImplicitly]
        public static void DoWindowContentsPostfix(Rect rect)
        {
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            CustomWidgets.ButtonImageToggle(ref component.Enabled, new Rect(rect.xMin, rect.yMin, 24, 24),
                Resources.Strings.GlobalDisableTooltip, Resources.Textures.GlobalToggleButtonEnabled,
                Resources.Strings.GlobalEnableTooltip, Resources.Textures.GlobalToggleButtonDisabled);
        }

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void GetMinHeaderHeightPostfix(ref int __result)
        {
            __result += 30;
        }
    }
}