using JetBrains.Annotations;
using LordKuper.Common.Cache;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager;

/// <summary>
///     A level range for a single <see cref="NeedDef" />: a pawn is allowed the work type only while its level for the
///     need (<c>CurLevelPercentage</c>, 0..1) is inside the inclusive range <see cref="Limit" />; outside it the work
///     type is blocked. Only the def name and the range are persisted.
/// </summary>
internal sealed class NeedLimit : DefCache<NeedDef>, IExposable
{
    /// <summary>
    ///     The lower bound given to a newly added need limit (50%).
    /// </summary>
    internal const float DefaultMin = 0.5f;

    /// <summary>
    ///     The upper bound given to a newly added need limit (100%).
    /// </summary>
    internal const float DefaultMax = 1f;

    /// <summary>
    ///     The settings node name used by earlier builds for the single threshold.
    /// </summary>
    private const string LegacyThresholdNodeName = "Threshold";

    /// <summary>
    ///     The allowed need level range, as fractions in [0, 1] of the need's maximum, inclusive at both ends.
    /// </summary>
    public FloatRange Limit = new(DefaultMin, DefaultMax);

    /// <summary>
    ///     Initializes a new instance of the <see cref="NeedLimit" /> class for deserialization.
    /// </summary>
    [UsedImplicitly]
    public NeedLimit() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NeedLimit" /> class for the need with the specified def name.
    /// </summary>
    /// <param name="defName">The def name of the need.</param>
    public NeedLimit(string defName) : base(defName) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NeedLimit" /> class for the specified need.
    /// </summary>
    /// <param name="def">The need definition.</param>
    public NeedLimit(NeedDef def) : base(GetDefName(def)) { }

    /// <summary>
    ///     Serializes the def name and the range. Settings saved before the range existed hold a single legacy
    ///     <c>Threshold</c>; it is migrated to the range [threshold, 1]. Touches only values, never <c>Def</c>.
    /// </summary>
    public new void ExposeData()
    {
        base.ExposeData();
        var limit = Scribe.mode == LoadSaveMode.Saving ? Limit : new FloatRange(float.NaN, float.NaN);
        Scribe_Values.Look(ref limit, nameof(Limit), new FloatRange(float.NaN, float.NaN));
        var legacyThreshold = float.NaN;
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            Scribe_Values.Look(ref legacyThreshold, LegacyThresholdNodeName, float.NaN);
        if (Scribe.mode != LoadSaveMode.LoadingVars) return;
        if (!float.IsNaN(limit.min) || !float.IsNaN(limit.max)) Limit = limit;
        else if (!float.IsNaN(legacyThreshold)) Limit = new FloatRange(legacyThreshold, DefaultMax);
    }
}
