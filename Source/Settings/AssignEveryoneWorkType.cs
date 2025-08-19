using JetBrains.Annotations;
using Verse;

namespace LordKuper.WorkManager;

public class AssignEveryoneWorkType : IExposable
{
    private bool _isInitialized;
    private WorkTypeDef _workTypeDef;
    public bool AllowDedicated;
    public int Priority;
    public string WorkTypeDefName;

    [UsedImplicitly]
    public AssignEveryoneWorkType() { }

    public AssignEveryoneWorkType(string workTypeDefName, int priority, bool allowDedicated)
    {
        WorkTypeDefName = workTypeDefName;
        Priority = priority;
        AllowDedicated = allowDedicated;
    }

    public bool IsWorkTypeLoaded
    {
        get
        {
            if (!_isInitialized) Initialize();
            return WorkTypeDef != null;
        }
    }

    public string Label
    {
        get
        {
            if (!_isInitialized) Initialize();
            return WorkTypeDef?.labelShort ?? WorkTypeDefName;
        }
    }

    public WorkTypeDef WorkTypeDef
    {
        get
        {
            if (!_isInitialized) Initialize();
            return _workTypeDef;
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref WorkTypeDefName, nameof(WorkTypeDefName));
        Scribe_Values.Look(ref Priority, nameof(Priority), 1);
        Scribe_Values.Look(ref AllowDedicated, nameof(AllowDedicated));
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _workTypeDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(WorkTypeDefName);
        _isInitialized = true;
    }
}