using LordKuper.WorkManager.Helpers;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Tests for <see cref="PassionHelper.GetPassionScore" /> method.
///     PassionHelper.GetPassionScore is a thin wrapper around LordKuper.Common.Helpers.PassionHelper,
///     which computes normalized passion scores based on skill learn/forget rates.
///     The normalization logic resides in Common and requires full RimWorld game context
///     (game initialization, def databases, UI system) to execute deterministically.
///     Unit testing of passion-score normalization is not feasible without live game context.
///     Behavior verification: Manual step required to confirm that passion scores normalize to [0, 1] range
///     across different passion levels (None, Minor, Major), with scores proportionally distributed.
/// </summary>
[TestFixture]
public class PassionHelperTests
{
    // No automated tests possible without full RimWorld game context.
    // See class documentation for manual verification spec.
}