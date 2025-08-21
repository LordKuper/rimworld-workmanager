namespace LordKuper.WorkManager;

/// <summary>
///     Specifies the mode used to determine the allocation of dedicated workers in a system.
/// </summary>
/// <remarks>
///     This enumeration defines various strategies for assigning workers, such as using a constant value,
///     basing the allocation on the number of work types, or considering the ratio or count of capable pawns.
/// </remarks>
internal enum DedicatedWorkerMode
{
    Constant,
    WorkTypeCount,
    CapablePawnRatio,
    PawnCount
}