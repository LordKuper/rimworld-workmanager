using Verse;

namespace WorkManager
{
    public class AssignEveryoneWorkType : IExposable
    {
        public bool AllowDesignated;
        public int Priority;
        public string WorkTypeDefName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref WorkTypeDefName, nameof(WorkTypeDefName));
            Scribe_Values.Look(ref Priority, nameof(Priority), 1);
            Scribe_Values.Look(ref AllowDesignated, nameof(AllowDesignated));
        }
    }
}