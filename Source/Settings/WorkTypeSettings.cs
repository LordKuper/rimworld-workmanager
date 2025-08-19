using JetBrains.Annotations;
using Verse;

namespace LordKuper.WorkManager;

[UsedImplicitly]
public class WorkTypeSettings : IExposable
{
    private bool _isInitialized;
    private WorkTypeDef _workTypeDef;
    public bool AllowDedicatedWorkers = true;
    public bool AssignToIdlePawns = true;
    public DedicatedWorkerMode DedicatedWorkersMode = DedicatedWorkerMode.WorkTypeCount;
    public bool EnsureDedicatedWorker = true;
    public PawnCountSettings PawnCountSettings = new();
    public float WorkTypeCountFactor = 1f;
    public string WorkTypeDefName;
    public WorkTypeSettings() { }

    public WorkTypeSettings(string workTypeDefName)
    {
        WorkTypeDefName = workTypeDefName;
    }

    public WorkTypeDef WorkTypeDef
    {
        get
        {
            if (!_isInitialized) Initialize();
            return _workTypeDef;
        }
    }

    public string WorkTypeName
    {
        get
        {
            if (WorkTypeDefName == null) return Resources.Strings.Settings.WorkTypes.DefaultWorkTypeRuleLabel;
            if (!_isInitialized) Initialize();
            return WorkTypeDef == null ? WorkTypeDefName : WorkTypeDef.labelShort;
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref WorkTypeDefName, nameof(WorkTypeDefName));
        Scribe_Values.Look(ref AssignToIdlePawns, nameof(AssignToIdlePawns), true);
        Scribe_Values.Look(ref AllowDedicatedWorkers, nameof(AllowDedicatedWorkers), true);
        Scribe_Values.Look(ref EnsureDedicatedWorker, nameof(EnsureDedicatedWorker), true);
        Scribe_Values.Look(ref DedicatedWorkersMode, nameof(DedicatedWorkersMode));
        Scribe_Values.Look(ref WorkTypeCountFactor, nameof(WorkTypeCountFactor), 1f);
        Scribe_Deep.Look(ref PawnCountSettings, nameof(PawnCountSettings));
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _workTypeDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(WorkTypeDefName);
        _isInitialized = true;
    }
}