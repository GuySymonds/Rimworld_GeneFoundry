using GeneFoundry.Jobs;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;

namespace GeneFoundry.Components
{
    public class CompProperties_GeneVatRotStink : CompProperties
    {
        public CompProperties_GeneVatRotStink() => compClass = typeof(CompGeneVatRotStink);
    }

    public class CompGeneVatRotStink : ThingComp
    {
        public const int CheckInterval = 250;
        public const int IncidentDuration = 2500;
        // Ten small releases per incident; vanilla handles density limits and dispersal.
        public const int GasPerEmission = 50;
        private int extractionTicks;
        private int incidentEndTick;
        private int nextEmissionTick;

        internal bool ActiveAt(int tick) => tick < incidentEndTick;

        // Called exclusively by the corpse dissolution toil, for its current operator.
        public void NotifyCorpseExtraction(Pawn pawn, int delta)
        {
            if (!parent.Spawned || parent.Destroyed)
                return;
            int now = Find.TickManager.TicksGame;
            if (ActiveAt(now))
                return;
            extractionTicks += delta;
            if (extractionTicks < CheckInterval)
                return;
            float risk = BiomassCalculations.CorpseRotStinkRisk(
                pawn.skills.GetSkill(SkillDefOf.Cooking).Level,
                pawn.skills.GetSkill(SkillDefOf.Medicine).Level,
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Level);
            while (extractionTicks >= CheckInterval)
            {
                extractionTicks -= CheckInterval;
                if (risk > 0f && TryStartIncident(now, risk, Rand.Value))
                {
                    CompTick();
                    break;
                }
            }
        }

        internal bool TryStartIncident(int now, float risk, float roll)
        {
            if (ActiveAt(now) || risk <= 0f || roll >= risk)
                return false;
            incidentEndTick = now + IncidentDuration;
            nextEmissionTick = now;
            extractionTicks = 0;
            return true;
        }

        internal bool EmissionDue(int now)
        {
            if (!ActiveAt(now) || now < nextEmissionTick)
                return false;
            nextEmissionTick = now + CheckInterval;
            return true;
        }

        public override void CompTick() => CompTickInterval(1);

        public override void CompTickInterval(int delta)
        {
            if (parent.Destroyed || !parent.Spawned || !EmissionDue(Find.TickManager.TicksGame))
                return;
            GasUtility.AddGas(GeneVatIngredientPlacement.CorpseCell(parent), parent.Map,
                GasType.RotStink, GasPerEmission);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref extractionTicks, "gfRotStinkExtractionTicks", 0);
            Scribe_Values.Look(ref incidentEndTick, "gfRotStinkEndTick", 0);
            Scribe_Values.Look(ref nextEmissionTick, "gfRotStinkNextEmissionTick", 0);
        }

        public override string CompInspectStringExtra() => ActiveAt(Find.TickManager.TicksGame)
            ? "Chemical reaction unstable — releasing rot stink" : null;
    }
}
