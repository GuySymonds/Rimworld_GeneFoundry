using Verse;

namespace GeneFoundry.PlaceWorkers
{
    public class PlaceWorker_BiomassHopper : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            Thing thingToIgnore = null,
            Thing thing = null)
        {
            // The hopper's outlet points to this adjacent cell.
            IntVec3 outletCell = loc + rot.FacingCell;

            if (!outletCell.InBounds(map))
            {
                return "The hopper outlet must face an adjacent biomass refinery.";
            }

            foreach (Thing candidate in map.thingGrid.ThingsListAt(outletCell))
            {
                if (candidate == thingToIgnore)
                    continue;

                if (candidate is Building &&
                    candidate.def.defName == "GF_BiomassRefinery")
                {
                    return true;
                }
            }

            return "The hopper outlet must face an adjacent biomass refinery.";
        }
    }
}