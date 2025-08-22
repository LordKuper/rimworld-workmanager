using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.Patches;

/// <summary>
///     Harmony patch class for customizing the behavior of work box drawing in the work tab.
/// </summary>
[HarmonyPatch(typeof(WidgetsWork))]
[UsedImplicitly]
public static class WidgetsWorkPatch
{
    /// <summary>
    ///     Postfix patch for <see cref="WidgetsWork.DrawWorkBoxFor" />.
    ///     Draws a semi-transparent overlay if the pawn's work type is disabled.
    /// </summary>
    /// <param name="x">The x position of the work box.</param>
    /// <param name="y">The y position of the work box.</param>
    /// <param name="p">The pawn for which the work box is drawn.</param>
    /// <param name="wType">The work type definition.</param>
    [UsedImplicitly]
    [HarmonyPatch(nameof(WidgetsWork.DrawWorkBoxFor))]
    [HarmonyPostfix]
    private static void DrawWorkBoxForPostfix(float x, float y, Pawn p, WorkTypeDef wType)
    {
        var component = Current.Game.GetComponent<WorkManagerGameComponent>();
        if (!component.PriorityManagementEnabled || !Find.PlaySettings.useWorkPriorities) return;
        Rect rect = new(x, y, 25f, 25f);
        var enabled = component.GetPawnWorkTypeEnabled(p, wType);
        if (!enabled)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            var position = rect;
            position.xMax = rect.center.x;
            position.yMax = rect.center.y;
            GUI.DrawTexture(position.ContractedBy(2f), Resources.Textures.PawnWorkTypeDisabled);
        }
        GUI.color = Color.white;
    }

    /// <summary>
    ///     Prefix patch for <see cref="WidgetsWork.DrawWorkBoxFor" />.
    ///     Handles middle mouse button clicks to toggle the enabled state of a pawn's work type.
    /// </summary>
    /// <param name="x">The x position of the work box.</param>
    /// <param name="y">The y position of the work box.</param>
    /// <param name="p">The pawn for which the work box is drawn.</param>
    /// <param name="wType">The work type definition.</param>
    [UsedImplicitly]
    [HarmonyPatch(nameof(WidgetsWork.DrawWorkBoxFor))]
    [HarmonyPrefix]
    private static void DrawWorkBoxForPrefix(float x, float y, [CanBeNull] Pawn p, [CanBeNull] WorkTypeDef wType)
    {
        if (p == null || wType == null) return;
        var component = Current.Game.GetComponent<WorkManagerGameComponent>();
        if (!component.PriorityManagementEnabled || !Find.PlaySettings.useWorkPriorities) return;
        Rect rect = new(x, y, 25f, 25f);
        var enabled = component.GetPawnWorkTypeEnabled(p, wType);
        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && Event.current.button == 2)
            component.SetPawnWorkTypeEnabled(p, wType, !enabled);
    }
}