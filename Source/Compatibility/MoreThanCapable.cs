using System;
using HarmonyLib;
using Verse;

namespace LordKuper.WorkManager.Compatibility;

/// <summary>
///     Provides compatibility integration with the MoreThanCapable mod.
/// </summary>
internal static class MoreThanCapable
{
    /// <summary>
    ///     Indicates whether the compatibility layer has been initialized.
    /// </summary>
    private static bool _isInitialized;

    /// <summary>
    ///     Delegate to the MoreThanCapableMod.IsBadWork method.
    /// </summary>
    internal static IsBadWorkDelegate IsBadWork;

    /// <summary>
    ///     Indicates whether the More Than Capable mod is active.
    /// </summary>
    internal static bool MoreThanCapableActive;

    /// <summary>
    ///     Initializes the compatibility layer for the MoreThanCapable mod.
    ///     Checks if the mod is active and sets up the IsBadWork delegate.
    /// </summary>
    internal static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        MoreThanCapableActive =
            LoadedModManager.RunningModsListForReading.Any(m =>
                "notfood.MoreThanCapable".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase)) ||
            LoadedModManager.RunningModsListForReading.Any(m =>
                "void.MoreThanCapable".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase));
        if (!MoreThanCapableActive) return;
#if DEBUG
        Logger.LogMessage("MoreThanCapable detected.");
#endif
        try
        {
            IsBadWork = AccessTools.MethodDelegate<IsBadWorkDelegate>(
                AccessTools.Method(AccessTools.TypeByName("MoreThanCapable.MoreThanCapableMod"), "IsBadWork"));
            if (IsBadWork == null) throw new InvalidOperationException("Could not create 'IsBadWork' method delegate.");
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to initialize MoreThanCapable compatibility.", e);
        }
    }

    /// <summary>
    ///     Represents a delegate for determining if a work type is considered "bad" for a pawn.
    /// </summary>
    /// <param name="pawn">The pawn to check.</param>
    /// <param name="workType">The work type definition.</param>
    /// <returns>True if the work type is considered bad for the pawn; otherwise, false.</returns>
    internal delegate bool IsBadWorkDelegate(Pawn pawn, WorkTypeDef workType);
}