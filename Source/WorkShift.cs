using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager;

/// <summary>
///     Represents a work shift: a 24-hour timetable assignment template with a pawn-count threshold.
/// </summary>
public class WorkShift : IExposable
{
    private List<string> _hours;

    /// <summary>
    ///     The minimum number of pawns required for this work shift to be used.
    /// </summary>
    public int PawnThreshold = 1;

    internal WorkShift()
    {
        _hours = new List<string>(24);
        for (var i = 0; i < 24; i++)
        {
            _hours.Add("Anything");
        }
    }

    internal WorkShift(IEnumerable<string> assignments, int pawnThreshold)
    {
        _hours = [.. assignments];
        if (_hours.Count != 24)
            throw new ArgumentException("Invalid schedule for creating work shift", nameof(assignments));
        if (pawnThreshold < 1)
            throw new ArgumentException("Invalid pawn threshold for creating work shift", nameof(pawnThreshold));
        PawnThreshold = pawnThreshold;
    }

    /// <summary>
    ///     Serializes and deserializes the work shift data.
    /// </summary>
    public void ExposeData()
    {
        Scribe_Collections.Look(ref _hours, nameof(_hours), LookMode.Value);
        Scribe_Values.Look(ref PawnThreshold, nameof(PawnThreshold), 1);
    }

    internal TimeAssignmentDef GetTimeAssignment(int hour)
    {
        return hour is < 0 or >= 24
            ? throw new ArgumentOutOfRangeException(nameof(hour))
            : DefDatabase<TimeAssignmentDef>.GetNamedSilentFail(_hours[hour]) ??
              DefDatabase<TimeAssignmentDef>.GetNamed("Anything");
    }

    internal void SetTimeAssignment(int hour, TimeAssignmentDef assignment)
    {
        if (hour is < 0 or >= 24) return;
        _hours[hour] = assignment.defName ?? throw new ArgumentNullException(nameof(assignment));
    }
}