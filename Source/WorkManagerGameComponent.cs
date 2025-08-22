using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.WorkManager.Helpers;
using Verse;

namespace LordKuper.WorkManager;

/// <summary>
///     Game component for managing work assignments, priorities, and schedules for pawns.
///     Handles enabling/disabling work types, pawn work assignments, and caches assignment rules.
/// </summary>
[UsedImplicitly]
public class WorkManagerGameComponent : GameComponent
{
    /// <summary>
    ///     Dictionary of work types assigned to everyone, with their priorities.
    /// </summary>
    private readonly Dictionary<WorkTypeDef, int> _assignEveryoneWorkTypes = [];

    /// <summary>
    ///     Represents a collection of combined work type assignment rules, sorted according to a specified comparer.
    /// </summary>
    internal readonly SortedSet<WorkTypeAssignmentRule> CombinedRules = new(WorkTypeAssignmentRuleComparer);

    /// <summary>
    ///     List of pawns with disabled work assignments.
    /// </summary>
    private List<Pawn> _disabledPawns = [];

    /// <summary>
    ///     List of pawns with disabled schedules.
    /// </summary>
    private List<Pawn> _disabledPawnSchedules = [];

    /// <summary>
    ///     Dictionary of pawns and their disabled work types.
    /// </summary>
    private Dictionary<Pawn, WorkTypeDef> _disabledPawnWorkTypes = [];

    /// <summary>
    ///     List of disabled work types.
    /// </summary>
    private List<WorkTypeDef> _disabledWorkTypes = [];

    /// <summary>
    ///     Indicates if priority management is enabled.
    /// </summary>
    public bool PriorityManagementEnabled = true;

    /// <summary>
    ///     Indicates if schedule management is enabled.
    /// </summary>
    public bool ScheduleManagementEnabled = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkManagerGameComponent" /> class and sets the singleton instance.
    /// </summary>
    /// <remarks>
    ///     This constructor sets the <see cref="Instance" /> property to the current instance, ensuring
    ///     that only one instance of <see cref="WorkManagerGameComponent" /> exists at a time. This component is typically
    ///     used to manage work-related tasks within the game.
    /// </remarks>
    /// <param name="game">The game instance to associate with this component. Cannot be null.</param>
    public WorkManagerGameComponent(Game game)
    {
        Instance = this;
    }

    /// <summary>
    ///     Gets the dictionary of work types assigned to everyone and their priorities.
    /// </summary>
    internal IReadOnlyDictionary<WorkTypeDef, int> AssignEveryoneWorkTypes => _assignEveryoneWorkTypes;

    /// <summary>
    ///     Gets the set of work types that configured to use dedicated workers.
    /// </summary>
    internal HashSet<WorkTypeDef> DedicatedWorkTypes { get; } = [];

    /// <summary>
    ///     Gets the list of pawns with disabled work assignments.
    /// </summary>
    public IReadOnlyList<Pawn> DisabledPawns => _disabledPawns;

    /// <summary>
    ///     Gets the list of pawns with disabled schedules.
    /// </summary>
    public IReadOnlyList<Pawn> DisabledPawnSchedules => _disabledPawnSchedules;

    /// <summary>
    ///     Gets the dictionary of pawns and their disabled work types.
    /// </summary>
    public IReadOnlyDictionary<Pawn, WorkTypeDef> DisabledPawnWorkTypes => _disabledPawnWorkTypes;

    /// <summary>
    ///     Gets the list of disabled work types.
    /// </summary>
    public IReadOnlyList<WorkTypeDef> DisabledWorkTypes => _disabledWorkTypes;

    /// <summary>
    ///     Gets the singleton instance of the <see cref="WorkManagerGameComponent" /> class.
    /// </summary>
    internal static WorkManagerGameComponent Instance { get; private set; }

    /// <summary>
    ///     Gets a comparer for evaluating work type assignment rules.
    /// </summary>
    internal static WorkTypeAssignmentRuleComparer WorkTypeAssignmentRuleComparer { get; } = new();

