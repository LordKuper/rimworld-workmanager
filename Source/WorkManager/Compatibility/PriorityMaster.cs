using System;
using HarmonyLib;
using Verse;

namespace LordKuper.WorkManager.Compatibility;

/// <summary>
///     Provides compatibility integration with the PriorityMaster mod, which expands the vanilla work priority range.
/// </summary>
internal static class PriorityMaster
{
    /// <summary>
    ///     Indicates whether the PriorityMaster compatibility has been initialized.
    /// </summary>
    private static bool _isInitialized;

    /// <summary>
    ///     Indicates whether the PriorityMaster mod is active.
    /// </summary>
    internal static bool PriorityMasterActive;

    /// <summary>
    ///     Initializes PriorityMaster compatibility by reading the mod's configured maximum work priority.
    /// </summary>
    /// <remarks>
    ///     PriorityMaster only widens the vanilla priority range; it keeps the vanilla pawn work settings priority
    ///     API, so no custom get/set delegates are required. Only the maximum priority value needs to be imported.
    /// </remarks>
    internal static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        PriorityMasterActive = LoadedModManager.RunningModsListForReading.Any(m =>
            "Lauriichan.PriorityMaster".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase));
        if (!PriorityMasterActive) return;
#if DEBUG
        Logger.LogMessage("PriorityMaster detected.");
#endif
        try
        {
            var modType = AccessTools.TypeByName("PriorityMod.Core.PriorityMaster") ??
                          throw new InvalidOperationException("Could not find 'PriorityMod.Core.PriorityMaster' type.");
            var settings = AccessTools.Field(modType, "settings")?.GetValue(null) ??
                           throw new InvalidOperationException("PriorityMaster settings are not loaded.");
            var maxPriority = Traverse.Create(settings).Method("GetMaxPriority").GetValue<int>();
            if (maxPriority <= 0)
                throw new InvalidOperationException("Invalid max priority.");
            WorkManagerMod.Settings.MaxWorkTypePriority = maxPriority;
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to initialize PriorityMaster compatibility.", e);
            PriorityMasterActive = false;
        }
#if DEBUG
        Logger.LogMessage($"Max priority is {WorkManagerMod.Settings.MaxWorkTypePriority}.");
#endif
    }
}
