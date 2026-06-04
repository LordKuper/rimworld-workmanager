using System;
using System.IO;
using System.Reflection;

/// <summary>
///     Global setup fixture that registers the RimWorld AssemblyResolve handler before any RimWorld-typed test loads.
///     This fixture is in the global namespace to ensure it runs before any test type loads.
///     This is required because Assembly-CSharp and Unity modules are not NuGet packages but live in the local
///     RimWorld installation directory.
/// </summary>
[SetUpFixture]
public class RimWorldAssemblyResolverFixture
{
    /// <summary>
    ///     Gets the RimWorld Managed directory path from the assembly metadata attribute.
    /// </summary>
    /// <returns>The full path to the RimWorld Managed directory.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the RimWorldManagedDir attribute is not set in assembly metadata.
    /// </exception>
    private static string GetRimWorldManagedDir()
    {
        var attribute = typeof(RimWorldAssemblyResolverFixture).Assembly
            .GetCustomAttribute<AssemblyMetadataAttribute>();
        if (attribute is not null && attribute.Key == "RimWorldManagedDir")
            return attribute.Value ??
                   throw new InvalidOperationException(
                       "RimWorldManagedDir assembly metadata attribute is empty.");
        throw new InvalidOperationException(
            "RimWorldManagedDir assembly metadata attribute not found. " +
            "Verify that the test project's csproj defines RimWorldManagedDir correctly.");
    }

    /// <summary>
    ///     Registers the AssemblyResolve handler to load RimWorld and Unity assemblies from the RimWorld Managed directory.
    /// </summary>
    [OneTimeSetUp]
    public void RegisterAssemblyResolver()
    {
        var rimWorldManagedDir = GetRimWorldManagedDir();
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var assemblyName = args.Name.Split(',')[0];

            // Try to load from RimWorld's Managed directory
            var assemblyPath = Path.Combine(rimWorldManagedDir, assemblyName + ".dll");
            if (File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);
            return null;
        };
    }
}