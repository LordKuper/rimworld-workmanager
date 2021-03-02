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
        private readonly Dictionary<WorkTypeDef, bool> _managedWorkTypes = new Dictionary<WorkTypeDef, bool>();
        private readonly Dictionary<SkillDef, float> _skillLearningRates = new Dictionary<SkillDef, float>();
        private readonly DayTime _updateDayTime = new DayTime(-1, -1);
        private readonly Dictionary<WorkTypeDef, float> _workSkillLearningRates = new Dictionary<WorkTypeDef, float>();

        private readonly Dictionary<WorkTypeDef, int> _workSkillLevels = new Dictionary<WorkTypeDef, int>();

        public PawnCache(Pawn pawn)
        {
            Pawn = pawn;
        }

        private Dictionary<WorkTypeDef, bool> BadWorkTypes { get; } = new Dictionary<WorkTypeDef, bool>();
        private Dictionary<WorkTypeDef, bool> DisabledWorkTypes { get; } = new Dictionary<WorkTypeDef, bool>();

        public DayTime IdleSince { get; set; }
        public bool IsCapable { get; private set; }
        public bool IsManaged { get; private set; }
        public bool IsRecovering { get; private set; }

        public Pawn Pawn { get; }
        private static WorkManagerGameComponent WorkManager => Current.Game.GetComponent<WorkManagerGameComponent>();
        public Dictionary<WorkTypeDef, int> WorkPriorities { get; } = new Dictionary<WorkTypeDef, int>();

        private float GetSkillLearningRate([NotNull] SkillDef skill)
        {
            if (skill == null) { throw new ArgumentNullException(nameof(skill)); }
            if (_skillLearningRates.ContainsKey(skill)) { return _skillLearningRates[skill]; }
            var value = Pawn.skills.GetSkill(skill).LearnRateFactor();
            _skillLearningRates.Add(skill, value);
            return value;
        }

        private float GetWorkSkillLearningRate([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_workSkillLearningRates.ContainsKey(workType)) { return _workSkillLearningRates[workType]; }
            var value = workType.relevantSkills.Any()
                ? workType.relevantSkills.Select(GetSkillLearningRate).Average()
                : 0;
            _workSkillLearningRates.Add(workType, value);
            return value;
        }

        public int GetWorkSkillLevel([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_workSkillLevels.ContainsKey(workType)) { return _workSkillLevels[workType]; }
            var value = workType.relevantSkills.Any()
                ? (int) Math.Floor(workType.relevantSkills.Select(skill => Pawn.skills.GetSkill(skill).Level).Average())
                : 0;
            _workSkillLevels.Add(workType, value);
            return value;
        }

        public bool IsActiveWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return WorkPriorities[workType] > 0;
        }

        public bool IsBadWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (Settings.IsBadWorkMethod == null) { return false; }
            if (BadWorkTypes.ContainsKey(workType)) { return BadWorkTypes[workType]; }
            var value = (bool) Settings.IsBadWorkMethod.Invoke(null, new object[] {Pawn, workType});
            BadWorkTypes.Add(workType, value);
            return value;
        }

        public bool IsDisabledWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (DisabledWorkTypes.ContainsKey(workType)) { return DisabledWorkTypes[workType]; }
            var value = Pawn.WorkTypeIsDisabled(workType);
            DisabledWorkTypes.Add(workType, value);
            return value;
        }

        public bool IsHunter()
        {
            return !IsDisabledWork(WorkTypeDefOf.Hunting) && !IsBadWork(WorkTypeDefOf.Hunting) &&
                   (Settings.AllowMeleeHunters || !Pawn.story.traits.HasTrait(TraitDefOf.Brawler)) &&
                   (Settings.AllowMeleeHunters ||
                    Pawn.equipment.Primary != null && !Pawn.equipment.Primary.def.IsMeleeWeapon);
        }

        public bool IsLearningRateAboveThreshold([NotNull] WorkTypeDef workType, bool majorThreshold)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            var threshold = majorThreshold ? Settings.MajorLearningRateThreshold : Settings.MinorLearningRateThreshold;
            var learnRate = GetWorkSkillLearningRate(workType);
            if (!Settings.UsePawnLearningRateThresholds) { return learnRate >= threshold; }
            var minLearningRate = _skillLearningRates.Values.Min();
            var maxLearningRate = _skillLearningRates.Values.Max();
            var learningRateRange = maxLearningRate - minLearningRate;
            if (learningRateRange < 0.01) { return false; }
            return learnRate >= minLearningRate + learningRateRange * threshold;
        }

        public bool IsManagedWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (_managedWorkTypes.ContainsKey(workType)) { return _managedWorkTypes[workType]; }
            var value = IsManaged && WorkManager.GetWorkTypeEnabled(workType) &&
                        WorkManager.GetPawnWorkTypeEnabled(Pawn, workType);
            _managedWorkTypes.Add(workType, value);
            return value;
        }

        public void Update(DayTime dayTime)
        {
            var hoursPassed = (dayTime.Day - _updateDayTime.Day) * 24 + dayTime.Hour - _updateDayTime.Hour;
            IsCapable = !Pawn.Dead && !Pawn.Downed && !Pawn.InMentalState;
            IsRecovering = Settings.RecoveringPawnsUnfitForWork && HealthAIUtility.ShouldSeekMedicalRest(Pawn);
            IsManaged = WorkManager.GetPawnEnabled(Pawn);
            WorkPriorities.Clear();
            _managedWorkTypes.Clear();
            var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible);
            foreach (var workType in workTypes)
            {
                WorkPriorities.Add(workType, IsManagedWork(workType) ? 0 : Pawn.workSettings.GetPriority(workType));
            }
            if (hoursPassed >= 24)
            {
                DisabledWorkTypes.Clear();
                BadWorkTypes.Clear();
            }
            if (hoursPassed >= 1)
            {
                if (Settings.UsePawnLearningRateThresholds)
                {
                    _skillLearningRates.Clear();
                    foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
                    {
                        _skillLearningRates.Add(skill, Pawn.skills.GetSkill(skill).LearnRateFactor());
                    }
                }
                _workSkillLearningRates.Clear();
                _workSkillLevels.Clear();
            }
            _updateDayTime.Day = dayTime.Day;
            _updateDayTime.Hour = dayTime.Hour;
        }
    }
}