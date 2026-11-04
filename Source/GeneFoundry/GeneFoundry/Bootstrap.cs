using System.Reflection;
using Verse;

namespace GeneFoundry
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            var version = typeof(Bootstrap).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Log.Message($"[GeneFoundry] v{version} loaded successfully.");
        }
    }
}