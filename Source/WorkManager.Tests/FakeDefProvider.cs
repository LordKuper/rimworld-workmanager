using System;
using System.Collections.Generic;
using System.Linq;
using LordKuper.Common;
using Verse;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     A dictionary-backed <see cref="IDefProvider" /> that resolves only the defs registered on it, so tests can
///     resolve <see cref="Def" /> references without the game's def database.
/// </summary>
/// <remarks>
///     This type's generic constraints reference <see cref="Def" />, so loading it requires Assembly-CSharp. The test
///     project copies Assembly-CSharp next to the test assembly so that test discovery can load it.
/// </remarks>
internal sealed class FakeDefProvider : IDefProvider
{
    private readonly List<Def> _defs = [];

    /// <inheritdoc />
    public IEnumerable<T> AllDefs<T>() where T : Def
    {
        return AllDefsListForReading<T>();
    }

    /// <inheritdoc />
    public IReadOnlyList<T> AllDefsListForReading<T>() where T : Def
    {
        return _defs.OfType<T>().ToList();
    }

    /// <inheritdoc />
    public T? GetNamedSilentFail<T>(string? defName) where T : Def
    {
        if (string.IsNullOrEmpty(defName)) return null;
        return _defs.OfType<T>().FirstOrDefault(def => string.Equals(def.defName, defName, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkTypeDef> WorkTypeDefsInPriorityOrder()
    {
        return [];
    }

    /// <summary>
    ///     Makes the specified def resolvable through this provider.
    /// </summary>
    /// <param name="def">The def to register.</param>
    public void Register(Def def)
    {
        _defs.Add(def);
    }
}
