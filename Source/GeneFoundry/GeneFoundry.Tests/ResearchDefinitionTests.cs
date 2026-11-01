using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace GeneFoundry.Tests
{
    public class ResearchDefinitionTests
    {
        private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "Fixtures");

        private static List<XElement> Definitions() => Directory
            .GetFiles(Path.Combine(Fixtures, "Defs"), "*.xml", SearchOption.AllDirectories)
            .SelectMany(path => XDocument.Load(path).Root.Elements()).ToList();

        [Fact]
        public void FourFoundationProjectsUseTheirOwnVisibleResearchTabAndDistinctPositions()
        {
            var defs = Definitions();
            XElement tab = Assert.Single(defs, def => def.Name == "ResearchTabDef");
            Assert.Equal("GF_GeneFoundry", (string)tab.Element("defName"));
            Assert.Equal("GeneFoundry", (string)tab.Element("label"));
            Assert.Equal("true", (string)tab.Element("visibleByDefault"));
            var projects = defs.Where(def => def.Name == "ResearchProjectDef").ToList();
            Assert.Equal(4, projects.Count);
            Assert.All(projects, project =>
            {
                Assert.Equal("GF_GeneFoundry", (string)project.Element("tab"));
                Assert.Equal("Industrial", (string)project.Element("techLevel"));
                Assert.True(int.Parse(project.Element("baseCost").Value, CultureInfo.InvariantCulture) > 0);
                Assert.True(float.Parse(project.Element("researchViewX").Value, CultureInfo.InvariantCulture) >= 0);
                Assert.True(float.Parse(project.Element("researchViewY").Value, CultureInfo.InvariantCulture) >= 0);
            });
            Assert.Equal(4, projects.Select(project => (
                (string)project.Element("researchViewX"), (string)project.Element("researchViewY"))).Distinct().Count());
        }

        [Theory]
        [InlineData("GF_BiomassExtraction", "Xenogermination", 500)]
        [InlineData("GF_EnzymaticExtraction", "GF_BiomassExtraction", 750)]
        [InlineData("GF_BiomassRefinement", "GF_BiomassExtraction", 1000)]
        [InlineData("GF_GeneFabrication", "GF_BiomassRefinement", 1500)]
        public void ResearchBranchesFromExtractionAndAlwaysRequiresVanillaXenogenetics(
            string name, string prerequisite, int cost)
        {
            var projects = Definitions().Where(def => def.Name == "ResearchProjectDef")
                .ToDictionary(def => def.Element("defName").Value);
            XElement project = projects[name];
            Assert.Equal(prerequisite, Assert.Single(project.Element("prerequisites").Elements()).Value);
            Assert.Equal(cost.ToString(CultureInfo.InvariantCulture), (string)project.Element("baseCost"));

            var visited = new HashSet<string>();
            string current = name;
            while (current != "Xenogermination")
            {
                Assert.True(visited.Add(current), "Research prerequisite cycle.");
                Assert.True(projects.ContainsKey(current), $"Unknown research prerequisite: {current}");
                current = Assert.Single(projects[current].Element("prerequisites").Elements()).Value;
            }
        }

        [Theory]
        [InlineData("GF_GeneVat", "GF_BiomassExtraction")]
        [InlineData("GF_BiomassRefinery", "GF_BiomassRefinement")]
        [InlineData("GF_BiomassHopper", "GF_BiomassRefinement")]
        public void ProductionBuildingsRequireTheirResearch(string name, string research)
        {
            XElement building = Definitions().Single(def => (string)def.Element("defName") == name);
            Assert.Equal("ThingDef", building.Name.LocalName);
            Assert.Equal(research, Assert.Single(building.Element("researchPrerequisites").Elements()).Value);
        }

        [Fact]
        public void EveryProductionRecipeHasTheCorrectResearchGateIncludingEnzymaticCorpses()
        {
            var recipes = Definitions().Where(def => def.Name == "RecipeDef").ToList();
            Assert.Equal(7, recipes.Count);
            foreach (XElement recipe in recipes)
            {
                string name = recipe.Element("defName").Value;
                string expected = name.Contains("Enzymatic") ? "GF_EnzymaticExtraction" :
                    name == "GF_StartBiomassRefinement" ? "GF_BiomassRefinement" : "GF_BiomassExtraction";
                Assert.Equal(expected, (string)recipe.Element("researchPrerequisite"));
                Assert.Equal("ResearchProjectDef", Definitions().Single(def =>
                    (string)def.Element("defName") == expected).Name.LocalName);
            }
        }

        [Fact]
        public void IntroductionTranslationsParseHaveUniqueKeysAndNoSurroundingWhitespace()
        {
            XElement language = XDocument.Load(Path.Combine(Fixtures,
                "Languages", "English", "Keyed", "GeneFoundryResearch.xml")).Root;
            Assert.Equal("LanguageData", language.Name.LocalName);
            Assert.Equal(new[] { "GF_IntroductionTitle", "GF_IntroductionText" },
                language.Elements().Select(element => element.Name.LocalName));
            Assert.All(language.Elements(), element =>
            {
                Assert.False(string.IsNullOrWhiteSpace(element.Value));
                Assert.Equal(element.Value.Trim(), element.Value);
            });
        }

        [Fact]
        public void CategoryGeneratorReferencesTheCorrectFoundationTabAndVanillaArchogenetics()
        {
            XElement generator = Assert.Single(Definitions(),
                def => def.Name == "GeneFoundry.Research.GeneResearchGeneratorDef");
            Assert.Equal("GF_GeneFabrication", (string)generator.Element("fabricationResearch"));
            Assert.Equal("GF_GeneFoundry", (string)generator.Element("researchTab"));
            Assert.Equal("Archogenetics", (string)generator.Element("architeResearch"));
        }
    }
}
