using System;
using JetBrains.Annotations;
using LordKuper.Common.Filters;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager;

/// <summary>
///     Stores configuration settings for dedicated worker management.
/// </summary>
internal class DedicatedWorkerSettings : IExposable
{
    /// <summary>
    ///     The default value for <see cref="CapablePawnRatioFactor" />.
    /// </summary>
    private const float CapablePawnRatioFactorDefault = 1f;

    /// <summary>
    ///     The maximum allowed value for <see cref="CapablePawnRatioFactor" />.
    /// </summary>
    public const float CapablePawnRatioFactorMax = 5f;

    /// <summary>
    ///     The minimum allowed value for <see cref="CapablePawnRatioFactor" />.
    /// </summary>
    public const float CapablePawnRatioFactorMin = 0.1f;

    /// <summary>
    ///     The default value for <see cref="ConstantWorkerCount" />.
    /// </summary>
    private const int ConstantWorkerCountDefault = 1;

    /// <summary>
    ///     The maximum allowed value for <see cref="ConstantWorkerCount" />.
    /// </summary>
    public const int ConstantWorkerCountMax = 10;

    /// <summary>
    ///     The minimum allowed value for <see cref="ConstantWorkerCount" />.
    /// </summary>
    public const int ConstantWorkerCountMin = 1;

    /// <summary>
    ///     The default value for <see cref="PawnCountFactor" />.
    /// </summary>
    private const float PawnCountFactorDefault = 1f;

    /// <summary>
    ///     The maximum allowed value for <see cref="PawnCountFactor" />.
    /// </summary>
    public const float PawnCountFactorMax = 5f;

    /// <summary>
    ///     The minimum allowed value for <see cref="PawnCountFactor" />.
    /// </summary>
    public const float PawnCountFactorMin = 0.1f;

    /// <summary>
    ///     The default value for <see cref="WorkTypeCountFactor" />.
    /// </summary>
    private const float WorkTypeCountFactorDefault = 0.1f;

    /// <summary>
    ///     The maximum allowed value for <see cref="WorkTypeCountFactor" />.
    /// </summary>
    public const float WorkTypeCountFactorMax = 2f;

    /// <summary>
    ///     The minimum allowed value for <see cref="WorkTypeCountFactor" />.
    /// </summary>
    public const float WorkTypeCountFactorMin = 1f / 20f;

    /// <summary>
    ///     Represents the mode of operation for a dedicated worker.
    /// </summary>
    private DedicatedWorkerMode? _mode;

    /// <summary>
    ///     Indicates whether dedicated workers are allowed.
    /// </summary>
    public bool? AllowDedicated;

    /// <summary>
    ///     Multiplier for the ratio of capable pawns when calculating dedicated workers.
    /// </summary>
    public float CapablePawnRatioFactor = CapablePawnRatioFactorDefault;

    /// <summary>
    ///     The constant number of dedicated workers to assign.
    /// </summary>
    public int ConstantWorkerCount = ConstantWorkerCountDefault;

    /// <summary>
    ///     Multiplier for the pawn count when calculating dedicated workers.
    /// </summary>
    public float PawnCountFactor = PawnCountFactorDefault;

    /// <summary>
    ///     Filter used when <see cref="Mode" /> is <see cref="DedicatedWorkerMode.PawnCount" />.
    /// </summary>
    public PawnFilter PawnCountFilter;

    /// <summary>
    ///     Indicates whether tri-state mode is enabled.
    /// </summary>
    public bool TriStateMode;

    /// <summary>
    ///     Multiplier for the work type count when calculating dedicated workers.
    /// </summary>
    public float WorkTypeCountFactor = WorkTypeCountFactorDefault;

