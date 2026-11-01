using GeneFoundry.Research;
using Xunit;
#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
#endif

namespace GeneFoundry.Tests
{
    public class CosmeticResearchTests
    {
        [Theory]
        [InlineData("Cosmetic", true)]
        [InlineData("Cosmetic_Hair", true)]
        [InlineData("Cosmetic_Skin", true)]
        [InlineData("Cosmetic_Body", true)]
        [InlineData("Cosmetic_AlienFeatures", true)]
        [InlineData("CosmeticallyStrong", false)]
        [InlineData("Beauty", false)]
        [InlineData("Miscellaneous", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void CosmeticFamilyUsesStableDefinitionNames(string name, bool expected)
        {
            Assert.Equal(expected, GeneResearchCalculations.IsCosmeticCategory(name));
        }
    }

#if RIMWORLD_TESTS
    // Shares a collection with the other tests that temporarily replace the research database.
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class CosmeticResearchGameTests
    {
        [Fact]
        public void AutomaticCosmeticsRequireFoundationPreserveArchiteGateAndSkipCompletedProjects()
        {
            Game previousGame = Current.Game;
            var previousDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ToList();
            try
            {
                var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                game.researchManager = new ResearchManager();
                Current.Game = game;
                var foundation = new ResearchProjectDef { defName = "GF_GeneFabrication", baseCost = 100 };
                var archogenetics = new ResearchProjectDef { defName = "Archogenetics", baseCost = 100 };
                var tab = new ResearchTabDef { defName = "GF_GeneFoundry" };
                var progress = (Dictionary<ResearchProjectDef, float>)typeof(ResearchManager)
                    .GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(game.researchManager);
                var genes = new[] { "Cosmetic", "Cosmetic_Hair", "Cosmetic_Skin", "Cosmetic_Body", "Cosmetic_Modded" }
                    .Select(name => new GeneDef { defName = name + "Gene", label = name,
                        displayCategory = new GeneCategoryDef { defName = name, label = name } }).ToList();
                var architeCosmetic = new GeneDef { defName = "ArchiteCosmetic", biostatArc = 1,
                    displayCategory = new GeneCategoryDef { defName = "Cosmetic_Archite" } };
                var normal = new GeneDef { defName = "Functional", displayCategory =
                    new GeneCategoryDef { defName = "ModdedFunctional", label = "cosmetic" } };
                genes.Add(architeCosmetic);
                genes.Add(normal);
                var projects = GeneFabricationResearch.CreateProjects(genes, foundation, tab, archogenetics,
                    500, 100, 200, 500);
                DefDatabase<ResearchProjectDef>.Clear();
                DefDatabase<ResearchProjectDef>.Add(foundation);
                DefDatabase<ResearchProjectDef>.Add(archogenetics);
                DefDatabase<ResearchProjectDef>.Add(projects.Cast<ResearchProjectDef>());

                Assert.Empty(GeneFabricationResearch.AutomaticProjectsToComplete(foundation, projects));
                Assert.False(GeneFabricationResearch.IsUnlocked(genes[0]));
                progress[foundation] = 100; // Also represents a colony loaded with fabrication complete.
                var automatic = GeneFabricationResearch.AutomaticProjectsToComplete(foundation, projects).ToList();
                Assert.Empty(automatic); // Cosmetics have no generated projects.
                Assert.All(automatic, project =>
                {
                    Assert.Equal(new[] { foundation }, project.hiddenPrerequisites);
                    Assert.Contains("no additional research cost", project.description);
                    Assert.DoesNotContain("Research cost:", project.description);
                    progress[project] = project.baseCost;
                });
                Assert.Empty(GeneFabricationResearch.AutomaticProjectsToComplete(foundation, projects));
                Assert.All(genes.Where(gene => gene != normal && gene != architeCosmetic),
                    gene => Assert.True(GeneFabricationResearch.IsUnlocked(gene)));
                Assert.False(GeneFabricationResearch.IsUnlocked(normal));
                Assert.False(GeneFabricationResearch.IsUnlocked(architeCosmetic));
                Assert.False(archogenetics.IsFinished);
                progress[archogenetics] = 100;
                Assert.True(GeneFabricationResearch.IsUnlocked(architeCosmetic));
            }
            finally
            {
                Current.Game = previousGame;
                DefDatabase<ResearchProjectDef>.Clear();
                DefDatabase<ResearchProjectDef>.Add(previousDefs);
            }
        }
    }
#endif
}

