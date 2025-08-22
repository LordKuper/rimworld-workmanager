using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LordKuper.Common;
using LordKuper.WorkManager.Helpers;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager.Cache;

/// <summary>
///     Caches work and skill information for a specific pawn, including work priorities,
///     managed work types, and capability status. Provides efficient access to work-related
///     data and supports periodic cache updates.
/// </summary>
internal class PawnCache(Pawn pawn)
{
    /// <summary>
    ///     Stores whether each work type is managed for the pawn.
    ///     The key is the <see cref="WorkTypeDef" />, and the value indicates if it is managed.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, bool> _managedWorkTypes = [];

    /// <summary>
    ///     Stores the priority for each work type for the pawn.
    ///     The key is the <see cref="WorkTypeDef" />, and the value is the priority.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, int> _workPriorities = [];

    /// <summary>
    ///     Gets or sets the time since the pawn became idle.
    /// </summary>
    public RimWorldTime? IdleSince { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the pawn is currently capable of work.
    /// </summary>
    public bool IsCapable { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the pawn is managed by the work manager.
    /// </summary>
    public bool IsManaged { get; private set; }

    /// <summary>
    ///     Gets the pawn associated with this cache.
    /// </summary>
    public Pawn Pawn { get; } = pawn;

    /// <summary>
    ///     Gets the skill cache for the pawn.
    /// </summary>
    private PawnSkillCache Skill { get; } = new(pawn);

    /// <summary>
    ///     Gets the work cache for the pawn.
    /// </summary>
    private PawnWorkCache Work { get; } = new(pawn);

    /// <summary>
    ///     Retrieves the learning rate for a specified type of work.
    /// </summary>
    /// <param name="workType">The type of work for which to retrieve the learning rate. This parameter cannot be null.</param>
    /// <returns>The learning rate as a <see cref="float" /> for the specified work type.</returns>
    public float GetLearningRate([NotNull] WorkTypeDef workType)
    {
        return Skill.GetWorkSkillLearningRate(workType);
    }

    /// <summary>
    ///     Retrieves the passion level of the pawn for the specified type of work.
    /// </summary>
    /// <param name="workType">The type of work for which the passion level is being queried. Cannot be null.</param>
    /// <returns>The passion level of the pawn for the specified work type.</returns>
    public Passion GetWorkPassion([NotNull] WorkTypeDef workType)
    {
        return Work.GetWorkPassion(workType);
    }

    /// <summary>
    ///     Gets the priority for the specified work type.
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>The priority value for the work type, or 0 if not set.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    public int GetWorkPriority([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        return _workPriorities.TryGetValue(workType, out var priority) ? priority : 0;
    }

    /// <summary>
    ///     Gets the average skill level for the specified work type.
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>The average skill level for the work type.</returns>
    public int GetWorkSkillLevel([NotNull] WorkTypeDef workType)
    {
        return Skill.GetWorkSkillLevel(workType);
    }

    /// <summary>
    ///     Determines whether the specified work type is active for the pawn (priority &gt; 0).
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>
    ///     <c>true</c> if the work type is active for the pawn; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    public bool IsActiveWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        return _workPriorities[workType] > 0;
    }

    /// <summary>
    ///     Determines whether the specified work type is allowed for the worker.
    /// </summary>
    /// <param name="workType">The work type to evaluate. Cannot be <see langword="null" />.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified work type is allowed for the worker; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool IsAllowedWorker([NotNull] WorkTypeDef workType)
    {
        return Work.IsAllowedWorker(workType);
    }

    /// <summary>
    ///     Determines whether the specified work type is considered "bad" for the pawn.
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>
    ///     <c>true</c> if the work type is bad for the pawn; otherwise, <c>false</c>.
    /// </returns>
    public bool IsBadWork([NotNull] WorkTypeDef workType)
    {
        return Work.IsBadWork(workType);
    }

    /// <summary>
    ///     Determines whether the specified work type is considered dangerous.
    /// </summary>
    /// <param name="workType">The work type to evaluate. This parameter cannot be null.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified work type is classified as dangerous;  otherwise,
    ///     <see
    ///         langword="false" />
    ///     .
    /// </returns>
    public bool IsDangerousWork([NotNull] WorkTypeDef workType)
    {
        return Work.IsDangerousWork(workType);
    }

    /// <summary>
    ///     Determines whether the specified work type is managed for the pawn.
    ///     Results are cached for performance.
    /// </summary>
    /// <param name="workType">The work type definition.</param>
    /// <returns>
    ///     <c>true</c> if the work type is managed for the pawn; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    public bool IsManagedWork([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (_managedWorkTypes.TryGetValue(workType, out var work)) return work;
        var workManager = WorkManagerGameComponent.Instance;
        var value = IsManaged && workManager.GetWorkTypeEnabled(workType) &&
                    workManager.GetPawnWorkTypeEnabled(Pawn, workType);
        _managedWorkTypes.Add(workType, value);
        return value;
    }

    /// <summary>
    ///     Sets the priority level for the specified type of work.
    /// </summary>
    /// <remarks>
    ///     This method updates the priority level for the given work type. Higher priority values
    ///     typically indicate greater importance.
    /// </remarks>
    /// <param name="workType">
    ///     The type of work for which the priority is being set. This parameter cannot be
    ///     <see langword="null" />.
    /// </param>
    /// <param name="priority">The priority level to assign to the specified work type. Must be a non-negative integer.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="priority" /> is less than 0.</exception>
    public void SetWorkPriority([NotNull] WorkTypeDef workType, int priority)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
        _workPriorities[workType] = priority;
    }

    /// <summary>
    ///     Updates the cache for the pawn at the specified time.
    ///     Refreshes capability, management status, work priorities, and underlying caches.
    /// </summary>
    /// <param name="time">The current RimWorld time.</param>
    public void Update(RimWorldTime time)
    {
        IsCapable = !Pawn.Dead && !Pawn.InContainerEnclosed && !Pawn.InMentalState && !Pawn.Downed;
        IsManaged = WorkManagerGameComponent.Instance.GetPawnEnabled(Pawn);
        _workPriorities.Clear();
        _managedWorkTypes.Clear();
        foreach (var workType in WorkManagerGameComponent.Instance?.AllWorkTypes ?? [])
        {
            _workPriorities.Add(workType,
                IsManagedWork(workType) ? 0 : WorkTypePriorityHelper.GetPriority(Pawn, workType));
        }
        Work.Update(time);
        Skill.Update(time);
    }
}