using System;
using LordKuper.Common.Filters;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Tests for <see cref="WorkTypeAssignmentRule.GetTargetWorkersCount" /> method,
///     covering non-PawnCount modes (Constant, WorkTypeCount, CapablePawnRatio).
///     PawnCount mode requires live game context and is tested via manual verification.
/// </summary>
[TestFixture]
public class GetTargetWorkersCountTests
{
    /// <summary>
    ///     Tests that Constant mode returns the configured ConstantWorkerCount.
    /// </summary>
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(10)]
    public void GetTargetWorkersCount_ConstantMode_ReturnsConstantCount(int expected)
    {
        var rule = new WorkTypeAssignmentRule("Doctor")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.Constant,
                ConstantWorkerCount = expected
            }
        };

        // Pass null map (not used in Constant mode), arbitrary capable pawn count, arbitrary work type count
        var result = rule.GetTargetWorkersCount(null!, 10, 5);
        result.Should().Be(expected);
    }

    /// <summary>
    ///     Tests that WorkTypeCount mode calculates workers as factor * dedicatedWorkTypesCount, rounded up.
    /// </summary>
    [TestCase(0.1f, 5, 1)] // 0.1 * 5 = 0.5, rounded up to 1
    [TestCase(1f, 5, 5)] // 1 * 5 = 5
    [TestCase(0.5f, 10, 5)] // 0.5 * 10 = 5
    [TestCase(1.5f, 3, 5)] // 1.5 * 3 = 4.5, rounded up to 5
    public void GetTargetWorkersCount_WorkTypeCountMode_CalculatesCorrectly(float factor,
        int workTypeCount, int expected)
    {
        var rule = new WorkTypeAssignmentRule("Cleaning")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.WorkTypeCount,
                WorkTypeCountFactor = factor
            }
        };
        var result = rule.GetTargetWorkersCount(null!, 0, workTypeCount);
        result.Should().Be(expected);
    }

    /// <summary>
    ///     Tests that CapablePawnRatio mode calculates workers based on the ratio of capable pawns to work types.
    ///     Formula: factor * (capablePawnCount / dedicatedWorkTypesCount), rounded up.
    /// </summary>
    [TestCase(1f, 10, 2, 5)] // 1 * (10 / 2) = 5
    [TestCase(0.5f, 10, 2, 3)] // 0.5 * (10 / 2) = 2.5, rounded up to 3
    [TestCase(2f, 9, 3, 6)] // 2 * (9 / 3) = 6
    [TestCase(1.5f, 7, 2, 6)] // 1.5 * (7 / 2) = 5.25, rounded up to 6
    public void GetTargetWorkersCount_CapablePawnRatioMode_CalculatesCorrectly(float factor,
        int capablePawnCount, int workTypeCount, int expected)
    {
        var rule = new WorkTypeAssignmentRule("BasicWorker")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.CapablePawnRatio,
                CapablePawnRatioFactor = factor
            }
        };
        var result = rule.GetTargetWorkersCount(null!, capablePawnCount, workTypeCount);
        result.Should().Be(expected);
    }

    /// <summary>
    ///     Tests that CapablePawnRatio mode returns predictable values for common configurations.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_CapablePawnRatioMode_CommonScenarios()
    {
        // Scenario: 1.0 ratio factor, 30 capable pawns, 6 work types -> 5 workers
        var rule1 = new WorkTypeAssignmentRule("Doctor")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.CapablePawnRatio,
                CapablePawnRatioFactor = 1f
            }
        };
        var result1 = rule1.GetTargetWorkersCount(null!, 30, 6);
        result1.Should().Be(5); // 1 * (30 / 6) = 5

        // Scenario: 0.5 ratio factor, 20 capable pawns, 10 work types -> 1 worker
        var rule2 = new WorkTypeAssignmentRule("BasicWorker")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.CapablePawnRatio,
                CapablePawnRatioFactor = 0.5f
            }
        };
        var result2 = rule2.GetTargetWorkersCount(null!, 20, 10);
        result2.Should().Be(1); // 0.5 * (20 / 10) = 1
    }

    /// <summary>
    ///     Documents behavior of CapablePawnRatio mode with edge case workTypeCount=0.
    ///     Not a valid input (production code assumes dedicatedWorkTypesCount > 0),
    ///     but worth documenting what happens.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_CapablePawnRatioMode_ZeroWorkTypeCount_ReturnsMinValue()
    {
        var rule = new WorkTypeAssignmentRule("Patient")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.CapablePawnRatio,
                CapablePawnRatioFactor = 1f
            }
        };

        // With 0 work types: capablePawnCount / 0 = infinity (float)
        // Mathf.CeilToInt(infinity) = int.MinValue
        // This is an edge case from invalid input (production validates inputs elsewhere)
        var result = rule.GetTargetWorkersCount(null!, 10, 0);

        // Document the actual behavior (not ideal, but not tested elsewhere)
        result.Should().Be(int.MinValue);
    }

    /// <summary>
    ///     Tests that non-PawnCount modes ignore the map parameter.
    ///     The map parameter is only used in PawnCount mode to filter pawns.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_ConstantMode_IgnoresMapParameter()
    {
        var rule = new WorkTypeAssignmentRule("Hunting")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.Constant,
                ConstantWorkerCount = 2
            }
        };

        // Should return same result regardless of map (null or otherwise)
        var resultWithNull = rule.GetTargetWorkersCount(null!, 5, 3);
        resultWithNull.Should().Be(2);
    }

    /// <summary>
    ///     Tests that Constant mode returns the count regardless of other parameters.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_ConstantMode_IgnoresOtherParameters()
    {
        var rule = new WorkTypeAssignmentRule("Hauling")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.Constant,
                ConstantWorkerCount = 5
            }
        };
        var result1 = rule.GetTargetWorkersCount(null!, 100, 1);
        var result2 = rule.GetTargetWorkersCount(null!, 1, 100);
        result1.Should().Be(5);
        result2.Should().Be(5);
    }

    /// <summary>
    ///     Tests that GetTargetWorkersCount returns 0 when Mode is null.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_NullMode_ReturnsZero()
    {
        var rule = new WorkTypeAssignmentRule("Hauling")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = null
            }
        };
        var result = rule.GetTargetWorkersCount(null!, 10, 5);
        result.Should().Be(0);
    }

    /// <summary>
    ///     Tests that GetTargetWorkersCount throws when called with a PawnCount mode and null Map.
    ///     PawnCount mode requires a live Map instance and game context to filter pawns;
    ///     this test documents the failure boundary when the Map precondition is violated.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_PawnCountMode_NullMapThrows()
    {
        var rule = new WorkTypeAssignmentRule("Patient")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.PawnCount,
                PawnCountFactor = 1f,
                PawnCountFilter = new PawnFilter()
            }
        };

        // PawnCount mode calls GetFilteredPawns on the Map, which will throw NullReferenceException
        // if Map is null (GetFilteredPawns passes the map to filter logic that dereferences it).
        Action act = () => rule.GetTargetWorkersCount(null!, 10, 5);

        act.Should().Throw<NullReferenceException>();
    }

    /// <summary>
    ///     Tests that WorkTypeCount mode returns predictable values for common configurations.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_WorkTypeCountMode_CommonScenarios()
    {
        // Scenario: 10% of work types, 6 work types -> 1 worker
        var rule1 = new WorkTypeAssignmentRule("Cleaning")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.WorkTypeCount,
                WorkTypeCountFactor = 0.1f
            }
        };
        var result1 = rule1.GetTargetWorkersCount(null!, 0, 6);
        result1.Should().Be(1); // 0.1 * 6 = 0.6, rounded up to 1

        // Scenario: 20% of work types, 5 work types -> 1 worker
        var rule2 = new WorkTypeAssignmentRule("Hauling")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.WorkTypeCount,
                WorkTypeCountFactor = 0.2f
            }
        };
        var result2 = rule2.GetTargetWorkersCount(null!, 0, 5);
        result2.Should().Be(1); // 0.2 * 5 = 1
    }

    /// <summary>
    ///     Tests that WorkTypeCount mode returns 0 when workTypeCount is 0.
    /// </summary>
    [Test]
    public void GetTargetWorkersCount_WorkTypeCountMode_ZeroWorkTypeCount_ReturnsZero()
    {
        var rule = new WorkTypeAssignmentRule("Firefighter")
        {
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                Mode = DedicatedWorkerMode.WorkTypeCount,
                WorkTypeCountFactor = 1f
            }
        };
        var result = rule.GetTargetWorkersCount(null!, 0, 0);
        result.Should().Be(0);
    }
}