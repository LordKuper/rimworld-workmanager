using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.Patches
{
    public static class WorkTabPatch
    {
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.MainTabWindow_WorkTab"), "DoWindowContents"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(DoWindowContentsPostfix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "GetMinHeaderHeight"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(GetMinHeaderHeightPostfix)));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DoHeader"),
                new HarmonyMethod(typeof(WorkTabPatch), nameof(DoHeaderPrefix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"),
                    "HandleInteractionsDetailed"),
                new HarmonyMethod(typeof(WorkTabPatch), nameof(HandleInteractionsDetailedPrefix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DrawWorkTypeBoxFor"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(DrawWorkTypeBoxForPostfix)));
        }

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        public static void DoHeaderPrefix(PawnColumnWorker __instance, ref Rect rect)
        {
            const int iconSize = 16;
            var buttonRect = new Rect(rect.center.x - iconSize / 2, rect.yMax - iconSize - 4, iconSize, iconSize);
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
            rect = new Rect(rect.x, rect.y, rect.width, rect.height - 30);
        }

        [UsedImplicitly]
        public static void DoWindowContentsPostfix(Rect rect)
        {
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            CustomWidgets.ButtonImageToggle(ref component.PriorityManagementEnabled,
                new Rect(rect.xMin, rect.yMin, 24, 24), Resources.Strings.GlobalDisableTooltip,
                Resources.Textures.PrioritiesToggleButtonEnabled, Resources.Strings.GlobalEnableTooltip,
                Resources.Textures.PrioritiesToggleButtonDisabled);
        }

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        public static void DrawWorkTypeBoxForPostfix(Rect box, Pawn pawn, WorkTypeDef worktype)
        {
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (!component.PriorityManagementEnabled || !Find.PlaySettings.useWorkPriorities)
            {
                return;
            }
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

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void GetMinHeaderHeightPostfix(ref int __result)
        {
            __result += 30;
        }

        [UsedImplicitly]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void HandleInteractionsDetailedPrefix(PawnColumnWorker __instance, Rect rect, Pawn pawn)
        {
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            if (!component.PriorityManagementEnabled || !Find.PlaySettings.useWorkPriorities)
            {
                return;
            }
            var workType = __instance.def.workType;
            var enabled = component.GetPawnWorkTypeEnabled(pawn, workType);
            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && Event.current.button == 2)
            {
                component.SetPawnWorkTypeEnabled(pawn, workType, !enabled);
            }
        }
    }
}