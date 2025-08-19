using System;
using System.Collections.Generic;
using HarmonyLib;
using LordKuper.WorkManager.Patches;
using Verse;

namespace LordKuper.WorkManager.Compatibility;

/// <summary>
///     Provides compatibility integration with the WorkTab mod.
/// </summary>
internal static class WorkTab
{
    /// <summary>
    ///     Indicates whether the WorkTab compatibility has been initialized.
    /// </summary>
    private static bool _isInitialized;

    /// <summary>
    ///     Delegate for retrieving a pawn's work priority for a specific work type and hour.
    /// </summary>
    internal static GetPriorityDelegate GetPriority;

    /// <summary>
    ///     Delegate for setting a pawn's work priority for a specific work type, priority, and list of hours.
    /// </summary>
    internal static SetPriorityDelegate SetPriority;

    /// <summary>
    ///     Indicates whether the Work Tab mod is active.
    /// </summary>
    internal static bool WorkTabActive;

    /// <summary>
    ///     Initializes WorkTab compatibility by applying patches and setting up delegates for priority management.
    /// </summary>
    /// <param name="harmony">The Harmony instance used for patching.</param>
    internal static void Initialize(Harmony harmony)
    {
        if (_isInitialized) return;
        _isInitialized = true;
        WorkTabActive = LoadedModManager.RunningModsListForReading.Any(m =>
            "fluffy.worktab".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase));
        if (!WorkTabActive) return;
#if DEBUG
        Logger.LogMessage("WorkTab detected.");
#endif
        try
        {
            WorkTabPatch.Apply(harmony);
            var pawnExtensions = AccessTools.TypeByName("WorkTab.Pawn_Extensions") ??
                                 throw new InvalidOperationException("Could not find 'Pawn_Extensions' type.");
            GetPriority = AccessTools.MethodDelegate<GetPriorityDelegate>(AccessTools.Method(pawnExtensions,
                "GetPriority", [typeof(Pawn), typeof(WorkTypeDef), typeof(int)]));
            if (GetPriority == null)
                throw new InvalidOperationException("Could not create 'GetPriority' method delegate.");
            SetPriority = AccessTools.MethodDelegate<SetPriorityDelegate>(AccessTools.Method(pawnExtensions,
                "SetPriority", [typeof(Pawn), typeof(WorkTypeDef), typeof(int), typeof(List<int>)]));
            if (SetPriority == null)
                throw new InvalidOperationException("Could not create 'SetPriority' method delegate.");
            var settingsType = AccessTools.TypeByName("WorkTab.Settings") ??
                               throw new InvalidOperationException("Could not find 'WorkTab.Settings' type.");
            WorkManagerMod.Settings.MaxWorkTypePriority = Traverse.Create(settingsType).Field<int>("maxPriority").Value;
            if (WorkManagerMod.Settings.MaxWorkTypePriority <= 0)
                throw new InvalidOperationException("Invalid max priority.");
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to initialize WorkTab compatibility.", e);
        }
#if DEBUG
        Logger.LogMessage($"Max priority is {WorkManagerMod.Settings.MaxWorkTypePriority}.");
#endif
    }

    /// <summary>
    ///     Delegate signature for retrieving a pawn's work priority for a given work type and hour.
    /// </summary>
    /// <param name="pawn">The pawn whose priority is being queried.</param>
    /// <param name="workType">The work type definition.</param>
    /// <param name="hour">The hour for which the priority is requested.</param>
    /// <returns>The work priority as an integer.</returns>
    internal delegate int GetPriorityDelegate(Pawn pawn, WorkTypeDef workType, int hour);

    /// <summary>
    ///     Delegate signature for setting a pawn's work priority for a given work type, priority, and list of hours.
    /// </summary>
    /// <param name="pawn">The pawn whose priority is being set.</param>
    /// <param name="workType">The work type definition.</param>
    /// <param name="priority">The priority value to set.</param>
    /// <param name="hours">The list of hours for which to set the priority.</param>
    internal delegate void SetPriorityDelegate(Pawn pawn, WorkTypeDef workType, int priority, List<int> hours);
}