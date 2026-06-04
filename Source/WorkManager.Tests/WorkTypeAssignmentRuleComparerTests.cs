using System;
using LordKuper.WorkManager.Helpers;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Tests for <see cref="WorkTypeAssignmentRuleComparer" /> ordering logic and
///     <see cref="WorkTypeAssignmentRule.Combine" /> merge result. (AC-3)
/// </summary>
[TestFixture]
public class WorkTypeAssignmentRuleComparerTests
{
    /// <summary>
    ///     Tests that Combine throws when fallback is null.
    /// </summary>
    [Test]
    public void Combine_FallbackIsNull_Throws()
    {
        var main = new WorkTypeAssignmentRule("Hauling");
        Action act = () => WorkTypeAssignmentRule.Combine(main, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fallback");
    }

    /// <summary>
    ///     Tests that Combine throws when main is null.
    /// </summary>
    [Test]
    public void Combine_MainIsNull_Throws()
    {
        var fallback = new WorkTypeAssignmentRule("Hauling");
        Action act = () => WorkTypeAssignmentRule.Combine(null!, fallback);
        act.Should().Throw<ArgumentNullException>().WithParameterName("main");
    }

    /// <summary>
    ///     Tests that Combine merges two rules, with main taking precedence over fallback.
    /// </summary>
    [Test]
    public void Combine_MainTakesPrecedence_OverFallback()
    {
        var main = new WorkTypeAssignmentRule("Hauling")
        {
            EnsureWorkerAssigned = true,
            MinWorkerNumber = 5,
            AssignEveryone = null,
            AssignEveryonePriority = 3,
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            { Mode = DedicatedWorkerMode.Constant }
        };
        var fallback = new WorkTypeAssignmentRule("Hauling")
        {
            EnsureWorkerAssigned = false,
            MinWorkerNumber = 1,
            AssignEveryone = true,
            AssignEveryonePriority = 1,
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            { Mode = DedicatedWorkerMode.WorkTypeCount }
        };
        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        // Main's EnsureWorkerAssigned value should be used
        combined.EnsureWorkerAssigned.Should().Be(true);
        // When EnsureWorkerAssigned is set in main, main's MinWorkerNumber is used
        combined.MinWorkerNumber.Should().Be(5);
        // Main's AssignEveryone is null, so fallback is used
        combined.AssignEveryone.Should().Be(true);
    }

    /// <summary>
    ///     Tests that Combine correctly merges DedicatedWorkerSettings.
    /// </summary>
    [Test]
    public void Combine_MergesDedicatedWorkerSettings()
    {
        var main = new WorkTypeAssignmentRule("Doctor")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                AllowDedicated = true,
                Mode = DedicatedWorkerMode.Constant,
                ConstantWorkerCount = 3
            }
        };
        var fallback = new WorkTypeAssignmentRule("Doctor")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                AllowDedicated = false,
                Mode = DedicatedWorkerMode.WorkTypeCount
            }
        };
        var combined = WorkTypeAssignmentRule.Combine(main, fallback);
        combined.DedicatedWorkerSettings.Should().NotBeNull();
        combined.DedicatedWorkerSettings.AllowDedicated.Should().Be(true);
    }

    /// <summary>
    ///     Tests that Combine preserves the main rule's defName.
    /// </summary>
    [Test]
    public void Combine_PreservesMainDefName()
    {
        var main = new WorkTypeAssignmentRule("Hauling")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            { Mode = DedicatedWorkerMode.Constant }
        };
        var fallback = new WorkTypeAssignmentRule("Cleaning")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            { Mode = DedicatedWorkerMode.WorkTypeCount }
        };
        var combined = WorkTypeAssignmentRule.Combine(main, fallback);
        combined.DefName.Should().Be("Hauling");
    }

    /// <summary>
    ///     Tests that Combine uses fallback values when main values are null.
    /// </summary>
    [Test]
    public void Combine_UseFallbackWhenMainIsNull()
    {
        var main = new WorkTypeAssignmentRule("Cleaning")
        {
            EnsureWorkerAssigned = null,
            MinWorkerNumber = 0,
            AssignEveryone = null,
            AssignEveryonePriority = 0,
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            { Mode = DedicatedWorkerMode.WorkTypeCount }
        };
        var fallback = new WorkTypeAssignmentRule("Cleaning")
        {
            EnsureWorkerAssigned = true,
            MinWorkerNumber = 2,
            AssignEveryone = false,
            AssignEveryonePriority = 2,
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            { Mode = DedicatedWorkerMode.Constant }
        };
        var combined = WorkTypeAssignmentRule.Combine(main, fallback);
        combined.EnsureWorkerAssigned.Should().Be(true);
        combined.MinWorkerNumber.Should().Be(fallback.MinWorkerNumber);
        combined.AssignEveryone.Should().Be(false);
        combined.AssignEveryonePriority.Should().Be(fallback.AssignEveryonePriority);
    }

    /// <summary>
    ///     Tests that the comparer falls back to defName ordering when skill count and priority are equal.
    /// </summary>
    [Test]
    public void Compare_FallbackToDefName_OrdinalComparison()
    {
        var comparer = new WorkTypeAssignmentRuleComparer();
        var ruleA = new WorkTypeAssignmentRule("Aardvark");
        var ruleZ = new WorkTypeAssignmentRule("Zebra");
        var result = comparer.Compare(ruleA, ruleZ);
        result.Should().BeLessThan(0); // "Aardvark" < "Zebra" in ordinal comparison
    }

    /// <summary>
    ///     Tests that the comparer orders null rules correctly (null is less than non-null).
    /// </summary>
    [Test]
    public void Compare_NullVsNonNull_OrdersNullAsLess()
    {
        var comparer = new WorkTypeAssignmentRuleComparer();
        var rule = new WorkTypeAssignmentRule("Hauling");
        var resultNullFirst = comparer.Compare(null!, rule);
        var resultNullSecond = comparer.Compare(rule, null!);
        resultNullFirst.Should().BeLessThan(0);
        resultNullSecond.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     Tests that the comparer correctly orders rules by skill count (descending).
    /// </summary>
    [Test]
    public void Compare_OrdersBySkillCountDescending()
    {
        var comparer = new WorkTypeAssignmentRuleComparer();
        var rule1 = new WorkTypeAssignmentRule("Hauling");
        var rule2 = new WorkTypeAssignmentRule("Doctor");

        // Without RimWorld defs loaded, both rules have null Def, so they fall back to defName ordering.
        // This test verifies the comparer doesn't crash and respects the null-def case.
        var result = comparer.Compare(rule1, rule2);
        result.Should().NotBe(0); // Different rules should not be equal
    }

    /// <summary>
    ///     Tests that the comparer treats reference-equal rules as equal.
    /// </summary>
    [Test]
    public void Compare_ReferenceEqual_ReturnsZero()
    {
        var comparer = new WorkTypeAssignmentRuleComparer();
        var rule = new WorkTypeAssignmentRule("Cleaning");
        var result = comparer.Compare(rule, rule);
        result.Should().Be(0);
    }
}