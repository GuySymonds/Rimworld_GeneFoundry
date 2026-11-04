using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace GeneFoundry.Tests
{
    public class BiomassStorageTests
    {
        private static XElement Definition(string name) => XDocument.Load(Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "Defs", "ThingDefs", name + ".xml")).Root.Element("ThingDef");

        [Fact]
        public void RawBiomassUsesVanillaRotAndIsDestroyedWhenSpoiled()
        {
            Assert.Equal("Rare", Definition("RawBiomass").Element("tickerType").Value);
            XElement rot = Assert.Single(Definition("RawBiomass").Element("comps").Elements(),
                c => (string)c.Attribute("Class") == "GeneFoundry.Components.CompProperties_RawBiomassRottable");
            Assert.Equal("6", rot.Element("daysToRotStart").Value);
            Assert.Equal("true", rot.Element("rotDestroys").Value);
        }

        [Fact]
        public void HopperProtectsStoredBiomassFromEnvironmentalDeterioration()
        {
            Assert.Equal("true", Definition("RawBiomassHopper").Element("building")
                .Element("preventDeteriorationOnTop").Value);
        }

        [Fact]
        public void RefinedBiomassIsShelfStableButStillDamageable()
        {
            XElement refined = Definition("RefinedBiomass");
            Assert.DoesNotContain(refined.Descendants(), c => (string)c.Attribute("Class") == "CompProperties_Rottable");
            Assert.Equal("0", refined.Element("statBases").Element("DeteriorationRate").Value);
            Assert.Equal("true", refined.Element("useHitPoints").Value);
        }
    }
}
