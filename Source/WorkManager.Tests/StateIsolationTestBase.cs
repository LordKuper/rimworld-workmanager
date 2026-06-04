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
        var type = GetWorkManagerGameComponentType();
        if (type == null) return null;
        var property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic);
        return property?.GetValue(null);
    }

    /// <summary>
    ///     Gets the Type of WorkManagerGameComponent via reflection.
    /// </summary>
    private static Type? GetWorkManagerGameComponentType()
    {
        return Type.GetType("LordKuper.WorkManager.WorkManagerGameComponent");
    }

    /// <summary>
    ///     Gets the Type of WorkTypeAssignmentRule.
    ///     Uses the direct typeof operator rather than string-based lookup, which only searches
    ///     mscorlib and the calling assembly and would fail for types in other assemblies.
    /// </summary>
    private static Type GetWorkTypeAssignmentRuleType()
    {
        return typeof(WorkTypeAssignmentRule);
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
        var workTypeAssignmentRuleType = GetWorkTypeAssignmentRuleType();
        SetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRule", _snapshotDefaultRule);
        SetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRulesByName",
            _snapshotDefaultRulesByName);
    }

    /// <summary>
    ///     Sets the value of a static field via reflection.
    /// </summary>
    private static void SetStaticFieldValue(Type type, string fieldName, object? value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        if (field != null) field.SetValue(null, value);
    }

    /// <summary>
    ///     Sets the value of WorkManagerGameComponent.Instance via reflection.
    /// </summary>
    private static void SetWorkManagerGameComponentInstance(object? value)
    {
        var type = GetWorkManagerGameComponentType();
        if (type == null) return;
        var property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic);
        if (property?.GetSetMethod(true) != null) property.SetValue(null, value);
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
        var workTypeAssignmentRuleType = GetWorkTypeAssignmentRuleType();
        _snapshotDefaultRule = GetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRule");
        _snapshotDefaultRulesByName =
            GetStaticFieldValue(workTypeAssignmentRuleType, "_defaultRulesByName");
    }
}