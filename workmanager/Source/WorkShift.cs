using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace WorkManager
{
    public class WorkShift : IExposable
    {
        private List<string> _hours;

        public int PawnThreshold = 1;

        public WorkShift()
        {
            _hours = new List<string>(24);
            for (var i = 0; i < 24; i++) { _hours.Add("Anything"); }
        }

        public WorkShift(IEnumerable<string> assignments, int pawnThreshold)
        {
            _hours = new List<string>(assignments);
            if (_hours.Count != 24)
            {
                throw new ArgumentException("Invalid schedule for creating work shift", nameof(assignments));
            }
            if (pawnThreshold < 1)
            {
                throw new ArgumentException("Invalid pawn threshold for creating work shift", nameof(pawnThreshold));
            }
            PawnThreshold = pawnThreshold;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref _hours, nameof(_hours), LookMode.Value);
            Scribe_Values.Look(ref PawnThreshold, nameof(PawnThreshold), 1);
        }

        public TimeAssignmentDef GetTimeAssignment(int hour)
        {
            return hour < 0 || hour >= 24
                ? throw new ArgumentOutOfRangeException(nameof(hour))
                : DefDatabase<TimeAssignmentDef>.GetNamedSilentFail(_hours[hour]) ??
                  DefDatabase<TimeAssignmentDef>.GetNamed("Anything");
        }

        public void SetTimeAssignment(int hour, [NotNull] TimeAssignmentDef assignment)
        {
            if (hour < 0 || hour >= 24) { return; }
            _hours[hour] = assignment.defName ?? throw new ArgumentNullException(nameof(assignment));
        }
    }
}