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
        private readonly RimworldTime _updateTime = new RimworldTime(-1, -1, -1);
        private readonly Dictionary<WorkTypeDef, float> _workSkillLearningRates = new Dictionary<WorkTypeDef, float>();

        private readonly Dictionary<WorkTypeDef, int> _workSkillLevels = new Dictionary<WorkTypeDef, int>();

        public PawnCache(Pawn pawn)
        {
            Pawn = pawn;
        }

        private Dictionary<WorkTypeDef, bool> BadWorkTypes { get; } = new Dictionary<WorkTypeDef, bool>();
        private Dictionary<WorkTypeDef, bool> DisabledWorkTypes { get; } = new Dictionary<WorkTypeDef, bool>();

        public RimworldTime IdleSince { get; set; }
        public bool IsCapable { get; private set; }
        private bool IsForeigner { get; set; }
        public bool IsManaged { get; private set; }
        public bool IsRecovering { get; private set; }
        private bool IsSlave { get; set; }

        public Pawn Pawn { get; }
        private static WorkManagerGameComponent WorkManager => Current.Game.GetComponent<WorkManagerGameComponent>();
        public Dictionary<WorkTypeDef, int> WorkPriorities { get; } = new Dictionary<WorkTypeDef, int>();

        public Passion GetPassion([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            return !workType.relevantSkills.Any()
                ? Passion.None
                : workType.relevantSkills.Max(skill => Pawn.skills.GetSkill(skill)?.passion ?? Passion.None);
        }

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
                ? (int)Math.Floor(workType.relevantSkills.Select(skill => Pawn.skills.GetSkill(skill).Level).Average())
                : 0;
            _workSkillLevels.Add(workType, value);
            return value;
        }

        internal static int GetWorkTypePriority(Pawn pawn, WorkTypeDef workType)
        {
            return Settings.GetPriorityMethod == null
                ? pawn.workSettings.GetPriority(workType)
                : (int)Settings.GetPriorityMethod.Invoke(null, new object[] { pawn, workType, -1 });
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
            var value = (bool)Settings.IsBadWorkMethod.Invoke(null, new object[] { Pawn, workType });
            BadWorkTypes.Add(workType, value);
            return value;
        }

        public bool IsDisabledWork([NotNull] WorkTypeDef workType)
        {
            if (workType == null) { throw new ArgumentNullException(nameof(workType)); }
            if (DisabledWorkTypes.ContainsKey(workType)) { return DisabledWorkTypes[workType]; }
            var value = Pawn.WorkTypeIsDisabled(workType) ||
                        IsForeigner &&
                        Settings.DisabledWorkTypesForForeigners.Any(dwt => dwt.WorkTypeDef == workType) || IsSlave &&
                        Settings.DisabledWorkTypesForSlaves.Any(dwt => dwt.WorkTypeDef == workType);
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

        public void Update(RimworldTime time)
        {
            var hoursPassed = (time.Year - _updateTime.Year) * 60 * 24 + (time.Day - _updateTime.Day) * 24 + time.Hour -
                              _updateTime.Hour;
            _updateTime.Year = time.Year;
            _updateTime.Day = time.Day;
            _updateTime.Hour = time.Hour;
            IsCapable = !Pawn.Dead && !Pawn.Downed && !Pawn.InMentalState && !Pawn.InContainerEnclosed;
            IsRecovering = IsCapable && Settings.RecoveringPawnsUnfitForWork &&
                           HealthAIUtility.ShouldSeekMedicalRest(Pawn);
            IsManaged = WorkManager.GetPawnEnabled(Pawn);
            IsForeigner = Pawn.Faction != Faction.OfPlayer || Pawn.HasExtraMiniFaction() || Pawn.HasExtraHomeFaction();
            if (IsForeigner && Prefs.DevMode && Settings.VerboseLogging)
            {
                Log.Message(
                    $"----- Work Manager: {Pawn.LabelShort} is Foreigner {(Pawn.Faction != Faction.OfPlayer ? "(Non-player)" : "")}{(Pawn.HasExtraMiniFaction() ? "(Extra Mini)" : "")}{(Pawn.HasExtraHomeFaction() ? "(Extra Home)" : "")} -----");
            }
            IsSlave = ModsConfig.IdeologyActive && Pawn.IsSlaveOfColony;
            WorkPriorities.Clear();
            _managedWorkTypes.Clear();
            var workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w => w.visible);
            foreach (var workType in workTypes)
            {
                WorkPriorities.Add(workType, IsManagedWork(workType) ? 0 : GetWorkTypePriority(Pawn, workType));
            }
            if (!IsCapable) { return; }
            if (hoursPassed >= 24)
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"----- Work Manager: Updating work type cache for {(IsForeigner ? "[F]" : "")}{(IsSlave ? "[S]" : "")}{Pawn.LabelShort} (hours passed = {hoursPassed:N1})... -----");
                }
                DisabledWorkTypes.Clear();
                BadWorkTypes.Clear();
            }
            if (hoursPassed >= 6)
            {
                if (Prefs.DevMode && Settings.VerboseLogging)
                {
                    Log.Message(
                        $"----- Work Manager: Updating skill cache for {(IsForeigner ? "[F]" : "")}{(IsSlave ? "[S]" : "")}{Pawn.LabelShort} (hours passed = {hoursPassed:N1})... -----");
                }
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
        }
    }
}