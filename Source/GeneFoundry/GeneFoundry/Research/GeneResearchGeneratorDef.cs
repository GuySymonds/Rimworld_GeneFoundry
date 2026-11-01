using RimWorld;
using Verse;

namespace GeneFoundry.Research
{
    // A separate Def avoids modifying the ResearchProjectDef list while its resolver iterates it.
    public class GeneResearchGeneratorDef : Def
    {
        public ResearchProjectDef fabricationResearch;
        public ResearchProjectDef architeResearch;
        public ResearchTabDef researchTab;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (fabricationResearch == null || architeResearch == null || researchTab == null)
            {
                Log.Error("[GeneFoundry] Cannot generate gene research: missing foundation, Archogenetics or research tab.");
                return;
            }
            GeneFoundrySettings settings = GeneFoundryMod.Settings;
            settings.Validate();
            fabricationResearch.baseCost = settings.geneFabricationResearchCost;
            var projects = GeneFabricationResearch.CreateProjects(DefDatabase<GeneDef>.AllDefsListForReading,
                fabricationResearch, researchTab, architeResearch, settings.geneCategoryBaseCost,
                settings.geneResearchPerGene, settings.geneResearchPerComplexity, settings.geneResearchPerArchite,
                settings.geneCategoryCostPercent, settings.geneCategoryMaximumCost, settings.geneCategoryMinimumCost);
            GeneFabricationResearch.RegisterProjects(projects);
        }
    }
}
