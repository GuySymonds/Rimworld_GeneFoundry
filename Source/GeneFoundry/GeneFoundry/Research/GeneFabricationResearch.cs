using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneFoundry.Research
{
    public class GeneCategoryResearchProjectDef : ResearchProjectDef
    {
        public List<GeneDef> genes;
        public bool automaticallyResearched;
    }

    public static class GeneFabricationResearch
    {
        public static string ProjectName(GeneCategoryDef category)
        {
            return category == null ? "GF_GeneUncategorized" : "GF_GeneCategory_" + category.defName;
        }

        // Called during definition resolution, before short hashes and research UI caches are built.
        public static List<GeneCategoryResearchProjectDef> CreateProjects(IEnumerable<GeneDef> genes,
            ResearchProjectDef fabrication, ResearchTabDef tab, ResearchProjectDef archogenetics,
            int baseCost, int perGene, int perComplexity, int perArchite,
            int costPercent = 25, int maximumCost = 4000, int minimumCost = 1000)
        {
            var result = new List<GeneCategoryResearchProjectDef>();
            var groups = genes.Where(gene => gene != null).Distinct().GroupBy(gene => gene.displayCategory)
                .OrderBy(group => ProjectName(group.Key), StringComparer.Ordinal);
            foreach (var group in groups)
            {
                var members = group.OrderBy(gene => gene.defName, StringComparer.Ordinal).ToList();
                long complexity = members.Sum(gene => (long)Math.Max(0, gene.biostatCpx));
                long archite = members.Sum(gene => (long)Math.Max(0, gene.biostatArc));
                int cost = GeneResearchCalculations.CategoryCost(members.Count, complexity, archite,
                    baseCost, perGene, perComplexity, perArchite, costPercent, maximumCost, minimumCost);
                string category = group.Key?.label ?? group.Key?.defName ?? "uncategorized";
                bool automatic = GeneResearchCalculations.IsCosmeticCategory(group.Key?.defName);
                var prerequisites = new List<ResearchProjectDef> { fabrication };
                if (!automatic && members.All(gene => gene.biostatArc > 0))
                    prerequisites.Add(archogenetics);
                string researchDescription = automatic
                    ? "Automatically researched at no additional research cost once Gene fabrication is complete.\n\n"
                    : $"Research cost: ({baseCost} + {members.Count} × {perGene} + {complexity} × {perComplexity} + {archite} × {perArchite}) × {costPercent}%, rounded up, minimum {minimumCost}, capped at {maximumCost} points = {cost}. " +
                        "Costs are configured in GeneFoundry's Gene research settings and apply after restarting RimWorld.\n\n";
                int index = result.Count;
                result.Add(new GeneCategoryResearchProjectDef
                {
                    defName = ProjectName(group.Key),
                    label = category,
                    description = $"Unlock fabrication of all {members.Count} genes in {category}. " +
                        $"Total complexity: {complexity}. Total archite value: {archite}.\n\n" +
                        researchDescription +
                        "Genes with an archite cost also require vanilla Archogenetics, even after this category is complete. " +
                        (automatic ? "" : "Categories containing only archite genes require Archogenetics before research can begin. ") +
                        "\n\nGenes:\n" +
                        string.Join("\n", members.Select(gene => gene.label ?? gene.defName)),
                    baseCost = cost,
                    techLevel = TechLevel.Industrial,
                    tab = tab,
                    // Vanilla enforces and lists these in project details without drawing edges.
                    prerequisites = new List<ResearchProjectDef>(),
                    hiddenPrerequisites = prerequisites,
                    researchViewX = 1.5f * (index % 4),
                    researchViewY = 4f + index / 4,
                    modContentPack = fabrication.modContentPack,
                    genes = members,
                    automaticallyResearched = automatic
                });
            }
            return result;
        }

        public static IEnumerable<GeneCategoryResearchProjectDef> AutomaticProjectsToComplete(
            ResearchProjectDef fabrication, IEnumerable<GeneCategoryResearchProjectDef> projects)
        {
            if (fabrication == null || !fabrication.IsFinished)
                yield break;
            foreach (GeneCategoryResearchProjectDef project in projects)
                if (project.automaticallyResearched && !project.IsFinished && project.PrerequisitesCompleted)
                    yield return project;
        }

        public static void RegisterProjects(IEnumerable<GeneCategoryResearchProjectDef> projects)
        {
            foreach (GeneCategoryResearchProjectDef project in projects)
            {
                if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(project.defName) != null)
                    continue;
                DefGenerator.AddImpliedDef<ResearchProjectDef>(project);
                project.ResolveReferences();
            }
            // Vanilla calculated its private x/y coordinates before definition resolution.
            // Rebuild through the public API so generated projects do not all appear at (1, 1).
            ResearchProjectDef.GenerateNonOverlappingCoordinates();
        }

        // Shared gate for the forthcoming fabricator's selection UI and production jobs.
        // Never modifies vanilla gene availability, extraction or gene assembly.
        public static bool IsUnlocked(GeneDef gene)
        {
            if (gene == null || Current.Game == null)
                return false;
            ResearchProjectDef fabrication = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("GF_GeneFabrication");
            ResearchProjectDef category = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ProjectName(gene.displayCategory));
            ResearchProjectDef archogenetics = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Archogenetics");
            return GeneResearchCalculations.IsUnlocked(fabrication?.IsFinished == true,
                category?.IsFinished == true, gene.biostatArc, archogenetics?.IsFinished == true);
        }
    }
}
