using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using LordKuper.WorkManager.Compatibility;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager;

/// <summary>
///     The main mod class for Work Manager, responsible for initialization and settings management.
/// </summary>
[UsedImplicitly]
public class WorkManagerMod : Mod
{
    /// <summary>
    ///     The unique identifier for the Work Manager mod.
    /// </summary>
    internal const string ModId = "LordKuper.WorkManager";

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkManagerMod" /> class.
    ///     Sets up logging, settings, Harmony patches, and compatibility integrations.
    /// </summary>
    /// <param name="content">The mod content pack.</param>
    public WorkManagerMod(ModContentPack content) : base(content)
    {
        Logger.LogMessage($"Initializing (v.{Assembly.GetExecutingAssembly().GetName().Version})...");
        Settings = GetSettings<Settings>();
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        WorkTab.Initialize(harmony);
        MoreThanCapable.Initialize();
    }

    /// <summary>
    ///     Gets the mod settings instance.
    /// </summary>
    internal static Settings Settings { get; private set; }

    /// <summary>
    ///     Draws the settings window contents for the mod.
    /// </summary>
    /// <param name="inRect">The rectangle area to draw the settings window contents.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Settings.DoWindowContents(inRect);
    }

    /// <summary>
    ///     Returns a unique input ID for the mod by adding an offset to the local ID.
    ///     This ensures that input IDs used within the mod do not conflict with those from other mods or the base game.
    /// </summary>
    /// <param name="localId">
    ///     The local input ID to be offset. This should be a unique identifier within the context of the mod.
    /// </param>
    /// <returns>
    ///     An integer representing the unique mod input ID, calculated by adding the hash code of the mod's unique identifier
    ///     (<see cref="ModId" />) to the provided <paramref name="localId" />.
    /// </returns>
    internal static int GetModInputId(int localId)
    {
        return ModId.GetHashCode() + localId;
    }

    /// <summary>
    ///     Opens the Mod Settings window, allowing the user to configure settings for the current mod.
    /// </summary>
    internal static void OpenModSettingsWindow()
    {
        Find.WindowStack.Add(new Dialog_ModSettings(Settings.Mod));
    }

    /// <summary>
    ///     Gets the settings category label for the mod.
    /// </summary>
    /// <returns>The settings category label.</returns>
    public override string SettingsCategory()
    {
        return Resources.Strings.ModTitle;
    }

    /// <summary>
    ///     Writes the current settings to the appropriate game component, ensuring they are updated in the game's settings
    ///     cache.
    /// </summary>
    public override void WriteSettings()
    {
        base.WriteSettings();
        Settings.Validate();
        WorkManagerGameComponent.Instance?.UpdateSettingsCache();
    }
}