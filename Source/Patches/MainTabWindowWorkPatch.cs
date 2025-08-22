using HarmonyLib;
using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;

namespace LordKuper.WorkManager.Patches;

/// <summary>
///     Harmony patch for <see cref="MainTabWindow_Work.DoWindowContents" /> to inject custom UI buttons for work
///     management.
/// </summary>
[HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
[UsedImplicitly]
public static class MainTabWindowWorkPatch
{
    /// <summary>
    ///     Postfix method that adds custom buttons to the work tab window.
    /// </summary>
    /// <param name="rect">The window rectangle to draw contents in.</param>
    [UsedImplicitly]
    private static void Postfix(Rect rect)
    {
        var component = WorkManagerGameComponent.Instance;
        var buttonRow = new Rect(rect.xMin + Layout.ElementGapTiny, rect.yMin + Layout.ElementGapTiny,
            rect.width - Layout.ElementGap - Layout.ElementGapTiny * 2, Buttons.IconButtonSize);
        var buttonRect = Layout.GetRightColumnRect(buttonRow, Buttons.IconButtonSize, out buttonRow);
        Buttons.DoIconButtonToggle(buttonRect, ref component.PriorityManagementEnabled,
            Resources.Strings.GlobalDisableTooltip, Resources.Textures.PrioritiesToggleButtonEnabled,
            Resources.Strings.GlobalEnableTooltip, Resources.Textures.PrioritiesToggleButtonDisabled);
        if (component.PriorityManagementEnabled)
        {
            Layout.GetRightColumnRect(buttonRow, Layout.ElementGapSmall, out buttonRow);
            buttonRect = Layout.GetRightColumnRect(buttonRow, Buttons.IconButtonSize, out buttonRow);
            Buttons.DoIconButton(buttonRect,
                new IconButton(Resources.Textures.RefreshButton, WorkManagerGameComponent.ForceUpdateAssignments,
                    Resources.Strings.UpdateNowTooltip));
        }
        Layout.GetRightColumnRect(buttonRow, Layout.ElementGapSmall, out buttonRow);
        buttonRect = Layout.GetRightColumnRect(buttonRow, Buttons.IconButtonSize, out buttonRow);
        Buttons.DoIconButton(buttonRect,
            new IconButton(Resources.Textures.SettingsButton, WorkManagerMod.OpenModSettingsWindow,
                Resources.Strings.OpenSettingsTooltip));
    }
}