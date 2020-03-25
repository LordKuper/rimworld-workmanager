using Verse;

namespace WorkManager
{
    internal class WorkPriority
    {
        internal WorkPriority(WorkTypeDef workType, int priority)
        {
            WorkType = workType;
            Priority = priority;
        }

        public int Priority { get; set; }
        public WorkTypeDef WorkType { get; set; }
    }
}