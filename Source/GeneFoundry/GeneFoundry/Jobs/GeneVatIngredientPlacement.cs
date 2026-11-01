using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Jobs
{
    public static class GeneVatIngredientPlacement
    {
        public static IntVec3 CorpseCell(Thing vat) => vat.Position - vat.Rotation.FacingCell;

        public static void RecordPlacement(Job job, Thing placed, int count)
        {
            HaulAIUtility.UpdateJobWithPlacedThings(job, placed, count);
        }

        public static void Configure(Toil toil, bool positionCorpse = false)
        {
            if (toil.debugName != nameof(Toils_Haul.PlaceHauledThingInCell))
                return;

            // Vanilla's placement callback is gated on particular vanilla JobDefs.
            // Custom jobs must supply the same public tracking callback themselves.
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Job job = actor.CurJob;
                Thing carried = actor.carryTracker.CarriedThing;
                if (carried == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                IntVec3 cell = positionCorpse && carried is Corpse ?
                    CorpseCell(job.targetA.Thing) : job.GetTarget(TargetIndex.C).Cell;
                if (!actor.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct,
                    out Thing dropped, (thing, count) => RecordPlacement(job, thing, count)))
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
            };
        }

        public static bool HasFullRecipeCost(Job job)
        {
            if (job.placedThings == null || job.bill?.recipe == null)
                return false;
            RecipeDef recipe = job.bill.recipe;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                double available = 0d;
                foreach (ThingCountClass entry in job.placedThings)
                {
                    Thing thing = entry.thing;
                    if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != job.bill.Map ||
                        !ingredient.filter.Allows(thing))
                        continue;
                    available += Math.Max(0, Math.Min(entry.Count, thing.stackCount)) *
                        recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
                }
                if (available + 0.00001d < recipe.Worker.GetIngredientCount(ingredient, job.bill))
                    return false;
            }
            return true;
        }
    }
}
