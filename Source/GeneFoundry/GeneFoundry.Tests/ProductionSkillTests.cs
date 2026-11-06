using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace GeneFoundry.Tests
{
    public class ProductionSkillTests
    {
        private static string Source(string path) => File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "Fixtures/Source", path));

        [Theory]
        [InlineData("Recipes/GeneVatSkills.cs", "Cooking")]
        [InlineData("Jobs/JobDriver_PrepareBiomassRefinement.cs", "Intellectual")]
        public void ProductionReadsAndTrainsOnlyItsGoverningSkill(string path, string governing)
        {
            string source = Source(path);
            Assert.Contains("Learn(SkillDefOf." + governing + ",", source);
            foreach (string secondary in new[] { "Cooking", "Intellectual", "Crafting", "Medicine" }.Where(s => s != governing))
                Assert.DoesNotContain("SkillDefOf." + secondary, source);
        }

        [Fact]
        public void FabricationQualificationPerformanceAndLearningOnlyReadCrafting()
        {
            string source = Source("Fabrication/FabricationState.cs");
            string skills = source.Substring(source.IndexOf("public static class FabricationSkills"));
            skills = skills.Substring(0, skills.IndexOf("public partial class Building_GeneFabricator"));
            Assert.Contains("SkillDefOf.Crafting).TotallyDisabled", skills);
            Assert.Contains("Learn(SkillDefOf.Crafting, xp)", skills);
            Assert.DoesNotContain("SkillDefOf.Intellectual", skills);
            Assert.DoesNotContain("SkillDefOf.Medicine", skills);
        }

        [Theory]
        [InlineData("GeneVat.xml", "Cooking")]
        [InlineData("BiomassRefinement.xml", "Research")]
        [InlineData("GeneFabrication.xml", "Crafting")]
        public void WorkTypesMatchSpecialists(string file, string workType)
        {
            var def = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures/Defs/WorkGiverDefs", file));
            Assert.Equal(workType, def.Root.Element("WorkGiverDef").Element("workType").Value);
        }

        [Fact]
        public void MedicalRecoveryStillGuardsCorpseSelectionAndRunsBeforeExtraction()
        {
            Assert.Contains("!ArtificialPartRecovery.CanRecoverAll(pawn, corpse.InnerPawn)", Source("WorkGivers/WorkGiver_GeneVat.cs"));
            string medical = Source("Recipes/ArtificialPartRecovery.cs");
            Assert.Contains("SkillDefOf.Medicine).TotallyDisabled", medical);
            Assert.Contains("SurgeryRecipe(implant).PawnSatisfiesSkillRequirements(surgeon)", medical);
            string job = Source("Jobs/JobDriver_ExtractCorpseBiomass.cs");
            Assert.Contains("recovery.AddPreTickIntervalAction(delta => pawn.skills.Learn(SkillDefOf.Medicine", job);
            Assert.True(job.IndexOf("yield return recovery") < job.IndexOf("Toil processing ="));
        }
    }
}
