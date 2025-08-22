using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.Patches;

/// <summary>
///     Harmony patch for <see cref="PawnColumnWorker_WorkPriority" /> to add custom UI elements and adjust header height.
/// </summary>
[HarmonyPatch(typeof(PawnColumnWorker_WorkPriority))]
[UsedImplicitly]
public static class PawnColumnWorkerWorkPriorityPatch
{
    /// <summary>
    ///     Postfix for <see cref="PawnColumnWorker_WorkPriority.DoHeader" />.
    ///     Draws a toggle button for enabling/disabling work type management in the work priority column header.
    /// </summary>
    /// <param name="__instance">The instance of <see cref="PawnColumnWorker_WorkPriority" /> being patched.</param>
    /// <param name="rect">The rectangle area for the header UI.</param>
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(nameof(PawnColumnWorker_WorkPriority.DoHeader))]
    [HarmonyPostfix]
    private static void DoHeaderPostfix(PawnColumnWorker_WorkPriority __instance, Rect rect)
    {
        const int iconSize = 16;
        Rect buttonRect = new(rect.center.x - iconSize / 2f + 1, rect.yMax - iconSize - 4, iconSize, iconSize);
        var component = Current.Game.GetComponent<WorkManagerGameComponent>();
        if (component.PriorityManagementEnabled)
        {
            Buttons.DoIconButtonToggle(buttonRect, () => component.GetWorkTypeEnabled(__instance.def.workType),
                newValue => component.SetWorkTypeEnabled(__instance.def.workType, newValue),
                Resources.Strings.WorkTypeDisableTooltip, Resources.Textures.WorkTypeToggleButtonEnabled,
                Resources.Strings.WorkTypeEnableTooltip, Resources.Textures.WorkTypeToggleButtonDisabled);
        }
        else
        {
            GUI.color = Color.white;
            GUI.DrawTexture(buttonRect, Resources.Textures.WorkTypeToggleButtonInactive);
        }
    }

    /// <summary>
    ///     Postfix for <see cref="PawnColumnWorker_WorkPriority.GetMinHeaderHeight" />.
    ///     Increases the minimum header height to accommodate custom UI elements.
    /// </summary>
    /// <param name="__result">The result value to be modified.</param>
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(nameof(PawnColumnWorker_WorkPriority.GetMinHeaderHeight))]
    [HarmonyPostfix]
    private static void GetMinHeaderHeightPostfix(ref int __result)
    {
        __result += 30;
    }
}