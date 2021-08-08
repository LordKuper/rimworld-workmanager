using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkManager.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
    [UsedImplicitly]
    public static class MainTabWindowWorkPatch
    {
        [UsedImplicitly]
        private static void Postfix(Rect rect)
        {
            var component = Current.Game.GetComponent<WorkManagerGameComponent>();
            CustomWidgets.ButtonImageToggle(ref component.PriorityManagementEnabled,
                new Rect(rect.x + 175, rect.y + 88, 24, 24), Resources.Strings.GlobalDisableTooltip,
                Resources.Textures.PrioritiesToggleButtonEnabled, Resources.Strings.GlobalEnableTooltip,
                Resources.Textures.PrioritiesToggleButtonDisabled);
        }
    }
}