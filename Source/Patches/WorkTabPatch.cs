using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using LordKuper.Common.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.Patches;

/// <summary>
///     Contains Harmony patches for the WorkTab mod, enabling enhanced work type management features.
/// </summary>
public static class WorkTabPatch
{
    /// <summary>
    ///     Applies all Harmony patches related to the WorkTab mod.
    /// </summary>
    /// <param name="harmony">The Harmony instance to use for patching.</param>
    internal static void Apply([NotNull] Harmony harmony)
    {
        _ = harmony.Patch(
            AccessTools.Method(AccessTools.TypeByName("WorkTab.MainTabWindow_WorkTab"), "DoWindowContents"),
            postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(DoWindowContentsPostfix)));
        _ = harmony.Patch(
            AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "GetMinHeaderHeight"),
            postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(GetMinHeaderHeightPostfix)));
        _ = harmony.Patch(AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DoHeader"),
            new HarmonyMethod(typeof(WorkTabPatch), nameof(DoHeaderPrefix)));
        _ = harmony.Patch(
            AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"),
                "HandleInteractionsDetailed"),
            new HarmonyMethod(typeof(WorkTabPatch), nameof(HandleInteractionsDetailedPrefix)));
        _ = harmony.Patch(
            AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DrawWorkTypeBoxFor"),
            postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(DrawWorkTypeBoxForPostfix)));
    }

    /// <summary>
    ///     Prefix patch for the DoHeader method. Draws a toggle button for enabling/disabling work types in the header.
    /// </summary>
    /// <param name="__instance">The PawnColumnWorker instance.</param>
    /// <param name="rect">The header rectangle, modified to accommodate the button.</param>
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public static void DoHeaderPrefix(PawnColumnWorker __instance, ref Rect rect)
    {
        const int iconSize = 16;
        Rect buttonRect = new(rect.center.x - iconSize / 2, rect.yMax - iconSize - 4, iconSize, iconSize);
        var component = WorkManagerGameComponent.Instance;
        Buttons.DoIconButtonToggle(buttonRect, () => component.GetWorkTypeEnabled(__instance.def.workType),
            newValue => component.SetWorkTypeEnabled(__instance.def.workType, newValue),
            Resources.Strings.WorkTypeDisableTooltip, Resources.Textures.WorkTypeToggleButtonEnabled,
            Resources.Strings.WorkTypeEnableTooltip, Resources.Textures.WorkTypeToggleButtonDisabled);
        rect = new Rect(rect.x, rect.y, rect.width, rect.height - 30);
    }

    /// <summary>
    ///     Adds additional UI elements to the window, including toggle buttons and action buttons for managing priority
    ///     settings and accessing mod settings.
    /// </summary>
    /// <remarks>
    ///     This method dynamically adds buttons to the UI based on the current state of the priority
    ///     management system.  - If priority management is enabled, an additional "Update Now" button is displayed to allow
    ///     immediate updates. - A settings button is always displayed, allowing users to access the mod's configuration
    ///     window.
    /// </remarks>
    /// <param name="rect">
    ///     The rectangular area within which the UI elements are drawn. This defines the layout space available for the
    ///     buttons.
    /// </param>
    [UsedImplicitly]
    public static void DoWindowContentsPostfix(Rect rect)
    {
        var component = WorkManagerGameComponent.Instance;
        var buttonRow = new Rect(rect.xMin + Layout.ElementGapTiny, rect.yMin + Layout.ElementGapTiny,
            rect.width - Layout.ElementGapTiny * 2, Buttons.IconButtonSize);
        var buttonRect = Layout.GetLeftColumnRect(buttonRow, Buttons.IconButtonSize, out buttonRow);
        Buttons.DoIconButtonToggle(buttonRect, ref component.PriorityManagementEnabled,
            Resources.Strings.GlobalDisableTooltip, Resources.Textures.PrioritiesToggleButtonEnabled,
            Resources.Strings.GlobalEnableTooltip, Resources.Textures.PrioritiesToggleButtonDisabled);
        Layout.GetLeftColumnRect(buttonRow, Layout.ElementGapSmall, out buttonRow);
        buttonRect = Layout.GetLeftColumnRect(buttonRow, Buttons.IconButtonSize, out buttonRow);
        Buttons.DoIconButton(buttonRect,
            new IconButton(Resources.Textures.RefreshButton, WorkManagerGameComponent.ForceUpdateAssignments,
                Resources.Strings.UpdateNowTooltip));
        Layout.GetLeftColumnRect(buttonRow, Layout.ElementGapSmall, out buttonRow);
        buttonRect = Layout.GetLeftColumnRect(buttonRow, Buttons.IconButtonSize, out buttonRow);
        Buttons.DoIconButton(buttonRect,
            new IconButton(Resources.Textures.SettingsButton, WorkManagerMod.OpenModSettingsWindow,
                Resources.Strings.OpenSettingsTooltip));
    }

    /// <summary>
    ///     Postfix patch for the DrawWorkTypeBoxFor method. Draws a visual indicator if a pawn's work type is disabled.
    /// </summary>
    /// <param name="box">The rectangle representing the work type box.</param>
    /// <param name="pawn">The pawn for whom the box is drawn.</param>
    /// <param name="worktype">The work type definition.</param>
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public static void DrawWorkTypeBoxForPostfix(Rect box, Pawn pawn, WorkTypeDef worktype)
    {
        var component = WorkManagerGameComponent.Instance;
        if (!component.PriorityManagementEnabled || !Find.PlaySettings.useWorkPriorities) return;
        var enabled = component.GetPawnWorkTypeEnabled(pawn, worktype);
        if (!enabled)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            var position = box;
            position.xMax = box.center.x;
            position.yMax = box.center.y;
            GUI.DrawTexture(position.ContractedBy(2f), Resources.Textures.PawnWorkTypeDisabled);
        }
        GUI.color = Color.white;
    }

    /// <summary>
    ///     Postfix patch for the GetMinHeaderHeight method. Increases the minimum header height to accommodate custom UI
    ///     elements.
    /// </summary>
    /// <param name="__result">The minimum header height result to modify.</param>
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void GetMinHeaderHeightPostfix(ref int __result)
    {
        __result += 30;
    }

    /// <summary>
    ///     Prefix patch for the HandleInteractionsDetailed method. Handles middle-click interactions to toggle pawn work type
    ///     enablement.
    /// </summary>
    /// <param name="__instance">The PawnColumnWorker instance.</param>
    /// <param name="rect">The rectangle representing the UI element.</param>
    /// <param name="pawn">The pawn being interacted with.</param>
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void HandleInteractionsDetailedPrefix(PawnColumnWorker __instance, Rect rect, Pawn pawn)
    {
        var component = WorkManagerGameComponent.Instance;
        if (!component.PriorityManagementEnabled || !Find.PlaySettings.useWorkPriorities) return;
        var workType = __instance.def.workType;
        var enabled = component.GetPawnWorkTypeEnabled(pawn, workType);
        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && Event.current.button == 2)
            component.SetPawnWorkTypeEnabled(pawn, workType, !enabled);
    }
}