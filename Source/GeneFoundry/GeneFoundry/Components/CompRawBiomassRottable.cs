using RimWorld;
using Verse;

namespace GeneFoundry.Components
{
    public class CompProperties_RawBiomassRottable : CompProperties_Rottable
    {
        public CompProperties_RawBiomassRottable() { compClass = typeof(CompRawBiomassRottable); }
    }

    // Retain vanilla rot state, stacking and temperature rules outside the hopper.
    // Preservation suspends accumulation; it never refreshes an older stack.
    public class CompRawBiomassRottable : CompRottable
    {
        internal bool PreservedInHopper => parent.Spawned &&
            parent.Position.GetThingList(parent.Map).Exists(t =>
                t is Building_Storage && t.def.defName == "GF_BiomassHopper");

        public override void CompTickRare()
        {
            if (!PreservedInHopper) base.CompTickRare();
        }

        public override void CompTickInterval(int delta)
        {
            if (!PreservedInHopper) base.CompTickInterval(delta);
        }

        public override string CompInspectStringExtra() => PreservedInHopper
            ? "Refrigerated in biomass hopper (spoilage paused)."
            : base.CompInspectStringExtra();
    }
}
