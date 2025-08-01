using System.Collections.Generic;
using Verse;

namespace LordKuper.WorkManager.Settings
{
    public class WorkTypePawnSettings : IExposable
    {
        private List<PawnCapacityLimit> _capacityLimits = new List<PawnCapacityLimit>();
        private Dictionary<string, bool> _passions = new Dictionary<string, bool>();
        private Dictionary<string, bool> _traits = new Dictionary<string, bool>();
        private Dictionary<string, bool> _workCapacities = new Dictionary<string, bool>();
        public bool AllowColonists = true;
        public bool AllowForeigners = true;
        public bool AllowInjured;
        public bool AllowNeedingRest;
        public bool AllowSlaves = true;
        public bool AllowUncontrollable;
        public bool AssignEveryone;
        public int AssignEveryonePriority;

        public List<PawnCapacityLimit> CapacityLimits
        {
            get => _capacityLimits;
            set => _capacityLimits = value;
        }

        public Dictionary<string, bool> Passions
        {
            get => _passions;
            set => _passions = value;
        }

        public Dictionary<string, bool> Traits
        {
            get => _traits;
            set => _traits = value;
        }

        public Dictionary<string, bool> WorkCapacities
        {
            get => _workCapacities;
            set => _workCapacities = value;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref AllowColonists, nameof(AllowColonists), true);
            Scribe_Values.Look(ref AllowForeigners, nameof(AllowForeigners), true);
            Scribe_Values.Look(ref AllowSlaves, nameof(AllowSlaves), true);
            Scribe_Values.Look(ref AllowInjured, nameof(AllowInjured));
            Scribe_Values.Look(ref AllowNeedingRest, nameof(AllowNeedingRest));
            Scribe_Values.Look(ref AllowUncontrollable, nameof(AllowUncontrollable));
            Scribe_Values.Look(ref AssignEveryone, nameof(AssignEveryone));
            Scribe_Values.Look(ref AssignEveryonePriority, nameof(AssignEveryonePriority));
            Scribe_Collections.Look(ref _traits, nameof(Traits), LookMode.Value);
            Scribe_Collections.Look(ref _passions, nameof(Passions), LookMode.Value);
            Scribe_Collections.Look(ref _workCapacities, nameof(WorkCapacities), LookMode.Value);
            Scribe_Collections.Look(ref _capacityLimits, nameof(CapacityLimits), LookMode.Deep);
        }
    }
}