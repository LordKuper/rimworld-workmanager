using Verse;

namespace LordKuper.WorkManager;

public partial class Settings
{
    /// <summary>
    ///     The current version of the settings schema.
    /// </summary>
    private const int CurrentVersion = 1;

    /// <summary>
    ///     The version of the settings currently stored.
    /// </summary>
    public int Version;

    /// <summary>
    ///     Serializes or deserializes the <see cref="Version" /> field using RimWorld's Scribe system.
    /// </summary>
    private void ExposeVersionData()
    {
        Scribe_Values.Look(ref Version, nameof(Version), forceSave: true);
    }

    /// <summary>
    ///     Validates the settings version and upgrades or resets settings if necessary.
    /// </summary>
    private void ValidateVersion()
    {
        if (Version == CurrentVersion) return;
        if (Version == 0)
        {
            Logger.LogMessage("Resetting all mod setting to default values...");
            ResetAll();
            Version = CurrentVersion;
        }
        else
        {
            Logger.LogMessage($"Upgrading settings from version {Version} to {CurrentVersion}...");
        }
    }
}