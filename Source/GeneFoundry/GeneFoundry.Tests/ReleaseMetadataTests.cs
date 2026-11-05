using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace GeneFoundry.Tests
{
    public class ReleaseMetadataTests
    {
        private static string Fixture(string path) => Path.Combine(AppContext.BaseDirectory, "Fixtures", path);
        private static XElement Metadata => XDocument.Load(Fixture("About/About.xml")).Root;

        [Fact]
        public void MetadataRetainsIdentityAndRequiresSupportedGamesAndBiotech()
        {
            var metadata = Metadata;
            Assert.Equal("ModMetaData", metadata.Name.LocalName);
            Assert.Equal("GeneFoundry", (string)metadata.Element("name"));
            Assert.Equal("Guy Symonds", (string)metadata.Element("author"));
            Assert.Equal("GBF.GeneticBiomassFabrication", (string)metadata.Element("packageId"));
            Assert.Equal(new[] { "1.6", "1.7" }, metadata.Element("supportedVersions").Elements().Select(element => element.Value));
            var dependency = Assert.Single(metadata.Element("modDependencies").Elements());
            Assert.Equal("Ludeon.RimWorld.Biotech", (string)dependency.Element("packageId"));
            Assert.Equal("Biotech", (string)dependency.Element("displayName"));
            Assert.Equal("https://github.com/GuySymonds/Rimworld_GeneFoundry", (string)metadata.Element("url"));
            Assert.False(string.IsNullOrWhiteSpace((string)metadata.Element("description")));
        }

        [Fact]
        public void AssemblyVersionsAgreeWithMetadataAndStartupUsesAssemblyVersion()
        {
            string version = (string)Metadata.Element("modVersion");
            Assert.Matches(@"^\d+\.\d+\.\d+$", version);
            string assembly = File.ReadAllText(Fixture("Source/Properties/AssemblyInfo.cs"));
            foreach (string attribute in new[] { "AssemblyVersion", "AssemblyFileVersion" })
                Assert.Equal(version + ".0", Regex.Match(assembly, attribute + "\\(\"([^\"]+)\"\\)").Groups[1].Value);
            Assert.Equal(version, Regex.Match(assembly, "AssemblyInformationalVersion\\(\"([^\"]+)\"\\)").Groups[1].Value);
            string startup = File.ReadAllText(Fixture("Source/Bootstrap.cs"));
            Assert.Contains("GetCustomAttribute<AssemblyInformationalVersionAttribute>", startup);
            Assert.DoesNotContain("Development build", startup);
            Assert.DoesNotContain("v0.", startup);
            Assert.Equal(File.ReadAllText(Fixture("Repository-LICENSE")), File.ReadAllText(Fixture("Mod-LICENSE")));
        }

        [Theory]
        [InlineData("Preview.png", 640, 360)]
        [InlineData("ModIcon.png", 64, 64)]
        public void WorkshopArtworkHasValidPngSignatureDimensionsAndSize(string name, int width, int height)
        {
            byte[] bytes = File.ReadAllBytes(Fixture("About/" + name));
            Assert.True(bytes.Length < 1000000);
            Assert.True(bytes.Length > 1000, "Artwork must not be an empty placeholder.");
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.Take(8));
            Assert.Equal(width, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
            Assert.Equal(height, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        }

#if RIMWORLD_TESTS
        [Fact]
        public void MetadataUsesRealRimWorld16Fields()
        {
            var schema = typeof(Verse.ModMetaData).GetNestedType("ModMetaDataInternal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            Assert.NotNull(schema);
            foreach (var element in Metadata.Elements())
                Assert.NotNull(schema.GetField(element.Name.LocalName));
            foreach (var element in Metadata.Element("modDependencies").Element("li").Elements())
                Assert.NotNull(typeof(Verse.ModDependency).GetField(element.Name.LocalName));
        }
#endif
    }
}
