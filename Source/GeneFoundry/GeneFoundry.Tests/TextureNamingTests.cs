using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace GeneFoundry.Tests
{
    public class TextureNamingTests
    {
        private static string Root => Path.Combine(AppContext.BaseDirectory, "Fixtures/Textures");
        private static HashSet<string> Paths => Directory.GetFiles(Root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);

        [Fact]
        public void RuntimeNamesHaveConsistentCaseAndNoCollisions()
        {
            var paths = Paths;
            Assert.NotEmpty(paths);
            Assert.Equal(paths.Count, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (string path in paths)
                Assert.Matches(@"^(?:[A-Z][A-Za-z0-9]*/)+[A-Z][A-Za-z0-9]*(?:_(?:[A-Z][A-Za-z0-9]*|[0-9]{2}))*(?:_(?:north|east|south|west))?\.png$", path);
        }

        [Fact]
        public void EveryDefinitionTextureResolvesWithExactCaseAndEveryDetailedDirectionExists()
        {
            var paths = Paths;
            string definitions = Path.Combine(AppContext.BaseDirectory, "Fixtures/Defs");
            foreach (var file in Directory.GetFiles(definitions, "*.xml", SearchOption.AllDirectories))
            foreach (var texture in XDocument.Load(file).Descendants("texPath").Where(x => x.Value.StartsWith("GeneFoundry/", StringComparison.Ordinal)))
            {
                string path = texture.Value;
                string graphic = (string)texture.Parent.Element("graphicClass");
                if (graphic == "Graphic_StackCount")
                    Assert.Contains(paths, candidate => candidate.StartsWith(path + "/", StringComparison.Ordinal));
                else if (graphic == "Graphic_Multi" || graphic == "GeneFoundry.Recipes.Graphic_GeneVat" ||
                    graphic == "GeneFoundry.Components.Graphic_BiomassRefinery" || graphic == "GeneFoundry.Fabrication.Graphic_GeneFabricator")
                    foreach (string direction in new[] { "north", "east", "south", "west" })
                        Assert.Contains(path + "_" + direction + ".png", paths);
                else Assert.Contains(path + ".png", paths);
            }
        }

        [Fact]
        public void AllFabricatorIdleAndProcessingFramesMatchEachDirectionalBaseCanvas()
        {
            foreach (string direction in new[] { "north", "east", "south", "west" })
            {
                string prefix = "GeneFoundry/Buildings/";
                byte[] chassis = File.ReadAllBytes(Path.Combine(Root, prefix + "GeneFabricator_Detailed_" + direction + ".png"));
                foreach (string state in new[] { "Idle" }.Concat(Enumerable.Range(0, 8).Select(i => $"Processing_{i:00}")))
                {
                    string path = prefix + $"GeneFabricatorFrames/GeneFabricator_Detailed_{state}_{direction}.png";
                    Assert.Contains(path, Paths);
                    byte[] frame = File.ReadAllBytes(Path.Combine(Root, path));
                    Assert.Equal(chassis.Skip(16).Take(8), frame.Skip(16).Take(8));
                }
            }
        }
    }
}
