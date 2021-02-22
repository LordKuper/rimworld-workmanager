using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace WorkManager
{
    internal class PawnCache
    {
        public PawnCache(Pawn pawn)
        {
            Pawn = pawn;
        }

        public HashSet<WorkTypeDef> BadWorkTypes { get; } = new HashSet<WorkTypeDef>();
        public HashSet<WorkTypeDef> DisabledWorkTypes { get; } = new HashSet<WorkTypeDef>();
        public DayTime IdleSince { get; set; }
        public bool IsCapable { get; set; }

        public bool IsManaged { get; set; }
        public bool IsRecovering { get; set; }

        public HashSet<WorkTypeDef> ManagedWorkTypes { get; } = new HashSet<WorkTypeDef>();
        public Pawn Pawn { get; }
        public Dictionary<SkillDef, float> SkillLearningRates { get; } = new Dictionary<SkillDef, float>();

        public Dictionary<WorkTypeDef, int> WorkPriorities { get; } = new Dictionary<WorkTypeDef, int>();
        public Dictionary<WorkTypeDef, float> WorkSkillLearningRates { get; } = new Dictionary<WorkTypeDef, float>();

        public Dictionary<WorkTypeDef, int> WorkSkillLevels { get; } = new Dictionary<WorkTypeDef, int>();

        public bool IsActiveWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return WorkPriorities[workType] > 0;
        }

        public bool IsBadWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return BadWorkTypes.Contains(workType);
        }

        public bool IsDisabledWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return DisabledWorkTypes.Contains(workType);
        }

        public bool IsHunter()
        {
            return !DisabledWorkTypes.Contains(WorkTypeDefOf.Hunting) && !IsBadWork(WorkTypeDefOf.Hunting) &&
                   (Settings.AllowMeleeHunters || !Pawn.story.traits.HasTrait(TraitDefOf.Brawler)) &&
                   (Settings.AllowMeleeHunters || Pawn.equipment.Primary != null) &&
                   !Pawn.equipment.Primary.def.IsMeleeWeapon;
        }

        public bool IsLearningRateAboveThreshold([NotNull] WorkTypeDef workType, bool majorThreshold)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            var threshold = majorThreshold ? Settings.MajorLearningRateThreshold : Settings.MinorLearningRateThreshold;
            var learnRate = WorkSkillLearningRates[workType];
            if (!Settings.UsePawnLearningRateThresholds) { return learnRate >= threshold; }
            var minLearningRate = SkillLearningRates.Values.Min();
            var maxLearningRate = SkillLearningRates.Values.Max();
            var learningRateRange = maxLearningRate - minLearningRate;
            if (learningRateRange < 0.01) { return false; }
            return learnRate >= minLearningRate + learningRateRange * threshold;
        }

        public bool IsManagedWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return ManagedWorkTypes.Contains(workType);
        }
    }
}