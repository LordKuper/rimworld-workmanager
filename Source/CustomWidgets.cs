using System;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager;

internal static class CustomWidgets
{
    internal static void ButtonImageToggle(ref bool property, Rect rect, string enabledTooltip,
        Texture2D enabledTexture, string disabledTooltip, Texture2D disabledTexture)
    {
        TooltipHandler.TipRegion(rect, property ? enabledTooltip : disabledTooltip);
        if (Widgets.ButtonImage(rect, property ? enabledTexture : disabledTexture, Color.white, GenUI.MouseoverColor))
            property = !property;
    }

    internal static void ButtonImageToggle([NotNull] Func<bool> getter, Action<bool> setter, Rect rect,
        string enabledTooltip, Texture2D enabledTexture, string disabledTooltip, Texture2D disabledTexture)
    {
        TooltipHandler.TipRegion(rect, getter() ? enabledTooltip : disabledTooltip);
        if (Widgets.ButtonImage(rect, getter() ? enabledTexture : disabledTexture, Color.white, GenUI.MouseoverColor))
            setter(!getter());
    }
}