    /// <summary>
    ///     Serializes and deserializes the component's data.
    /// </summary>
    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) Validate();
        base.ExposeData();
        Scribe_Values.Look(ref PriorityManagementEnabled, nameof(PriorityManagementEnabled), true);
        Scribe_Values.Look(ref ScheduleManagementEnabled, nameof(ScheduleManagementEnabled), true);
        Scribe_Collections.Look(ref _disabledWorkTypes, nameof(DisabledWorkTypes), LookMode.Def);
        Scribe_Collections.Look(ref _disabledPawns, nameof(DisabledPawns), LookMode.Reference);
        Scribe_Collections.Look(ref _disabledPawnWorkTypes, nameof(DisabledPawnWorkTypes), LookMode.Deep);
        Scribe_Collections.Look(ref _disabledPawnSchedules, nameof(DisabledPawnSchedules), LookMode.Reference);
    }

    /// <summary>
    ///     Forces an update of work assignments for the current map.
    /// </summary>
    internal static void ForceUpdateAssignments()
    {
        var map = Find.CurrentMap;
#if DEBUG
        if (map == null) Logger.LogMessage("Can not force update assignments (map = null).");
        Logger.LogMessage($"Force updating assignments for {map}");
#endif
        if (map == null) return;
        var priorityUpdater = map.GetComponent<WorkPriorityUpdater>();
        if (priorityUpdater == null)
        {
            Logger.LogError("Could not get work priority updater component for current map.");
            return;
        }
        priorityUpdater.ForceUpdate();
    }

    /// <summary>
    ///     Determines if a pawn is enabled for work assignments.
    /// </summary>
    /// <param name="pawn">The pawn to check.</param>
    /// <returns>True if enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> is null.</exception>
    internal bool GetPawnEnabled([NotNull] Pawn pawn)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        _disabledPawns ??= [];
        return !_disabledPawns.Contains(pawn);
    }

    /// <summary>
    ///     Determines if a pawn's schedule is enabled.
    /// </summary>
    /// <param name="pawn">The pawn to check.</param>
    /// <returns>True if enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> is null.</exception>
    internal bool GetPawnScheduleEnabled([NotNull] Pawn pawn)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        _disabledPawnSchedules ??= [];
        return !_disabledPawnSchedules.Contains(pawn);
    }

    /// <summary>
    ///     Determines if a pawn is enabled for a specific work type.
    /// </summary>
    /// <param name="pawn">The pawn to check.</param>
    /// <param name="workType">The work type to check.</param>
    /// <returns>True if enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> or <paramref name="workType" /> is null.</exception>
    internal bool GetPawnWorkTypeEnabled([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        _disabledPawnWorkTypes ??= [];
        return !_disabledPawnWorkTypes.Any(pwt => pwt.Key == pawn && pwt.Value == workType);
    }

    /// <summary>
    ///     Determines if a work type is enabled.
    /// </summary>
    /// <param name="workType">The work type to check.</param>
    /// <returns>True if enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    internal bool GetWorkTypeEnabled([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        _disabledWorkTypes ??= [];
        return !_disabledWorkTypes.Contains(workType);
    }

    /// <summary>
    ///     Called when a game is loaded. Initializes settings and updates caches.
    /// </summary>
    public override void LoadedGame()
    {
        base.LoadedGame();
        WorkManagerMod.Settings.Initialize();
        UpdateSettingsCache();
    }

    /// <summary>
    ///     Enables or disables a pawn for work assignments.
    /// </summary>
    /// <param name="pawn">The pawn to modify.</param>
    /// <param name="enabled">True to enable; false to disable.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> is null.</exception>
    internal void SetPawnEnabled([NotNull] Pawn pawn, bool enabled)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        _disabledPawns ??= [];
        if (enabled)
        {
            _ = _disabledPawns.RemoveAll(p => p == pawn);
        }
        else
        {
            if (!_disabledPawns.Contains(pawn)) _disabledPawns.Add(pawn);
        }
    }

    /// <summary>
    ///     Enables or disables a pawn's schedule.
    /// </summary>
    /// <param name="pawn">The pawn to modify.</param>
    /// <param name="enabled">True to enable; false to disable.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> is null.</exception>
    internal void SetPawnScheduleEnabled([NotNull] Pawn pawn, bool enabled)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        _disabledPawnSchedules ??= [];
        if (enabled)
        {
            _ = _disabledPawnSchedules.RemoveAll(p => p == pawn);
        }
        else
        {
            if (!_disabledPawnSchedules.Contains(pawn)) _disabledPawnSchedules.Add(pawn);
        }
    }

    /// <summary>
    ///     Enables or disables a pawn for a specific work type.
    /// </summary>
    /// <param name="pawn">The pawn to modify.</param>
    /// <param name="workType">The work type to modify.</param>
    /// <param name="enabled">True to enable; false to disable.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> or <paramref name="workType" /> is null.</exception>
    internal void SetPawnWorkTypeEnabled([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType, bool enabled)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        _disabledPawnWorkTypes ??= [];
        if (enabled)
        {
            _ = _disabledPawnWorkTypes.RemoveAll(pwt => pwt.Key == pawn && pwt.Value == workType);
        }
        else
        {
            if (!_disabledPawnWorkTypes.Any(pwt => pwt.Key == pawn && pwt.Value == workType))
                _disabledPawnWorkTypes.Add(pawn, workType);
        }
    }

    /// <summary>
    ///     Enables or disables a work type.
    /// </summary>
    /// <param name="workType">The work type to modify.</param>
    /// <param name="enabled">True to enable; false to disable.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workType" /> is null.</exception>
    internal void SetWorkTypeEnabled([NotNull] WorkTypeDef workType, bool enabled)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        _disabledWorkTypes ??= [];
        if (enabled)
        {
            _ = _disabledWorkTypes.RemoveAll(wt => wt == workType);
        }
        else
        {
            if (!_disabledWorkTypes.Contains(workType)) _disabledWorkTypes.Add(workType);
        }
    }

    /// <summary>
    ///     Called when a new game is started. Initializes settings and updates caches.
    /// </summary>
    public override void StartedNewGame()
    {
        base.StartedNewGame();
        WorkManagerMod.Settings.Initialize();
        UpdateSettingsCache();
    }

    /// <summary>
    ///     Updates the dictionary of work types that should be assigned to everyone, along with their priorities.
    /// </summary>
    /// <remarks>
    ///     This method clears the <see cref="_assignEveryoneWorkTypes" /> dictionary and repopulates it by iterating
    ///     through the combined work type assignment rules. For each rule where
    ///     <see cref="WorkTypeAssignmentRule.AssignEveryone" />
    ///     is <c>true</c>, the corresponding <see cref="WorkTypeDef" /> and
    ///     <see cref="WorkTypeAssignmentRule.AssignEveryonePriority" />
    ///     are added to the dictionary.
    /// </remarks>
    private void UpdateAssignEveryoneWorkTypes()
    {
        _assignEveryoneWorkTypes.Clear();
        foreach (var rule in CombinedRules)
        {
            if (rule.AssignEveryone == true)
                _assignEveryoneWorkTypes.Add(rule.Def, rule.AssignEveryonePriority);
        }
    }

    /// <summary>
    ///     Updates the combined work type assignment rules by merging default and special rules.
    /// </summary>
    /// <remarks>
    ///     This method clears the existing combined rules and rebuilds them by combining a default  work
    ///     type assignment rule with any applicable special rules. If no default rule is found,  a
    ///     <see
    ///         cref="InvalidOperationException" />
    ///     is thrown. The combined rules are stored in  a dictionary where each key is a
    ///     <see cref="WorkTypeDef" /> and the value is the resulting  <see cref="WorkTypeAssignmentRule" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if no default work type assignment rule is found in the settings.</exception>
    private void UpdateCombinedRules()
    {
        CombinedRules.Clear();
        var rules = WorkManagerMod.Settings.WorkTypeRules;
        WorkTypeAssignmentRule defaultRule = null;
        var rulesCount = rules.Count;
        for (var i = 0; i < rulesCount; i++)
        {
            if (rules[i].DefName != null) continue;
            defaultRule = rules[i];
            break;
        }
        if (defaultRule == null)
            throw new InvalidOperationException("Default work type assignment rule not found.");
        var specialRulesDict = new Dictionary<WorkTypeDef, WorkTypeAssignmentRule>(rulesCount);
        for (var i = 0; i < rulesCount; i++)
        {
            var rule = rules[i];
            if (rule.Def != null && rule.DefName != null)
                specialRulesDict[rule.Def] = rule;
        }
        foreach (var def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!specialRulesDict.TryGetValue(def, out var specialRule))
                specialRule = WorkTypeAssignmentRule.CreateRule(def.defName);
            var combinedRule = WorkTypeAssignmentRule.Combine(specialRule, defaultRule);
            CombinedRules.Add(combinedRule);
        }
