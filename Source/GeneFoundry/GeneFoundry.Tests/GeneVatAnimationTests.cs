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
        public void FullCyclesHoldFramesForSixTicksWithoutRepeatedEndpoints()
        {
            int[] sequence = { 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2, 1 };
            for (int tick = 0; tick < 3 * 84; tick++)
                Assert.Equal(sequence[tick / 6 % 14], GeneVatAnimationTiming.FrameAt(tick));
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(-1));
        }

        [Fact]
        public void ElapsedGameTicksAloneDetermineTheFrame()
        {
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(0));
            Assert.Equal(0, GeneVatAnimationTiming.FrameAt(5));
            Assert.Equal(1, GeneVatAnimationTiming.FrameAt(6));
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
                string name = "vat_" + i.ToString("00") + ".png";
                byte[] png = File.ReadAllBytes(Path.Combine(fixtures, "Frames", name));
                Assert.Equal(File.ReadAllBytes(Path.Combine(fixtures, "SourceFrames", name)), png);
                Assert.Equal(1086, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4)));
                Assert.Equal(1448, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4)));
                Assert.Equal(6, png[25]);
            }
        }

        [Fact]
        public void RuntimeBubblesAreByteIdenticalAndHaveAlignedTransparentCanvases()
        {
            string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            Assert.Equal(8, Directory.GetFiles(Path.Combine(fixtures, "Bubbles"), "*.png").Length);
            for (int i = 0; i < 8; i++)
            {
                string name = i.ToString("00") + ".png";
                byte[] png = File.ReadAllBytes(Path.Combine(fixtures, "Bubbles", name));
                Assert.Equal(File.ReadAllBytes(Path.Combine(fixtures, "SourceBubbles", name)), png);
                // PNG IHDR uses big-endian dimensions; colour type 6 is RGBA.
                Assert.Equal(500, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4)));
                Assert.Equal(203, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4)));
                Assert.Equal(6, png[25]);
            }
        }

        [Fact]
        public void VatUsesOneRealtimeGraphicWithExistingDimensionsAndDirections()
        {
            var vat = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures/Defs/ThingDefs/GeneVat.xml"))
                .Descendants("ThingDef").Single(x => (string)x.Element("defName") == "GF_GeneVat");
            Assert.Equal(new[] { "GF_ExtractBiomassPlant", "GF_ExtractBiomassPlantEnzymatic",
                "GF_ExtractBiomassBasic", "GF_ExtractBiomassEnzymatic", "GF_ExtractBiomassCorpse",
                "GF_ExtractBiomassCorpseEnzymatic" }, vat.Element("recipes").Elements("li").Select(x => x.Value));
            Assert.Equal("Building_WorkTable", (string)vat.Element("thingClass"));
            Assert.Equal("RealtimeOnly", (string)vat.Element("drawerType"));
            Assert.Equal("BuildingOnTop", (string)vat.Element("altitudeLayer"));
            Assert.Equal("GeneFoundry.Recipes.Graphic_GeneVat", (string)vat.Element("graphicData").Element("graphicClass"));
            Assert.Equal("GeneFoundry/Buildings/GeneVat", (string)vat.Element("graphicData").Element("texPath"));
            Assert.Equal("(3,3.6)", (string)vat.Element("graphicData").Element("drawSize"));
            Assert.Equal("(3,3)", (string)vat.Element("size"));
            Assert.Contains(vat.Element("comps").Elements(), x =>
                (string)x.Attribute("Class") == "GeneFoundry.Components.CompProperties_GeneVatAnimation");
        }
    }
}
