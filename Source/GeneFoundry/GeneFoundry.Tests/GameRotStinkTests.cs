#if RIMWORLD_TESTS
using System;
using System.IO;
using GeneFoundry.Components;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameRotStinkTests
    {
        [Fact]
        public void IncidentCannotOverlapOrBeStoppedByASafeReplacementOperator()
        {
            var vat = new CompGeneVatRotStink();
            Assert.True(vat.TryStartIncident(100, .15f, 0f));
            Assert.False(vat.TryStartIncident(200, .15f, 0f));
            Assert.False(vat.TryStartIncident(300, 0f, 0f));
            Assert.True(vat.ActiveAt(2599));
            Assert.False(vat.ActiveAt(2600));
        }

        [Fact]
        public void EmissionsContinueWithoutWorkerAndStopAtExactly2500Ticks()
        {
            var vat = new CompGeneVatRotStink();
            vat.TryStartIncident(100, .15f, 0f);
            int count = 0;
            for (int tick = 100; tick <= 3000; tick++)
                if (vat.EmissionDue(tick)) count++;
            Assert.Equal(10, count);
            Assert.False(vat.EmissionDue(3100));
            Assert.True(vat.TryStartIncident(3100, .05f, 0f));
        }

        [Fact]
        public void VatsHaveIndependentDeadlinesAndSchedules()
        {
            var first = new CompGeneVatRotStink();
            var second = new CompGeneVatRotStink();
            first.TryStartIncident(100, .15f, 0f);
            Assert.False(second.ActiveAt(100));
            second.TryStartIncident(500, .15f, 0f);
            Assert.True(first.EmissionDue(500));
            Assert.True(second.EmissionDue(500));
            Assert.False(first.ActiveAt(2600));
            Assert.True(second.ActiveAt(2600));
        }

        [Theory]
        [InlineData(RotStage.Fresh)]
        [InlineData(RotStage.Rotting)]
        [InlineData(RotStage.Dessicated)]
        public void DecayNeverChangesRisk(RotStage stage)
        {
            // Decay is intentionally absent from the production hazard contract.
            Assert.NotNull(Enum.GetName(stage));
            Assert.Equal(.15f, BiomassCalculations.CorpseRotStinkRisk(3, 3, 2), 5);
            Assert.Equal(3, typeof(BiomassCalculations).GetMethod("CorpseRotStinkRisk").GetParameters().Length);
        }

        [Fact]
        public void IncidentScribeRoundTripPreservesDeadlineAndNextEmission()
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var source = new CompGeneVatRotStink();
                source.TryStartIncident(100, .15f, 0f);
                Assert.True(source.EmissionDue(100));
                Scribe.saver.InitSaving(path, "vat");
                source.PostExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = new CompGeneVatRotStink();
                Scribe.loader.InitLoading(path);
                loaded.PostExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.False(loaded.EmissionDue(349));
                Assert.True(loaded.EmissionDue(350));
                Assert.True(loaded.ActiveAt(2599));
                Assert.False(loaded.ActiveAt(2600));
            }
            finally
            {
                Scribe.ForceStop();
                File.Delete(path);
                DeepProfiler.enabled = profiling;
            }
        }

        [Fact]
        public void DespawnedOrDestroyedVatDoesNotEvenAdvanceEmissionSchedule()
        {
            var vat = new CompGeneVatRotStink { parent = new ThingWithComps() };
            vat.TryStartIncident(100, .15f, 0f);
            vat.CompTick(); // No TickManager/map needed: despawn guard returns first.
            Assert.True(vat.EmissionDue(100));
            // Set vanilla's destroyed state without Unity destruction notifications.
            typeof(Thing).GetField("mapIndexOrState", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(vat.parent, (sbyte)-2);
            Assert.True(vat.parent.Destroyed);
            vat.CompTick();
            Assert.True(vat.EmissionDue(350));
        }

        [Fact]
        public void SafeSkillsAndFailedRollsCannotStartAnIncident()
        {
            var vat = new CompGeneVatRotStink();
            Assert.False(vat.TryStartIncident(100, 0f, 0f));
            Assert.False(vat.TryStartIncident(100, .05f, .05f));
            Assert.False(vat.TryStartIncident(100, .15f, .99f));
            Assert.False(vat.EmissionDue(100));
            Assert.True(vat.TryStartIncident(100, .05f, .049f));
        }

        [Fact]
        public void GasUsesInstalledVanillaRotStinkApi()
        {
            Assert.NotNull(typeof(GasUtility).GetMethod("AddGas",
                new[] { typeof(IntVec3), typeof(Map), typeof(GasType), typeof(int) }));
            string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures/Source/Components/CompGeneVatRotStink.cs"));
            Assert.Contains("GasUtility.AddGas", source);
            Assert.Contains("GasType.RotStink, GasPerEmission", source);
            Assert.DoesNotContain("Hediff", source);
        }
    }
}
#endif
