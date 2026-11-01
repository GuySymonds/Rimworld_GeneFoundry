using RimWorld;
using Verse;

namespace GeneFoundry.Recipes
{
    public class RecipeWorkerCounter_CorpseBiomass : RecipeWorkerCounter
    {
        private static ThingDef Biomass => DefDatabase<ThingDef>.GetNamed("GF_RawBiomass");

        public override bool CanCountProducts(Bill_Production bill) => true;

        public override int CountProducts(Bill_Production bill)
        {
            if (bill.Map == null)
                return 0;
            int count = CountValidThings(bill.Map.listerThings.ThingsOfDef(Biomass), bill, Biomass);
            foreach (Pawn pawn in bill.Map.mapPawns.AllPawnsSpawned)
            {
                Thing carried = pawn.carryTracker?.CarriedThing;
                if (carried != null && carried.def == Biomass && CountValidThing(carried, bill, Biomass))
                    count += carried.stackCount;
            }
            return count;
        }

        public override string ProductsDescription(Bill_Production bill) =>
            "Raw biomass: variable yield from body material, decay and operator expertise (minimum 1).";

        public override bool CanPossiblyStore(Bill_Production bill, ISlotGroup group) =>
            group.Settings.filter.Allows(Biomass);
    }
}
