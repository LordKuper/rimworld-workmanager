using System;
using LordKuper.Common.Filters;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Tests for <see cref="DedicatedWorkerSettings.Combine" /> method.
///     Covers value-clamping and merging behavior. (AC-4)
/// </summary>
[TestFixture]
public class DedicatedWorkerSettingsTests
{
    /// <summary>
    ///     Tests that Combine uses fallback when main's AllowDedicated is null.
    /// </summary>
    [Test]
    public void Combine_AllowDedicatedNull_UsesFallback()
    {
        var main = new DedicatedWorkerSettings
        {
            AllowDedicated = null,
            Mode = DedicatedWorkerMode.Constant
        };
        var fallback = new DedicatedWorkerSettings
        {
            AllowDedicated = true
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.AllowDedicated.Should().Be(true);
    }

    /// <summary>
    ///     Tests that Combine uses main's factor when mode matches CapablePawnRatio.
    /// </summary>
    [Test]
    public void Combine_CapablePawnRatioMode_UsesMainFactor()
    {
        var main = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.CapablePawnRatio,
            CapablePawnRatioFactor = 1.5f
        };
        var fallback = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.CapablePawnRatio,
            CapablePawnRatioFactor = 2.5f
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.CapablePawnRatioFactor.Should().BeApproximately(1.5f, 0.001f);
    }

    /// <summary>
    ///     Tests that Combine uses fallback's factor when main Mode is null (Constant case).
    /// </summary>
    [Test]
    public void Combine_ConstantMode_NullMode_UsesFallback()
    {
        var main = new DedicatedWorkerSettings
        {
            Mode = null,
            ConstantWorkerCount = 5
        };
        var fallback = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.Constant,
            ConstantWorkerCount = 3
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.Mode.Should().Be(DedicatedWorkerMode.Constant);
        combined.ConstantWorkerCount.Should().Be(fallback.ConstantWorkerCount);
    }

    /// <summary>
    ///     Tests that Combine uses main's factor when mode matches Constant.
    /// </summary>
    [Test]
    public void Combine_ConstantMode_UsesMainCount()
    {
        var main = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.Constant,
            ConstantWorkerCount = 7
        };
        var fallback = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.Constant,
            ConstantWorkerCount = 2
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.ConstantWorkerCount.Should().Be(7);
    }

    /// <summary>
    ///     Tests that Combine throws when fallback is null.
    /// </summary>
    [Test]
    public void Combine_FallbackNull_Throws()
    {
        var main = new DedicatedWorkerSettings();
        Action act = () => DedicatedWorkerSettings.Combine(main, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fallback");
    }

    /// <summary>
    ///     Tests that Combine throws when main is null.
    /// </summary>
    [Test]
    public void Combine_MainNull_Throws()
    {
        var fallback = new DedicatedWorkerSettings();
        Action act = () => DedicatedWorkerSettings.Combine(null!, fallback);
        act.Should().Throw<ArgumentNullException>().WithParameterName("main");
    }

    /// <summary>
    ///     Tests that Combine merges two settings, with main taking precedence.
    /// </summary>
    [Test]
    public void Combine_MainTakesPrecedence()
    {
        var main = new DedicatedWorkerSettings
        {
            AllowDedicated = true,
            Mode = DedicatedWorkerMode.Constant,
            ConstantWorkerCount = 5
        };
        var fallback = new DedicatedWorkerSettings
        {
            AllowDedicated = false,
            Mode = DedicatedWorkerMode.WorkTypeCount,
            WorkTypeCountFactor = 0.5f
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.AllowDedicated.Should().Be(true);
        combined.Mode.Should().Be(DedicatedWorkerMode.Constant);
        combined.ConstantWorkerCount.Should().Be(5);
    }

    /// <summary>
    ///     Tests that Combine uses main Mode when set, fallback otherwise.
    /// </summary>
    [Test]
    public void Combine_ModeSelection()
    {
        var main = new DedicatedWorkerSettings { Mode = null };
        var fallback = new DedicatedWorkerSettings { Mode = DedicatedWorkerMode.WorkTypeCount };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.Mode.Should().Be(DedicatedWorkerMode.WorkTypeCount);
    }

    /// <summary>
    ///     Tests that Combine uses main's factor when mode matches PawnCount.
    /// </summary>
    [Test]
    public void Combine_PawnCountMode_UsesMainFactor()
    {
        var filter = new PawnFilter();
        var main = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.PawnCount,
            PawnCountFactor = 1.2f,
            PawnCountFilter = filter
        };
        var fallback = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.PawnCount,
            PawnCountFactor = 0.8f
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.PawnCountFactor.Should().BeApproximately(1.2f, 0.001f);
        combined.PawnCountFilter.Should().Be(filter);
    }

    /// <summary>
    ///     Tests that Combine uses main's factor when mode matches WorkTypeCount.
    /// </summary>
    [Test]
    public void Combine_WorkTypeCountMode_UsesMainFactor()
    {
        var main = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.WorkTypeCount,
            WorkTypeCountFactor = 0.7f
        };
        var fallback = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.WorkTypeCount,
            WorkTypeCountFactor = 0.3f
        };
        var combined = DedicatedWorkerSettings.Combine(main, fallback);
        combined.WorkTypeCountFactor.Should().BeApproximately(0.7f, 0.001f);
    }

    /// <summary>
    ///     Tests that Mode property resets corresponding factor to default when mode is changed.
    /// </summary>
    [Test]
    public void Mode_Change_ResetsFactor()
    {
        var settings = new DedicatedWorkerSettings
        {
            Mode = DedicatedWorkerMode.Constant,
            ConstantWorkerCount = 5
        };
        settings.Mode = DedicatedWorkerMode.WorkTypeCount;
        settings.ConstantWorkerCount.Should().Be(1); // Reset to default
    }
}