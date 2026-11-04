using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace GeneFoundry.Tests
{
    public class XmlDefinitionTests
    {
        private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "Fixtures");

        public static IEnumerable<object[]> DefinitionFiles() =>
            Directory.GetFiles(Path.Combine(Fixtures, "Defs"), "*.xml", SearchOption.AllDirectories)
                .Select(path => new object[] { Path.GetRelativePath(Fixtures, path) });

        private static IEnumerable<XElement> Definitions() => DefinitionFiles()
            .SelectMany(file => XDocument.Load(Path.Combine(Fixtures, (string)file[0])).Root.Elements());

        private static XElement Def(string name) =>
            Definitions().Single(def => (string)def.Element("defName") == name);

        [Theory]
        [MemberData(nameof(DefinitionFiles))]
        public void DefinitionXmlParsesAndHasNoSurroundingTextWhitespace(string relativePath)
        {
            XDocument document = XDocument.Load(Path.Combine(Fixtures, relativePath));
            Assert.Equal("Defs", document.Root.Name.LocalName);
            Assert.NotEmpty(document.Root.Elements());
            foreach (XElement element in document.Root.Descendants().Where(element => !element.HasElements))
                Assert.True(element.Value == element.Value.Trim(),
                    $"{relativePath}: surrounding whitespace in <{element.Name}>.");
        }

        [Fact]
        public void DefinitionNamesArePresentAndUniqueWithinTheirType()
        {
            var definitions = Definitions().ToList();
            Assert.NotEmpty(definitions);
            Assert.All(definitions, def => Assert.False(string.IsNullOrWhiteSpace((string)def.Element("defName"))));
            Assert.All(definitions.GroupBy(def => (def.Name, (string)def.Element("defName"))),
                group => Assert.Single(group));
        }

        [Fact]
        public void AllGeneFoundryDefinitionReferencesResolve()
        {
            var definitions = Definitions().ToList();
            var names = definitions.Select(def => (string)def.Element("defName")).ToHashSet();
            foreach (XElement element in definitions.SelectMany(def => def.Descendants()))
            {
                if (!element.HasElements && element.Name.LocalName != "defName" &&
                    element.Value.StartsWith("GF_", StringComparison.Ordinal))
                    Assert.True(names.Contains(element.Value), $"Unresolved definition reference: {element.Value}");
                if (element.Name.LocalName.StartsWith("GF_", StringComparison.Ordinal))
                    Assert.True(names.Contains(element.Name.LocalName), $"Unresolved product: {element.Name}");
            }
        }

        [Fact]
        public void RecipesAndProductsReferenceTheCorrectDefinitionTypes()
        {
            foreach (XElement building in Definitions().Where(def => def.Name == "ThingDef"))
                foreach (XElement recipe in building.Element("recipes")?.Elements() ?? Enumerable.Empty<XElement>())
                    Assert.Equal("RecipeDef", Def(recipe.Value).Name.LocalName);

            foreach (XElement recipe in Definitions().Where(def => def.Name == "RecipeDef"))
            {
                foreach (XElement product in recipe.Element("products")?.Elements() ?? Enumerable.Empty<XElement>())
                {
                    if (product.Name.LocalName.StartsWith("GF_", StringComparison.Ordinal))
                        Assert.Equal("ThingDef", Def(product.Name.LocalName).Name.LocalName);
                    Assert.True(int.Parse(product.Value, CultureInfo.InvariantCulture) > 0);
                }
                foreach (XElement count in recipe.Element("ingredients")?.Descendants("count") ?? Enumerable.Empty<XElement>())
                    Assert.True(float.Parse(count.Value, CultureInfo.InvariantCulture) > 0f);
            }
        }

        [Fact]
        public void ReferencedCustomClassesExistAndAreIncludedInProductionProject()
        {
            XDocument project = XDocument.Load(Path.Combine(Fixtures, "GeneFoundry.csproj"));
            XNamespace ns = project.Root.Name.Namespace;
            var types = new HashSet<string>();
            foreach (XElement compile in project.Descendants(ns + "Compile"))
            {
                string path = Path.Combine(Fixtures, "Source",
                    ((string)compile.Attribute("Include")).Replace('\\', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"Missing compile source: {path}");
                string source = File.ReadAllText(path);
                Match space = Regex.Match(source, @"namespace\s+([\w.]+)");
                foreach (Match type in Regex.Matches(source, @"\bclass\s+(\w+)"))
                    types.Add(space.Groups[1].Value + "." + type.Groups[1].Value);
            }
            var references = Definitions().SelectMany(def => def.DescendantsAndSelf())
                .SelectMany(element => element.Attributes().Select(attribute => attribute.Value)
                    .Concat(element.HasElements ? Enumerable.Empty<string>() : new[] { element.Value })
                    .Concat(new[] { element.Name.LocalName }))
                .Where(value => value.StartsWith("GeneFoundry.", StringComparison.Ordinal));
            Assert.NotEmpty(references);
            foreach (string reference in references)
                Assert.True(types.Contains(reference), $"Custom class missing from compiled sources: {reference}");
        }

        [Theory]
        [InlineData("GF_ExtractBiomassBasic", 20)]
        [InlineData("GF_ExtractBiomassEnzymatic", 25)]
        [InlineData("GF_ExtractBiomassPlant", 8)]
        [InlineData("GF_ExtractBiomassPlantEnzymatic", 10)]
        public void MaterialConversionsUseFullRecoveryQuantities(string name, int count)
        {
            var recipe = Def(name);
            Assert.Equal(count, (int)recipe.Element("products").Element("GF_RawBiomass"));
            Assert.Equal(50, (int)recipe.Element("ingredients").Element("li").Element("count"));
            if(name.EndsWith("Enzymatic"))
                Assert.Equal(1, (int)recipe.Element("ingredients").Elements().Last().Element("count"));
        }

        [Fact]
        public void RefineryConversionAndQualificationsRemainFixed()
        {
            var comp = Def("GF_BiomassRefinery").Element("comps").Elements().Single(e =>
                (string)e.Attribute("Class") == "GeneFoundry.Components.CompProperties_BiomassRefiner");
            Assert.Equal(40, (int)comp.Element("inputCount"));
            Assert.Equal(10, (int)comp.Element("outputCount"));
            var recipe = Def("GF_StartBiomassRefinement");
            Assert.Equal(10, (int)recipe.Element("products").Element("GF_RefinedBiomass"));
            Assert.Equal(1, (int)Assert.Single(recipe.Element("ingredients").Elements()).Element("count"));
            Assert.Equal("Neutroamine", (string)recipe.Element("ingredients").Descendants("thingDefs").Single().Element("li"));
            Assert.Equal(5, (int)recipe.Element("skillRequirements").Element("Intellectual"));
            Assert.Equal(3, (int)recipe.Element("skillRequirements").Element("Crafting"));
            Assert.Equal(2, (int)recipe.Element("skillRequirements").Element("Medicine"));
            Assert.True((float)Def("GF_BiomassExtractionYield").Element("minValue") < 1f);
        }

        [Fact]
        public void AllSixExtractionRecipesHaveHalvedWork()
        {
            foreach (var recipe in Def("GF_GeneVat").Element("recipes").Elements())
                Assert.Equal("500", (string)Def(recipe.Value).Element("workAmount"));
        }

        [Fact]
        public void GeneVatRegistersAllExtractionBillsAndHasOneDedicatedWorkgiver()
        {
            Assert.Equal(new[] { "GF_ExtractBiomassPlant", "GF_ExtractBiomassPlantEnzymatic",
                "GF_ExtractBiomassBasic", "GF_ExtractBiomassEnzymatic",
                "GF_ExtractBiomassCorpse", "GF_ExtractBiomassCorpseEnzymatic" },
                Def("GF_GeneVat").Element("recipes").Elements().Select(element => element.Value));
            var givers = Definitions().Where(def => def.Name == "WorkGiverDef" &&
                def.Element("fixedBillGiverDefs")?.Elements().Any(element => element.Value == "GF_GeneVat") == true);
            XElement giver = Assert.Single(givers);
            Assert.Equal("GeneFoundry.WorkGivers.WorkGiver_GeneVat", (string)giver.Element("giverClass"));
            Assert.Equal("Cooking", (string)giver.Element("workType"));
        }

        [Fact]
        public void CorpseRecipePreservesSafetyAndDecayConfiguration()
        {
            XElement recipe = Def("GF_ExtractBiomassCorpse");
            Assert.Equal("false", (string)recipe.Element("interruptIfIngredientIsRotting"));
            Assert.Equal("1", (string)recipe.Element("ingredients").Element("li").Element("count"));
            Assert.Single(recipe.Element("ingredients").Elements());
            Assert.DoesNotContain(recipe.Element("ingredients").Descendants(),
                element => element.Value == "Medicine" || element.Value.StartsWith("Medicine", StringComparison.Ordinal));
            Assert.Equal("CorpsesAnimal", (string)recipe.Element("defaultIngredientFilter").Element("categories").Element("li"));
            Assert.Contains("GF_AllowUnsafeBiomassCorpses",
                recipe.Element("fixedIngredientFilter").Element("specialFiltersToDisallow").Elements().Select(element => element.Value));
            // This filter must not silently exclude implanted bodies from unrelated vanilla bills.
            Assert.Equal("true", (string)Def("GF_AllowUnsafeBiomassCorpses").Element("allowedByDefault"));
            Assert.Contains("AllowRotten", recipe.Element("defaultIngredientFilter")
                .Element("specialFiltersToAllow").Elements().Select(element => element.Value));
        }

        [Fact]
        public void ExtractionBillsUseWeightedSpeedAndDisableVanillaSingleSkillXp()
        {
            foreach (string name in new[] { "GF_ExtractBiomassPlant", "GF_ExtractBiomassPlantEnzymatic",
                "GF_ExtractBiomassBasic", "GF_ExtractBiomassEnzymatic",
                "GF_ExtractBiomassCorpse", "GF_ExtractBiomassCorpseEnzymatic" })
            {
                XElement recipe = Def(name);
                Assert.Equal("GF_BiomassExtractionSpeed", (string)recipe.Element("workSpeedStat"));
                Assert.Equal("0", (string)recipe.Element("workSkillLearnFactor"));
            }
            XElement stat = Def("GF_BiomassExtractionSpeed");
            Assert.Equal("StatDef", stat.Name.LocalName);
            Assert.Equal("GeneFoundry.Recipes.StatPart_GeneVatSkills",
                (string)stat.Element("parts").Element("li").Attribute("Class"));
            Assert.Equal("GeneFoundry.Jobs.JobDriver_ExtractMaterialBiomass",
                (string)Def("GF_ExtractMaterialBiomass").Element("driverClass"));
        }

        [Fact]
        public void OnlyMaterialRecipesUseVanillaEfficiencyForCachedWastage()
        {
            foreach (string name in new[] { "GF_ExtractBiomassBasic", "GF_ExtractBiomassEnzymatic", "GF_ExtractBiomassPlant",
                "GF_ExtractBiomassPlantEnzymatic" })
                Assert.Equal("GF_BiomassExtractionYield", (string)Def(name).Element("efficiencyStat"));
            // Corpse output is generated by the custom job so salvage never receives the multiplier.
            Assert.Null(Def("GF_ExtractBiomassCorpse").Element("efficiencyStat"));
            Assert.Null(Def("GF_ExtractBiomassCorpseEnzymatic").Element("efficiencyStat"));
            Assert.Equal("GeneFoundry.Recipes.StatPart_GeneVatYield",
                (string)Def("GF_BiomassExtractionYield").Element("parts").Element("li").Attribute("Class"));
        }

        [Theory]
        [InlineData("GF_ExtractBiomassBasic", false)]
        [InlineData("GF_ExtractBiomassPlant", false)]
        [InlineData("GF_ExtractBiomassEnzymatic", true)]
        [InlineData("GF_ExtractBiomassCorpse", true)]
        [InlineData("GF_ExtractBiomassPlantEnzymatic", true)]
        [InlineData("GF_ExtractBiomassCorpseEnzymatic", true)]
        public void ExtractionRequirementsReflectProcessComplexity(string recipeName, bool advanced)
        {
            XElement skills = Def(recipeName).Element("skillRequirements");
            Assert.NotNull(skills);
            Assert.Equal("3", (string)skills.Element("Cooking"));
            Assert.Equal(advanced ? "3" : null, (string)skills.Element("Medicine"));
            Assert.Equal(advanced ? "2" : null, (string)skills.Element("Intellectual"));
            Assert.Equal(advanced ? 3 : 1, skills.Elements().Count());
        }

        [Theory]
        [InlineData("GF_ExtractBiomassCorpse")]
        [InlineData("GF_ExtractBiomassCorpseEnzymatic")]
        public void CorpseBillsExcludeMachinesAllowDecayAndAvoidFalseFixedYield(string name)
        {
            XElement recipe = Def(name);
            Assert.Equal("GeneFoundry.Recipes.RecipeDef_BiomassCorpse", (string)recipe.Attribute("Class"));
            Assert.Null(recipe.Element("products"));
            Assert.Contains("variable yield", (string)recipe.Element("label"));
            Assert.Equal("GeneFoundry.Recipes.RecipeWorkerCounter_CorpseBiomass",
                (string)recipe.Element("workerCounterClass"));
            Assert.Equal("GF_RawBiomass", (string)recipe.Element("uiIconThing"));
            Assert.Equal("false", (string)recipe.Element("interruptIfIngredientIsRotting"));
            foreach (XElement filter in new[] { recipe.Element("ingredients").Element("li").Element("filter"),
                recipe.Element("fixedIngredientFilter") })
            {
                Assert.Null(filter.Element("disallowedCategories"));
                Assert.Equal(new[] { "CorpsesHumanlike", "CorpsesAnimal" },
                    filter.Element("categories").Elements().Select(element => element.Value));
            }
            Assert.Contains("AllowRotten", recipe.Element("defaultIngredientFilter")
                .Element("specialFiltersToAllow").Elements().Select(element => element.Value));
        }

        [Fact]
        public void EnzymaticPlantsUseFiftyPlantsOneHerbalMedicineAndTenBiomass()
        {
            XElement recipe = Def("GF_ExtractBiomassPlantEnzymatic");
            XElement[] ingredients = recipe.Element("ingredients").Elements().ToArray();
            Assert.Equal(2, ingredients.Length);
            Assert.Equal("50", (string)ingredients[0].Element("count"));
            Assert.Equal("PlantFoodRaw", (string)ingredients[0].Element("filter").Element("categories").Element("li"));
            Assert.Equal("1", (string)ingredients[1].Element("count"));
            Assert.Equal("MedicineHerbal", (string)ingredients[1].Element("filter").Element("thingDefs").Element("li"));
            Assert.Equal("10", (string)recipe.Element("products").Element("GF_RawBiomass"));
        }

        [Fact]
        public void EnzymaticCorpseFilterPermitsItsMandatoryHerbalCatalyst()
        {
            XElement recipe = Def("GF_ExtractBiomassCorpseEnzymatic");
            XElement catalyst = recipe.Element("ingredients").Elements().Last();
            Assert.Equal("MedicineHerbal", (string)catalyst.Element("filter").Element("thingDefs").Element("li"));
            Assert.Equal("1", (string)catalyst.Element("count"));
            Assert.Equal("MedicineHerbal", (string)recipe.Element("fixedIngredientFilter").Element("thingDefs").Element("li"));
            Assert.Equal("MedicineHerbal", (string)recipe.Element("defaultIngredientFilter").Element("thingDefs").Element("li"));
        }
    }
}
