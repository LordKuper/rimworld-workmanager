using HarmonyLib;
using Verse;

namespace LordKuper.WorkManager.Compatibility
{
    internal static class MoreThanCapable
    {
        internal static IsBadWorkDelegate IsBadWork;

        internal static void Initialize()
        {
#if DEBUG
            Logger.LogMessage("MoreThanCapable detected.");
#endif
            IsBadWork = AccessTools.MethodDelegate<IsBadWorkDelegate>(
                AccessTools.Method(AccessTools.TypeByName("MoreThanCapable.MoreThanCapableMod"), "IsBadWork"));
        }

        internal delegate bool IsBadWorkDelegate(Pawn pawn, WorkTypeDef workType);
    }
}