using System;
using System.Collections.Generic;

namespace LordKuper.WorkManager.Helpers;

/// <summary>
///     Provides a comparison mechanism for <see cref="WorkTypeAssignmentRule" /> instances,
///     ordering them by relevant skill count, natural priority, and definition name.
/// </summary>
internal class WorkTypeAssignmentRuleComparer : IComparer<WorkTypeAssignmentRule>
{
    /// <summary>
    ///     Compares two <see cref="WorkTypeAssignmentRule" /> objects and returns a value indicating
    ///     whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <param name="x">The first <see cref="WorkTypeAssignmentRule" /> to compare.</param>
    /// <param name="y">The second <see cref="WorkTypeAssignmentRule" /> to compare.</param>
    /// <returns>
    ///     A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Less than zero: <paramref name="x" /> is less than <paramref name="y" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>Zero: <paramref name="x" /> equals <paramref name="y" />.</description>
    ///         </item>
    ///         <item>
    ///             <description>Greater than zero: <paramref name="x" /> is greater than <paramref name="y" />.</description>
    ///         </item>
    ///     </list>
    ///     Comparison is performed by:
    ///     <list type="number">
    ///         <item>
    ///             <description>Relevant skill count (descending).</description>
    ///         </item>
    ///         <item>
    ///             <description>Natural priority (descending).</description>
    ///         </item>
    ///         <item>
    ///             <description>Definition name (culture-sensitive string comparison).</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public int Compare(WorkTypeAssignmentRule x, WorkTypeAssignmentRule y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (y is null) return 1;
        if (x is null) return -1;
        var xDef = x.Def;
        var yDef = y.Def;
        if (xDef != null && yDef != null)
        {
            var skillCountComparison = (yDef.relevantSkills?.Count ?? 0).CompareTo(xDef.relevantSkills?.Count ?? 0);
            if (skillCountComparison != 0)
                return skillCountComparison;
            var priorityComparison = yDef.naturalPriority.CompareTo(xDef.naturalPriority);
            if (priorityComparison != 0)
                return priorityComparison;
        }
        return string.Compare(x.DefName, y.DefName, StringComparison.CurrentCulture);
    }
}