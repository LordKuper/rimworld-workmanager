using JetBrains.Annotations;
using Verse;

namespace LordKuper.WorkManager;

public class DisabledWorkType : IExposable
{
    private bool _isInitialized;
    private WorkTypeDef _workTypeDef;
    public string WorkTypeDefName;

    [UsedImplicitly]
    public DisabledWorkType() { }

    public DisabledWorkType(string workTypeDefName)
    {
        WorkTypeDefName = workTypeDefName;
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
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _workTypeDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(WorkTypeDefName);
        _isInitialized = true;
    }
}