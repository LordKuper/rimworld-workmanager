using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.Common.Cache;
using LordKuper.Common.Helpers;
using LordKuper.WorkManager.Compatibility;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager.Cache;

/// <summary>
///     Caches work information for a pawn.
///     Inherits from <see cref="TimedCache" /> to provide time-based cache invalidation.
/// </summary>
internal class PawnWorkCache(Pawn pawn) : TimedCache(RimWorldTime.HoursInDay)
{
    /// <summary>
    ///     Dictionary of work types and whether they are considered bad for the pawn.
    ///     The key is the <see cref="WorkTypeDef" />, and the value indicates if it is bad.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, bool> _badWorkTypes = [];

    /// <summary>
    ///     Represents a collection that maps work types to a boolean value indicating whether the work type is considered
    ///     dangerous.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, bool> _dangerousWorkTypes = [];

    /// <summary>
    ///     Represents a mapping of work types to their associated passion levels.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, Passion> _workPassions = [];

    /// <summary>
    ///     Retrieves the passion level for the specified type of work.
    /// </summary>
    /// <remarks>
    ///     If the passion level for the specified work type is not already cached, it will be calculated
    ///     and stored for future retrieval.
    /// </remarks>
    /// <param name="workType">The type of work for which to retrieve the passion level. Cannot be <see langword="null" />.</param>
    /// <returns>The <see cref="Passion" /> level associated with the specified work type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is <see langword="null" />.</exception>
    public Passion GetWorkPassion([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_workPassions.TryGetValue(workType, out var passion)) return passion;
        passion = PawnHelper.GetWorkPassion(pawn, workType);
        _workPassions.Add(workType, passion);
        return passion;
    }

    /// <summary>
    ///     Determines whether the specified work type is considered "bad" for the pawn.
    ///     Uses the <see cref="MoreThanCapable" /> compatibility logic if active.
    ///     Results are cached for performance.
    /// </summary>
    /// <param name="workType">The work type to check.</param>
    /// <returns>
    ///     <c>true</c> if the work type is bad for the pawn; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is <c>null</c>.</exception>
    public bool IsBadWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (!MoreThanCapable.MoreThanCapableActive) return false;
        if (_badWorkTypes.TryGetValue(workType, out var work)) return work;
        var value = MoreThanCapable.IsBadWork(pawn, workType);
        _badWorkTypes.Add(workType, value);
        return value;
    }

    /// <summary>
    ///     Determines whether the specified work type is considered dangerous for the pawn.
    /// </summary>
    /// <remarks>
    ///     A work type is considered dangerous based on predefined mappings or the pawn's ideology. The
    ///     result is cached for subsequent evaluations.
    /// </remarks>
    /// <param name="workType">The work type to evaluate. Cannot be <see langword="null" />.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified work type is considered dangerous; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is <see langword="null" />.</exception>
    public bool IsDangerousWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_dangerousWorkTypes.TryGetValue(workType, out var dangerous)) return dangerous;
        dangerous = pawn.Ideo != null && pawn.Ideo.IsWorkTypeConsideredDangerous(workType);
        _dangerousWorkTypes.Add(workType, dangerous);
        return dangerous;
    }

    /// <summary>
    ///     Updates the cache for the specified time.
    ///     Clears cached work type information if the cache is invalidated.
    /// </summary>
    /// <param name="time">The current game time.</param>
    /// <returns>
    ///     <c>true</c> if the cache was updated; otherwise, <c>false</c>.
    /// </returns>
    public override bool Update(RimWorldTime time)
    {
        if (!base.Update(time)) return false;
        _badWorkTypes.Clear();
        _workPassions.Clear();
        _dangerousWorkTypes.Clear();
        return true;
    }
}