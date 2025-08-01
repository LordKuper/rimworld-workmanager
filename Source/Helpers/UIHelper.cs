using System;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager.Helpers
{
    internal static class UIHelper
    {
        public const float ActionButtonWidth = 200f;
        public const int BoolSettingsColumnCount = 2;
        public const float ButtonGap = 8f;
        public const float ButtonHeight = 32f;
        public const float ElementGap = 12f;
        public const float ListRowHeight = 32f;
        public static float LabelHeight => Text.LineHeightOf(GameFont.Medium) + 4f;

        public static bool? CycleSettingValue(MultiCheckboxState state)
        {
            switch (state)
            {
                case MultiCheckboxState.On:
                    return false;
                case MultiCheckboxState.Off:
                    return null;
                case MultiCheckboxState.Partial:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public static Rect GetBoolSettingRect(Rect rect, int index, float columnWidth)
        {
            var rowIndex = Math.DivRem(index, BoolSettingsColumnCount, out var columnIndex);
            return new Rect(rect.x + (columnWidth + ElementGap) * columnIndex, rect.y + ListRowHeight * rowIndex,
                columnWidth, ListRowHeight).ContractedBy(4f);
        }

        public static MultiCheckboxState GetSettingCheckboxState(bool? value)
        {
            return value == null ? MultiCheckboxState.Partial :
                value == false ? MultiCheckboxState.Off : MultiCheckboxState.On;
        }
    }
}