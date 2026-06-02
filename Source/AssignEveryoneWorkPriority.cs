using Verse;

namespace WorkManager;

/// <summary>
///     Represents a legacy work type entry that is assigned to everyone with a specific priority.
/// </summary>
public class AssignEveryoneWorkType : IExposable
{
    /// <summary>
    ///     Indicates whether designated pawns are allowed for this work type.
    /// </summary>
    public bool AllowDesignated;

    /// <summary>
    ///     The priority assigned to this work type.
    /// </summary>
    public int Priority;

    /// <summary>
    ///     The name of the work type definition.
    /// </summary>
    public string? WorkTypeDefName;

    /// <summary>
    ///     Serializes and deserializes the work type entry data.
    /// </summary>
    public void ExposeData()
    {
        Scribe_Values.Look(ref WorkTypeDefName, nameof(WorkTypeDefName));
        Scribe_Values.Look(ref Priority, nameof(Priority), 1);
        Scribe_Values.Look(ref AllowDesignated, nameof(AllowDesignated));
    }
}