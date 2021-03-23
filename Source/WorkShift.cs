using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WorkManager
{
    internal enum WorkShiftName
    {
        Morning,
        Afternoon,
        Night
    }

    internal class WorkShift
    {
        private readonly HashSet<int> _leftoverHours;
        private readonly HashSet<int> _sleepHours;
        private readonly HashSet<int> _workHours;

        public WorkShift(WorkShiftName name, IEnumerable<int> workHours, IEnumerable<int> sleepHours)
        {
            Name = name;
            _workHours = new HashSet<int>(workHours);
            if (_workHours.Any(hour => hour < 0 || hour > 23))
            {
                throw new ArgumentException($"Invalid work hours provided for {name} shift", nameof(workHours));
            }
            _sleepHours = new HashSet<int>(sleepHours);
            if (_sleepHours.Any(hour => hour < 0 || hour > 23))
            {
                throw new ArgumentException($"Invalid sleep hours provided for {name} shift", nameof(sleepHours));
            }
            _leftoverHours = new HashSet<int>();
            for (var hour = 0; hour < 24; hour++)
            {
                if (!_workHours.Contains(hour) && !_sleepHours.Contains(hour)) { _leftoverHours.Add(hour); }
            }
        }

        public IEnumerable<int> LeftoverHours => _leftoverHours;

        public WorkShiftName Name { get; }

        public IEnumerable<int> SleepHours => _sleepHours;

        public HashSet<Pawn> Workers { get; } = new HashSet<Pawn>();

        public IEnumerable<int> WorkHours => _workHours;
    }
}