#if DEBUG
        Logger.LogMessage(
            $"Combined rules = {string.Join(", ", CombinedRules.Select(r => $"{r.Label} [{r.DefName}]"))}");
#endif
    }

    /// <summary>
    ///     Updates the collection of dedicated work types based on the current rules.
    /// </summary>
    /// <remarks>
    ///     This method clears the existing collection of dedicated work types and repopulates it by
    ///     evaluating the rules in <see cref="CombinedRules" />. Only work types with rules that allow dedicated workers are
    ///     added to the collection.
    /// </remarks>
    private void UpdateDedicatedWorkTypes()
    {
        DedicatedWorkTypes.Clear();
        foreach (var rule in CombinedRules)
        {
            if (rule.DedicatedWorkerSettings.AllowDedicated == true) DedicatedWorkTypes.Add(rule.Def);
        }
    }

    /// <summary>
    ///     Updates the cached settings for work type assignment rules and assignments to everyone.
    /// </summary>
    internal void UpdateSettingsCache()
    {
#if DEBUG
        Logger.LogMessage("Updating settings cache...");
#endif
        UpdateCombinedRules();
        UpdateAssignEveryoneWorkTypes();
        UpdateDedicatedWorkTypes();
    }

    /// <summary>
    ///     Validates and cleans up the internal state by removing invalid or destroyed entries  from the disabled pawns,
    ///     pawn schedules, and work types collections.
    /// </summary>
    /// <remarks>
    ///     This method ensures that the internal collections only contain valid and active entries.  It
    ///     removes destroyed pawns from the disabled pawns and pawn schedules collections,  and removes work types that are
    ///     no longer defined in the database from the disabled work types collection.
    /// </remarks>
    private void Validate()
    {
        _ = _disabledPawns?.RemoveAll(pawn => pawn?.Destroyed ?? true);
        _ = _disabledPawnSchedules?.RemoveAll(pawn => pawn?.Destroyed ?? true);
        _ = _disabledWorkTypes?.RemoveAll(workType =>
            !DefDatabase<WorkTypeDef>.AllDefsListForReading.Contains(workType));
    }
}