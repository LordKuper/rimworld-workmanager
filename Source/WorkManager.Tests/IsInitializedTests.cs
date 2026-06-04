using System.Reflection;
using System.Runtime.Serialization;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Unit tests for <see cref="WorkManagerGameComponent.IsInitialized" />.
///     Verifies that the property correctly reflects whether <c>Instance</c> is non-null,
///     which determines whether it is safe to dereference <c>Instance</c> on UI paths.
/// </summary>
[TestFixture]
[NonParallelizable]
public class IsInitializedTests : StateIsolationTestBase
{
    private static readonly PropertyInfo InstanceProperty =
        typeof(WorkManagerGameComponent).GetProperty(
            "Instance",
            BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    ///     Sets <c>WorkManagerGameComponent.Instance</c> to the given value via reflection.
    /// </summary>
    private static void SetInstance(object? value) => InstanceProperty.SetValue(null, value);

    /// <summary>
    ///     When <c>Instance</c> is null, <c>IsInitialized</c> must return <c>false</c>.
    ///     This matches the game-less state (no active game loaded).
    /// </summary>
    [Test]
    public void IsInitialized_WhenInstanceIsNull_ReturnsFalse()
    {
        // Arrange — simulate no game loaded
        SetInstance(null);

        // Act
        var result = WorkManagerGameComponent.IsInitialized;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    ///     When <c>Instance</c> is non-null, <c>IsInitialized</c> must return <c>true</c>.
    ///     This matches the in-game state where the component constructor has run.
    ///     We use <see cref="FormatterServices.GetUninitializedObject" /> to allocate a
    ///     <see cref="WorkManagerGameComponent" /> without invoking its constructor, which
    ///     would require a live <c>Game</c> object.
    /// </summary>
    [Test]
    public void IsInitialized_WhenInstanceIsNotNull_ReturnsTrue()
    {
        // Arrange — allocate a real WorkManagerGameComponent without calling the constructor
        // (the constructor requires a live Game object, which is unavailable in unit tests).
        var fakeInstance = (WorkManagerGameComponent)
            FormatterServices.GetUninitializedObject(typeof(WorkManagerGameComponent));
        SetInstance(fakeInstance);

        // Act
        var result = WorkManagerGameComponent.IsInitialized;

        // Assert
        result.Should().BeTrue();
    }
}
