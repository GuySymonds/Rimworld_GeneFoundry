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
                    .Concat(element.HasElements ? Enumerable.Empty<string>() : new[] { element.Value }))
                .Where(value => value.StartsWith("GeneFoundry.", StringComparison.Ordinal));
            Assert.NotEmpty(references);
            foreach (string reference in references)
                Assert.True(types.Contains(reference), $"Custom class missing from compiled sources: {reference}");
        }

        [Fact]
        public void GeneVatRegistersAllExtractionBillsAndHasOneDedicatedWorkgiver()
        {
            Assert.Equal(new[] { "GF_ExtractBiomassBasic", "GF_ExtractBiomassEnzymatic",
                "GF_ExtractBiomassPlant", "GF_ExtractBiomassCorpse" },
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
    }
}
