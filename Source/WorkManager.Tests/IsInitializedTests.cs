using System.Reflection;
using System.Runtime.Serialization;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Unit tests for <see cref="WorkManagerGameComponent.IsInitialized" />.
///     Verifies that the property correctly reflects whether both an active game is loaded
///     (<c>Current.Game != null</c>) and <c>Instance</c> is non-null — both conditions
///     must hold before it is safe to dereference <c>Instance</c> on UI paths.
///     The <c>true</c> branch (game loaded + Instance set) requires a live <c>Current.Game</c>
///     and is not constructible in unit tests; it is verified by game-context testing (MS-1).
/// </summary>
[TestFixture]
[NonParallelizable]
public class IsInitializedTests : StateIsolationTestBase
{
    private static readonly PropertyInfo InstanceProperty =
        typeof(WorkManagerGameComponent).GetProperty("Instance",
            BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    ///     Sets <c>WorkManagerGameComponent.Instance</c> to the given value via reflection.
    /// </summary>
    private static void SetInstance(object? value)
    {
        InstanceProperty.SetValue(null, value);
    }

    /// <summary>
    ///     When <c>Instance</c> is null, <c>IsInitialized</c> must return <c>false</c>.
    ///     Covers the game-less path where no game has ever been loaded (cold main menu).
    /// </summary>
    [Test]
    public void IsInitialized_WhenInstanceIsNull_ReturnsFalse()
    {
        // Arrange — simulate no game loaded, no prior Instance
        SetInstance(null);

        // Act
        var result = WorkManagerGameComponent.IsInitialized;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    ///     When <c>Instance</c> is non-null but <c>Current.Game</c> is null (unit-test default —
    ///     no live game context), <c>IsInitialized</c> must return <c>false</c>.
    ///     This is the stale-Instance scenario: the player loaded a game, then quit to the main
    ///     menu. <c>Instance</c> still holds the defunct component reference, but
    ///     <c>Current.Game</c> is null, so the guard must block dereference.
    /// </summary>
    [Test]
    public void IsInitialized_WhenInstanceIsNotNull_ButNoActiveGame_ReturnsFalse()
    {
        // Arrange — allocate a WorkManagerGameComponent without calling its constructor
        // (the constructor requires a live Game object, unavailable in unit tests).
        // Current.Game is null by default in the unit-test AppDomain — this is exactly
        // the post-quit-to-menu state that was the reported bug.
        var staleInstance =
            (WorkManagerGameComponent)FormatterServices.GetUninitializedObject(
                typeof(WorkManagerGameComponent));
        SetInstance(staleInstance);

        // Act
        var result = WorkManagerGameComponent.IsInitialized;

        // Assert — stale Instance without an active game must NOT be treated as initialized
        result.Should().BeFalse();
    }
}
