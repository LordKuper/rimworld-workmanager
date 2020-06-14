using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkManager.Patches
{
    [HarmonyPatch(typeof(WidgetsWork))]
    [UsedImplicitly]
    public static class WidgetsWorkPatch
    {
        [UsedImplicitly]
        [HarmonyPatch(nameof(WidgetsWork.DrawWorkBoxFor))]
        [HarmonyPostfix]
        private static void DrawWorkBoxForPostfix(float x, float y, Pawn p, WorkTypeDef wType)
        {
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (!component.Enabled || !Find.PlaySettings.useWorkPriorities) { return; }
            var rect = new Rect(x, y, 25f, 25f);
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

        [UsedImplicitly]
        [HarmonyPatch(nameof(WidgetsWork.DrawWorkBoxFor))]
        [HarmonyPrefix]
        private static void DrawWorkBoxForPrefix(float x, float y, Pawn p, WorkTypeDef wType)
        {
            if (p == null || wType == null) { return; }
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (!component.Enabled || !Find.PlaySettings.useWorkPriorities) { return; }
            var rect = new Rect(x, y, 25f, 25f);
            var enabled = component.GetPawnWorkTypeEnabled(p, wType);
            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && Event.current.button == 2)
            {
                component.SetPawnWorkTypeEnabled(p, wType, !enabled);
            }
        }
    }
}