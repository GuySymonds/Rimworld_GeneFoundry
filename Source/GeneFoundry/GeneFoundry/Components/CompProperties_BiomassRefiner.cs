using Verse;

namespace GeneFoundry.Components
{
    public class CompProperties_BiomassRefiner : CompProperties
    {
        public ThingDef inputDef;
        public ThingDef outputDef;

        public int inputCount = 40;
        public int outputCount = 10;

        public int processingTicks = 30000;

        public float idlePower = 100f;
        public float activePower = 1500f;

        public CompProperties_BiomassRefiner()
        {
            compClass = typeof(CompBiomassRefiner);
        }
    }
}