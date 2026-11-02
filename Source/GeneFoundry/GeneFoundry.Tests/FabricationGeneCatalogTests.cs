#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GeneFoundry.Fabrication;
using GeneFoundry.Research;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class FabricationGeneCatalogTests
    {
        [Fact]
        public void CatalogIncludesAllCosmeticFamiliesAndHidesEveryUnresearchedGene()
        {
            Game previousGame = Current.Game;
            var previousDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ToList();
            try
            {
                var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                game.researchManager = new ResearchManager(); Current.Game = game;
                var foundation = new ResearchProjectDef { defName = "GF_GeneFabrication", baseCost = 100 };
                var archogenetics = new ResearchProjectDef { defName = "Archogenetics", baseCost = 100 };
                var category = new GeneCategoryDef { defName = "ModdedFunctional" };
                var categoryResearch = new ResearchProjectDef { defName = GeneFabricationResearch.ProjectName(category), baseCost = 100 };
                DefDatabase<ResearchProjectDef>.Clear();
                DefDatabase<ResearchProjectDef>.Add(new[] { foundation, archogenetics, categoryResearch });
                var progress = (Dictionary<ResearchProjectDef, float>)typeof(ResearchManager).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.researchManager);
                var cosmetics = new[] { "Cosmetic", "Cosmetic_Hair", "Cosmetic_Skin", "Cosmetic_Body", "Cosmetic_Modded" }
                    .Select(name => new GeneDef { defName = name + "Gene", label = name, canGenerateInGeneSet = false,
                        biostatCpx = 0, displayCategory = new GeneCategoryDef { defName = name } }).ToList();
                var functional = new GeneDef { defName = "Functional", label = "Functional", displayCategory = category };
                var archite = new GeneDef { defName = "CosmeticArchite", label = "Archite", biostatArc = 1, displayCategory = cosmetics[0].displayCategory };
                var genes = cosmetics.Concat(new[] { functional, archite, cosmetics[0], null }).ToList();
                Assert.Empty(FabricationGeneCatalog.Available(genes));
                progress[foundation] = 100;
                var visible = FabricationGeneCatalog.Available(genes);
                Assert.Equal(cosmetics.Count, visible.Count);
                Assert.All(cosmetics, gene => Assert.Contains(gene, visible));
                Assert.DoesNotContain(functional, visible); Assert.DoesNotContain(archite, visible);
                progress[categoryResearch] = 100;
                Assert.Contains(functional, FabricationGeneCatalog.Available(genes));
                Assert.DoesNotContain(archite, FabricationGeneCatalog.Available(genes));
                progress[archogenetics] = 100;
                Assert.Contains(archite, FabricationGeneCatalog.Available(genes));
            }
            finally
            {
                Current.Game = previousGame; DefDatabase<ResearchProjectDef>.Clear(); DefDatabase<ResearchProjectDef>.Add(previousDefs);
            }
        }
    }
}
#endif
