using Verse;

namespace GeneFoundry
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Log.Message("[GeneFoundry] Assembly loaded successfully.");
        }
    }
}