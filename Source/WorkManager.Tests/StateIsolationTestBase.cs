using System;
using System.Reflection;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Abstract base class for tests that mutate global or static state, providing snapshot/restore isolation
///     via [SetUp] and [TearDown] to ensure test independence.
/// </summary>
[NonParallelizable]
public abstract class StateIsolationTestBase
{
    private object? _snapshotDefaultRule;
    private object? _snapshotDefaultRulesByName;
    private object? _snapshotGameComponent;

    /// <summary>
    ///     Gets the value of a static field via reflection.
    /// </summary>
    private static object? GetStaticFieldValue(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        return field?.GetValue(null);
    }

    /// <summary>
    ///     Gets the current value of WorkManagerGameComponent.Instance via reflection.
    /// </summary>
    private static object? GetWorkManagerGameComponentInstance()
    {
        var type = typeof(WorkManagerGameComponent);
        var property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)
                      ?? throw new InvalidOperationException(
                          "WorkManagerGameComponent.Instance property not found; test infrastructure may be out of sync with production code");
        return property.GetValue(null);
    }

    /// <summary>
    ///     Restores the snapshotted mutable static state after each test.
    /// </summary>
    [TearDown]
    public void RestoreState()
    {
        // Restore WorkManagerGameComponent.Instance
        SetWorkManagerGameComponentInstance(_snapshotGameComponent);

        // Restore WorkTypeAssignmentRule static caches
        var workTypeAssignmentRuleType = typeof(WorkTypeAssignmentRule);
        SetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRule", _snapshotDefaultRule);
        SetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRulesByName",
            _snapshotDefaultRulesByName);
    }

    /// <summary>
    ///     Sets the value of a static field via reflection.
    /// </summary>
    private static void SetStaticFieldValue(Type type, string fieldName, object? value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                   ?? throw new InvalidOperationException(
                       $"Static field {type.Name}.{fieldName} not found; test infrastructure may be out of sync with production code");
        field.SetValue(null, value);
    }

    /// <summary>
    ///     Sets the value of WorkManagerGameComponent.Instance via reflection.
    /// </summary>
    private static void SetWorkManagerGameComponentInstance(object? value)
    {
        var type = typeof(WorkManagerGameComponent);
        var property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)
                      ?? throw new InvalidOperationException(
                          "WorkManagerGameComponent.Instance property not found; test infrastructure may be out of sync with production code");
        var setter = property.GetSetMethod(true)
                    ?? throw new InvalidOperationException(
                        "WorkManagerGameComponent.Instance property has no setter; test infrastructure may be out of sync with production code");
        setter.Invoke(null, [value]);
    }

    /// <summary>
    ///     Snapshots the relevant mutable static state before each test.
    /// </summary>
    [SetUp]
    public void SnapshotState()
    {
        // Snapshot WorkManagerGameComponent.Instance
        _snapshotGameComponent = GetWorkManagerGameComponentInstance();

        // Snapshot WorkTypeAssignmentRule static caches
        var workTypeAssignmentRuleType = typeof(WorkTypeAssignmentRule);
        _snapshotDefaultRule = GetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRule");
        _snapshotDefaultRulesByName =
            GetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRulesByName");
    }
}