#if RIMWORLD_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Research;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    public class GameGeneResearchTests
    {
        private class HeadlessCategoryProject : GeneCategoryResearchProjectDef
        {
            // The vanilla PostLoad hook discovers installed DLC through the running game.
            // Keep registration and coordinate calculation real without launching that runtime.
            public override void PostLoad() { }
        }

        private readonly ResearchProjectDef foundation = new ResearchProjectDef { defName = "GF_GeneFabrication" };
        private readonly ResearchProjectDef archogenetics = new ResearchProjectDef { defName = "Archogenetics" };
        private readonly ResearchTabDef tab = new ResearchTabDef { defName = "GF_GeneFoundry" };

        private List<GeneCategoryResearchProjectDef> Build(params GeneDef[] genes) =>
            GeneFabricationResearch.CreateProjects(genes, foundation, tab, archogenetics, 500, 100, 200, 500);

        private static GeneDef Gene(string name, GeneCategoryDef category, int complexity = 0, int archite = 0) =>
            new GeneDef { defName = name, label = name, displayCategory = category,
                biostatCpx = complexity, biostatArc = archite };

        [Fact]
        public void MixedCategoryUnlocksNormalResearchWithoutBlockingItBehindArchogenetics()
        {
            var category = new GeneCategoryDef { defName = "ModdedCategory", label = "modded category" };
            GeneDef normal = Gene("Ordinary", category, 2);
            GeneDef archite = Gene("Archite", category, 3, 2);
            var project = Assert.Single(Build(normal, archite));
            Assert.Equal(675f, project.baseCost);
            Assert.Equal(new[] { foundation }, project.prerequisites);
            Assert.Same(tab, project.tab);
            Assert.Equal(new[] { archite, normal }, project.genes);
            Assert.Contains("Archogenetics", project.description);
            Assert.Contains("Ordinary", project.description);
        }

        [Fact]
        public void PureArchiteCategoryRequiresVanillaArchogeneticsToBeginResearch()
        {
            var category = new GeneCategoryDef { defName = "AnyCategory" };
            var project = Assert.Single(Build(Gene("A", category, 0, 1), Gene("B", category, 0, 2)));
            Assert.Equal(new[] { foundation, archogenetics }, project.prerequisites);
            Assert.Equal(550f, project.baseCost);
        }

        [Fact]
        public void NegativeStatsCannotCancelOtherGenesPositiveCosts()
        {
            var category = new GeneCategoryDef { defName = "Modded" };
            var project = Assert.Single(Build(Gene("A", category, -10, -5), Gene("B", category, 2, 1)));
            Assert.Equal(400f, project.baseCost);
        }

        [Fact]
        public void UncategorizedAndNonRandomGenesAreIncludedWithoutHardcodedLists()
        {
            GeneDef gene = Gene("Special", null, 1);
            gene.canGenerateInGeneSet = false;
            var project = Assert.Single(Build(gene, gene));
            Assert.Single(project.genes);
            Assert.Equal("GF_GeneUncategorized", project.defName);
            Assert.Equal(200f, project.baseCost);
            Assert.Empty(Build());
        }

        [Fact]
        public void ProjectNamesAndLayoutAreStableRegardlessOfGeneLoadOrder()
        {
            GeneDef[] genes = Enumerable.Range(0, 15).Select(index =>
                Gene("Gene" + index, new GeneCategoryDef { defName = "Category" + index })).ToArray();
            var first = Build(genes);
            var reversed = Build(genes.Reverse().ToArray());
            Assert.Equal(first.Select(project => project.defName), reversed.Select(project => project.defName));
            Assert.Equal(15, first.Select(project => (project.researchViewX, project.researchViewY)).Distinct().Count());
            Assert.All(first, project =>
            {
                Assert.True(project.researchViewX >= 6);
                Assert.InRange(project.researchViewY, 0f, 5f);
            });
        }

        [Fact]
        public void GeneratedProjectsUseConfiguredScaleAndCapInCostAndDescription()
        {
            var category = new GeneCategoryDef { defName = "Large" };
            var project = Assert.Single(GeneFabricationResearch.CreateProjects(
                new[] { Gene("Complex", category, 100, 20) }, foundation, tab, archogenetics,
                500, 100, 200, 500, 50, 2500));
            Assert.Equal(2500f, project.baseCost);
            Assert.Contains("× 50%", project.description);
            Assert.Contains("capped at 2500 points = 2500", project.description);
            Assert.Contains(archogenetics, project.prerequisites);
        }

        [Fact]
        public void RegistrationUsesResearchDatabaseAndInitializesActualUiCoordinatesWithoutDuplicates()
        {
            var previous = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ToList();
            try
            {
                DefDatabase<ResearchProjectDef>.Clear();
                GeneDef[] genes = Enumerable.Range(0, 8).Select(index =>
                    Gene("Gene" + index, new GeneCategoryDef { defName = "LayoutCategory" + index })).ToArray();
                var projects = Build(genes).Select(project => new HeadlessCategoryProject
                {
                    defName = project.defName,
                    tab = project.tab,
                    researchViewX = project.researchViewX,
                    researchViewY = project.researchViewY
                }).ToList();
                GeneFabricationResearch.RegisterProjects(projects);
                GeneFabricationResearch.RegisterProjects(Build(genes));
                Assert.Equal(8, DefDatabase<ResearchProjectDef>.DefCount);
                foreach (var project in projects)
                {
                    Assert.Same(project, DefDatabase<ResearchProjectDef>.GetNamed(project.defName));
                    Assert.True(project.generated);
                    Assert.Equal(project.researchViewX, project.ResearchViewX);
                    Assert.Equal(project.researchViewY, project.ResearchViewY);
                }
                Assert.Equal(8, projects.Select(project => (project.ResearchViewX, project.ResearchViewY)).Distinct().Count());
            }
            finally
            {
                DefDatabase<ResearchProjectDef>.Clear();
                DefDatabase<ResearchProjectDef>.Add(previous);
            }
        }
    }
}
#endif