    /// <summary>
    ///     The mode used for dedicated worker calculation.
    /// </summary>
    public DedicatedWorkerMode? Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            if (_mode != DedicatedWorkerMode.Constant) ConstantWorkerCount = ConstantWorkerCountDefault;
            if (_mode != DedicatedWorkerMode.WorkTypeCount) WorkTypeCountFactor = WorkTypeCountFactorDefault;
            if (_mode != DedicatedWorkerMode.CapablePawnRatio) CapablePawnRatioFactor = CapablePawnRatioFactorDefault;
            PawnCountFilter = _mode == DedicatedWorkerMode.PawnCount ? new PawnFilter { TriStateMode = false } : null;
        }
    }

    /// <summary>
    ///     Serializes and deserializes the settings data.
    /// </summary>
    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) Validate();
        Scribe_Values.Look(ref TriStateMode, nameof(TriStateMode));
        Scribe_Values.Look(ref AllowDedicated, nameof(AllowDedicated), true);
        Scribe_Values.Look(ref _mode, nameof(Mode));
        Scribe_Values.Look(ref ConstantWorkerCount, nameof(ConstantWorkerCount), ConstantWorkerCountDefault);
        Scribe_Values.Look(ref WorkTypeCountFactor, nameof(WorkTypeCountFactor), WorkTypeCountFactorDefault);
        Scribe_Values.Look(ref CapablePawnRatioFactor, nameof(CapablePawnRatioFactor), CapablePawnRatioFactorDefault);
        Scribe_Values.Look(ref PawnCountFactor, nameof(PawnCountFactor), PawnCountFactorDefault);
        Scribe_Deep.Look(ref PawnCountFilter, nameof(PawnCountFilter));
    }

    /// <summary>
    ///     Combines two <see cref="DedicatedWorkerSettings" /> instances into a single configuration.
    /// </summary>
    /// <remarks>
    ///     The resulting configuration prioritizes values from <paramref name="main" />. If a value is
    ///     not specified in <paramref name="main" />, the corresponding value from <paramref name="fallback" /> is used. The
    ///     <c>Mode</c> property determines additional behavior, such as which specific settings are applied.
    /// </remarks>
    /// <param name="main">
    ///     The primary <see cref="DedicatedWorkerSettings" /> instance. Values from this instance take precedence if
    ///     specified.
    /// </param>
    /// <param name="fallback">
    ///     The fallback <see cref="DedicatedWorkerSettings" /> instance. Values from this instance are used if not specified
    ///     in <paramref name="main" />.
    /// </param>
    /// <returns>
    ///     A new <see cref="DedicatedWorkerSettings" /> instance that merges the values from <paramref name="main" /> and
    ///     <paramref name="fallback" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="main" /> or <paramref name="fallback" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [NotNull]
    public static DedicatedWorkerSettings Combine([NotNull] DedicatedWorkerSettings main,
        [NotNull] DedicatedWorkerSettings fallback)
    {
        if (main == null) throw new ArgumentNullException(nameof(main));
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        var settings = new DedicatedWorkerSettings
        {
            TriStateMode = false,
            AllowDedicated = main.AllowDedicated ?? fallback.AllowDedicated,
            Mode = main.Mode ?? fallback.Mode
        };
        switch (settings.Mode)
        {
            case DedicatedWorkerMode.Constant:
                settings.ConstantWorkerCount =
                    main.Mode.HasValue ? main.ConstantWorkerCount : fallback.ConstantWorkerCount;
                break;
            case DedicatedWorkerMode.WorkTypeCount:
                settings.WorkTypeCountFactor =
                    main.Mode.HasValue ? main.WorkTypeCountFactor : fallback.WorkTypeCountFactor;
                break;
            case DedicatedWorkerMode.CapablePawnRatio:
                settings.CapablePawnRatioFactor =
                    main.Mode.HasValue ? main.CapablePawnRatioFactor : fallback.CapablePawnRatioFactor;
                break;
            case DedicatedWorkerMode.PawnCount:
                settings.PawnCountFactor = main.Mode.HasValue ? main.PawnCountFactor : fallback.PawnCountFactor;
                settings.PawnCountFilter = main.Mode.HasValue ? main.PawnCountFilter : fallback.PawnCountFilter;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return settings;
    }

    /// <summary>
    ///     Validates and clamps all configurable values to their allowed ranges.
    /// </summary>
    private void Validate()
    {
        if (!TriStateMode)
        {
            AllowDedicated ??= true;
            Mode ??= DedicatedWorkerMode.WorkTypeCount;
        }
        if (Mode == DedicatedWorkerMode.PawnCount && PawnCountFilter == null)
            PawnCountFilter = new PawnFilter();
        ConstantWorkerCount = Mathf.Clamp(ConstantWorkerCount, ConstantWorkerCountMin, ConstantWorkerCountMax);
        WorkTypeCountFactor = Mathf.Clamp(WorkTypeCountFactor, WorkTypeCountFactorMin, WorkTypeCountFactorMax);
        CapablePawnRatioFactor = Mathf.Clamp(CapablePawnRatioFactor, CapablePawnRatioFactorMin,
            CapablePawnRatioFactorMax);
        PawnCountFactor = Mathf.Clamp(PawnCountFactor, PawnCountFactorMin, PawnCountFactorMax);
    }
}