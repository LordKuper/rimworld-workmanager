using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace LordKuper.WorkManager.Compatibility
{
    internal static class Vse
    {
        internal static GetPassionsDelegate GetPassions;

        public static void Initialize()
        {
#if DEBUG
            Logger.LogMessage("Vanilla Skills Expanded detected.");
#endif
            GetPassions = AccessTools.MethodDelegate<GetPassionsDelegate>(
                AccessTools.PropertyGetter(AccessTools.TypeByName("VSE.Passions.PassionManager"), "AllPassions"));
        }

        internal delegate IEnumerable<Passion> GetPassionsDelegate();
    }
}