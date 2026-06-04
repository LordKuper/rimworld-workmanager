using LordKuper.WorkManager.Helpers;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Placeholder tests for <see cref="PassionHelper.GetPassionScore" /> method (AC-5).
///
///     PassionHelper.GetPassionScore is a thin wrapper around LordKuper.Common.Helpers.PassionHelper,
///     which computes normalized passion scores based on skill learn/forget rates.
///     The normalization logic resides in Common and requires full RimWorld game context
///     (game initialization, def databases, UI system) to execute.
///
///     Manual verification required (see manual-steps.md MS-?: Verify passion score normalization):
///     - Launch RimWorld with WorkManager mod enabled
///     - Verify that passion scores are normalized to [0, 1] range
///     - Confirm different passion levels (None, Minor, Major) produce different normalized scores
///     - Expected: scores follow the formula min→0, max→1, proportionally distributed
/// </summary>
[TestFixture]
public class PassionHelperTests
{
    // No automated tests possible without full RimWorld game context.
    // See class documentation for manual verification spec.
}
