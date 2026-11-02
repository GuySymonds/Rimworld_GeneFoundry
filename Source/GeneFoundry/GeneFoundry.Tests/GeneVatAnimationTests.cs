using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GeneFoundry.Components;
using Xunit;

namespace GeneFoundry.Tests
{
    public class GeneVatAnimationTests
    {
        [Fact]
        public void FullCyclesHoldEachFrameForFifteenTicksWithoutRepeatedEndpoints()
        {
            int[] sequence = { 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2, 1 };
            for (int tick = 0; tick < 3 * 210; tick++)
                Assert.Equal(sequence[tick / 15 % 14], GeneVatAnimationTiming.FrameAt(tick));
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(-1));
        }

        [Fact]
        public void ElapsedGameTicksAloneDetermineTheFrame()
        {
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(0));
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(14));
            Assert.Equal(1, GeneVatAnimationTiming.FrameAt(15));
            // Different vat start ticks yield independent phases at the same game tick.
            Assert.Equal(6, GeneVatAnimationTiming.FrameAt(300 - 0));
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(300 - 300));
            Assert.Equal(6, GeneVatAnimationTiming.FrameAt(300 - 0));
        }

        [Fact]
        public void RuntimeFramesAreByteIdenticalToTheEightSourceImages()
        {
            string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            Assert.Equal(8, Directory.GetFiles(Path.Combine(fixtures, "Frames"), "*.png").Length);
            for (int i = 0; i < 8; i++)
            {
                string name = i.ToString("00") + ".png";
                Assert.Equal(File.ReadAllBytes(Path.Combine(fixtures, "SourceFrames", name)),
                    File.ReadAllBytes(Path.Combine(fixtures, "Frames", name)));
            }
        }

        [Fact]
        public void VatUsesOneRealtimeGraphicWithExistingDimensionsAndDirections()
        {
            var vat = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures/Defs/ThingDefs/GeneVat.xml"))
                .Descendants("ThingDef").Single(x => (string)x.Element("defName") == "GF_GeneVat");
            Assert.Equal("Building_WorkTable", (string)vat.Element("thingClass"));
            Assert.Equal("RealtimeOnly", (string)vat.Element("drawerType"));
            Assert.Equal("GeneFoundry.Recipes.Graphic_GeneVat", (string)vat.Element("graphicData").Element("graphicClass"));
            Assert.Equal("GeneFoundry/Buildings/GeneVat", (string)vat.Element("graphicData").Element("texPath"));
            Assert.Equal("(3,3.6)", (string)vat.Element("graphicData").Element("drawSize"));
            Assert.Equal("(3,3)", (string)vat.Element("size"));
            Assert.Contains(vat.Element("comps").Elements(), x =>
                (string)x.Attribute("Class") == "GeneFoundry.Components.CompProperties_GeneVatAnimation");
        }
    }
}
