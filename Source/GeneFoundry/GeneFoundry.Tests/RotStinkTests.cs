using System;
using System.IO;
using System.Xml.Linq;
using GeneFoundry.Recipes;
using Xunit;

namespace GeneFoundry.Tests
{
    public class RotStinkTests
    {
        [Fact]
        public void VatRegistersOnePersistentNormallyTickingHazardComponent()
        {
            var vat = XDocument.Load(Path.Combine(AppContext.BaseDirectory,
                "Fixtures/Defs/ThingDefs/GeneVat.xml")).Root.Element("ThingDef");
            Assert.Equal("Normal", (string)vat.Element("tickerType"));
            Assert.Single(vat.Element("comps").Elements("li"), element =>
                (string)element.Attribute("Class") == "GeneFoundry.Components.CompProperties_GeneVatRotStink");
        }

        [Theory]
        [InlineData(0,.15f)] [InlineData(3,.105f)] [InlineData(5,.075f)]
        [InlineData(9,.015f)] [InlineData(10,0f)] [InlineData(20,0f)]
        public void CookingRiskFallsLinearlyToTen(int cooking,float expected) =>
            Assert.Equal(expected,BiomassCalculations.CorpseRotStinkRisk(cooking),5);

        [Fact]
        public void OnlyDissolutionToilNotifiesHazardAndNoDecayIsPassed()
        {
            string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures/Source/Jobs/JobDriver_ExtractCorpseBiomass.cs"));
            int processing = source.IndexOf("Toil processing =");
            int notify = source.IndexOf("NotifyExtraction(pawn, delta)");
            Assert.True(notify > processing);
            Assert.True(notify < source.IndexOf("yield return processing;", processing));
            Assert.Equal(notify, source.LastIndexOf("NotifyExtraction(pawn, delta)"));
            string material = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures/Source/Jobs/JobDriver_ExtractMaterialBiomass.cs"));
            Assert.Contains("NotifyExtraction(pawn, delta)", material);
            Assert.True(material.IndexOf("NotifyExtraction") > material.IndexOf("toil.debugName == nameof(Toils_Recipe.DoRecipeWork)"));
        }
    }
}
