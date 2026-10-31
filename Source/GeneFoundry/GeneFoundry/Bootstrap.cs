using Verse;

namespace GeneFoundry
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Log.Message("[GeneFoundry] Development build v0.2 loaded successfully.");
        }
    }
}