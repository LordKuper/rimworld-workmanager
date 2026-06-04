using System;
using System.Collections.Generic;
using System.Linq;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Tests for the <see cref="WorkShift" /> class covering hour-to-shift mapping and validation. (AC-2)
/// </summary>
[TestFixture]
public class WorkShiftTests
{
    /// <summary>
    ///     Tests that a valid WorkShift can be created with a full 24-hour schedule and threshold.
    /// </summary>
    [Test]
    public void Constructor_ValidSchedule_CreatesShift()
    {
        var schedule = new List<string>(24);
        for (var i = 0; i < 24; i++)
        {
            schedule.Add("Anything");
        }

        var shift = new WorkShift(schedule, 2);

        shift.PawnThreshold.Should().Be(2);
    }

    /// <summary>
    ///     Tests that a WorkShift constructor throws when given fewer than 24 hours.
    /// </summary>
    [Test]
    public void Constructor_TooFewHours_Throws()
    {
        var schedule = new List<string> { "Anything" };

        Action act = () => new WorkShift(schedule, 1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid schedule*");
    }

    /// <summary>
    ///     Tests that a WorkShift constructor throws when given more than 24 hours.
    /// </summary>
    [Test]
    public void Constructor_TooManyHours_Throws()
    {
        var schedule = Enumerable.Range(0, 25).Select(_ => "Anything").ToList();

        Action act = () => new WorkShift(schedule, 1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid schedule*");
    }

    /// <summary>
    ///     Tests that a WorkShift constructor throws when pawn threshold is less than 1.
    /// </summary>
    [Test]
    public void Constructor_InvalidThreshold_Throws()
    {
        var schedule = Enumerable.Range(0, 24).Select(_ => "Anything").ToList();

        Action act = () => new WorkShift(schedule, 0);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid pawn threshold*");
    }

    /// <summary>
    ///     Tests that GetTimeAssignment throws when hour is negative.
    ///     Note: Actually calling GetTimeAssignment requires RimWorld defs to be loaded.
    /// </summary>
    [Test]
    public void GetTimeAssignment_NegativeHour_Throws()
    {
        var schedule = Enumerable.Range(0, 24).Select(_ => "Anything").ToList();
        var shift = new WorkShift(schedule, 1);

        Action act = () => shift.GetTimeAssignment(-1);

        // Negative hour should throw ArgumentOutOfRangeException before any RimWorld def lookup
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///     Tests that GetTimeAssignment throws when hour is 24 or higher.
    ///     Note: Actually calling GetTimeAssignment requires RimWorld defs to be loaded.
    /// </summary>
    [Test]
    public void GetTimeAssignment_HourOutOfRange_Throws()
    {
        var schedule = Enumerable.Range(0, 24).Select(_ => "Anything").ToList();
        var shift = new WorkShift(schedule, 1);

        Action act = () => shift.GetTimeAssignment(24);

        // Hour 24 should throw ArgumentOutOfRangeException before any RimWorld def lookup
        act.Should().Throw<ArgumentOutOfRangeException>();
    }


    /// <summary>
    ///     Tests the default WorkShift constructor initializes pawn threshold to 1.
    /// </summary>
    [Test]
    public void DefaultConstructor_InitializesPawnThreshold()
    {
        var shift = new WorkShift();

        shift.PawnThreshold.Should().Be(1);
    }
}
