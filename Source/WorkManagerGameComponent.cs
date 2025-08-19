using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace LordKuper.WorkManager;

[UsedImplicitly]
[method: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Game API")]
public class WorkManagerGameComponent(Game game) : GameComponent
{
    private List<Pawn> _disabledPawns = [];
    private List<Pawn> _disabledPawnSchedules = [];
    private Dictionary<Pawn, WorkTypeDef> _disabledPawnWorkTypes = [];
    private List<WorkTypeDef> _disabledWorkTypes = [];
    public bool PriorityManagementEnabled = true;
    public bool ScheduleManagementEnabled = true;
    public IReadOnlyList<Pawn> DisabledPawns => _disabledPawns;
    public IReadOnlyList<Pawn> DisabledPawnSchedules => _disabledPawnSchedules;
    public IReadOnlyDictionary<Pawn, WorkTypeDef> DisabledPawnWorkTypes => _disabledPawnWorkTypes;
    public IReadOnlyList<WorkTypeDef> DisabledWorkTypes => _disabledWorkTypes;

    public override void ExposeData()
    {
        base.ExposeData();
        _ = _disabledPawns?.RemoveAll(pawn => pawn?.Destroyed ?? true);
        _ = _disabledPawnSchedules?.RemoveAll(pawn => pawn?.Destroyed ?? true);
        _ = _disabledWorkTypes?.RemoveAll(workType =>
            !DefDatabase<WorkTypeDef>.AllDefsListForReading.Contains(workType));
        Scribe_Values.Look(ref PriorityManagementEnabled, nameof(PriorityManagementEnabled), true);
        Scribe_Values.Look(ref ScheduleManagementEnabled, nameof(ScheduleManagementEnabled), true);
        Scribe_Collections.Look(ref _disabledWorkTypes, nameof(DisabledWorkTypes), LookMode.Def);
        Scribe_Collections.Look(ref _disabledPawns, nameof(DisabledPawns), LookMode.Reference);
        Scribe_Collections.Look(ref _disabledPawnWorkTypes, nameof(DisabledPawnWorkTypes), LookMode.Deep);
        Scribe_Collections.Look(ref _disabledPawnSchedules, nameof(DisabledPawnSchedules), LookMode.Reference);
    }

    internal bool GetPawnEnabled([NotNull] Pawn pawn)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        _disabledPawns ??= [];
        return !_disabledPawns.Contains(pawn);
    }

    internal bool GetPawnScheduleEnabled([NotNull] Pawn pawn)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        _disabledPawnSchedules ??= [];
        return !_disabledPawnSchedules.Contains(pawn);
    }

    internal bool GetPawnWorkTypeEnabled([NotNull] Pawn pawn, [NotNull] WorkTypeDef workType)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        _disabledPawnWorkTypes ??= [];
        return !_disabledPawnWorkTypes.Any(pwt => pwt.Key == pawn && pwt.Value == workType);
    }

    internal bool GetWorkTypeEnabled([NotNull] WorkTypeDef workType)
    {
        if (workType == null) throw new ArgumentNullException(nameof(workType));
        _disabledWorkTypes ??= [];
        return !_disabledWorkTypes.Contains(workType);
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        WorkManagerMod.Settings.Initialize();
    }

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

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        WorkManagerMod.Settings.Initialize();
    }
}