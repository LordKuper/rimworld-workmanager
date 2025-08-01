using System;
using HarmonyLib;
using Verse;

namespace LordKuper.WorkManager.Compatibility
{
    /// <summary>
    ///     Provides compatibility checks and initialization for supported mods.
    /// </summary>
    internal static class Compatibility
    {
        /// <summary>
        ///     Indicates whether the More Than Capable mod is active.
        /// </summary>
        internal static bool MoreThanCapableActive;

        /// <summary>
        ///     Indicates whether the Vanilla Skills Expanded mod is active.
        /// </summary>
        internal static bool VanillaSkillsExpandedActive;

        /// <summary>
        ///     Indicates whether the Work Tab mod is active.
        /// </summary>
        internal static bool WorkTabActive;

        /// <summary>
        ///     Initializes compatibility with supported mods by checking if they are active and invoking their initialization
        ///     logic.
        /// </summary>
        /// <param name="harmony">The Harmony instance used for patching.</param>
        public static void Initialize(Harmony harmony)
        {
            WorkTabActive = LoadedModManager.RunningModsListForReading.Any(m =>
                "fluffy.worktab".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase));
            if (WorkTabActive)
            {
                WorkTab.Initialize(harmony);
            }
            MoreThanCapableActive = LoadedModManager.RunningModsListForReading.Any(m =>
                "notfood.MoreThanCapable".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase));
            if (MoreThanCapableActive)
            {
                MoreThanCapable.Initialize();
            }
            VanillaSkillsExpandedActive = LoadedModManager.RunningModsListForReading.Any(m =>
                "vanillaexpanded.skills".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase));
            if (VanillaSkillsExpandedActive)
            {
                Vse.Initialize();
            }
        }
    }
}