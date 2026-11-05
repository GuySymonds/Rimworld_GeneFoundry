#if RIMWORLD_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using GeneFoundry.Components;
using GeneFoundry.Fabrication;
using GeneFoundry.Recipes;
using UnityEngine;
using Verse;
using Verse.AI;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameGraphicsStyleTests
    {
        private class Vat : Graphic_GeneVat
        {
            public Func<string, Material> load;
            protected override Material LoadSimpleMaterial(GraphicRequest req, string path) => load(path);
        }
        private class Refinery : Graphic_BiomassRefinery
        {
            public Func<string, Material> load;
            protected override Material LoadSimpleMaterial(GraphicRequest req, string path) => load(path);
        }
        private class Fabricator : Graphic_GeneFabricator
        {
            public Func<string, Material> load;
            protected override Material LoadSimpleMaterial(GraphicRequest req, string path) => load(path);
        }
        private class Machine : ThingWithComps
        {
            public Graphic graphic;
            public override Graphic Graphic => graphic;
        }
        private static Material Material() => (Material)RuntimeHelpers.GetUninitializedObject(typeof(Material));
        private static Graphic_StyledMachine Graphic(string machine, Func<string, Material> load)
        {
            switch (machine)
            {
                case "GeneVat": return new Vat { load = load };
                case "BiomassRefinery": return new Refinery { load = load };
                default: return new Fabricator { load = load };
            }
        }

        [Theory]
        [InlineData("GeneVat")]
        [InlineData("BiomassRefinery")]
        [InlineData("GeneFabricator")]
        public void EveryAvailabilityCombinationSelectsExactDirectionalMaterialWithoutMixingMachines(string machine)
        {
            string[] directions = { "north", "east", "south", "west" };
            foreach (bool detailed in new[] { true, false })
            for (int mask = 0; mask < 16; mask++)
            {
                var available = new Dictionary<string, Material>();
                var detailedMaterials = new[] { Material(), Material(), Material(), Material() };
                for (int direction = 0; direction < 4; direction++)
                    if ((mask & (1 << direction)) != 0)
                        available.Add($"GeneFoundry/Buildings/{machine}_Simple_{directions[direction]}", Material());
                var requests = new List<string>();
                var graphic = Graphic(machine, path => {
                    requests.Add(path);
                    return available.TryGetValue(path, out var material) ? material : null;
                });
                // Substitute only the Unity materials, retaining native MatAt/MatSingle dispatch.
                typeof(Graphic_Multi).GetField("mats", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(graphic, detailedMaterials);
                graphic.drawSize = new Vector2(3f, 3.6f);
                graphic.data = new GraphicData { drawRotated = false };
                graphic.ApplySimpleGraphics(new GraphicRequest {
                    path = $"GeneFoundry/Buildings/{machine}_Detailed"
                }, detailed);
                Assert.Equal(detailed ? 0 : 4, requests.Count);
                Assert.False(graphic.ShouldDrawRotated);
                Assert.Equal(new Vector2(3f, 3.6f), graphic.drawSize);
                for (int direction = 0; direction < 4; direction++)
                {
                    bool simple = !detailed && (mask & (1 << direction)) != 0;
                    var rotation = new Rot4(direction);
                    var building = new Machine { graphic = graphic, Rotation = rotation };
                    Assert.Equal(simple, graphic.UsesSimple(rotation));
                    Assert.Equal(simple, Graphic_StyledMachine.UsesSimple(building));
                    Material expected = simple
                        ? available[$"GeneFoundry/Buildings/{machine}_Simple_{directions[direction]}"]
                        : detailedMaterials[direction];
                    Assert.Same(expected, graphic.MatAt(rotation));
                    if (!simple) continue;
                    // These real entry points must return before consulting work state,
                    // creating ingredient suppression or loading any Detailed overlays.
                    if (machine == "GeneVat") new CompGeneVatAnimation { parent = building }.NotifyProcessing(null);
                    if (machine == "BiomassRefinery") new CompBiomassRefiner { parent = building }.PostDraw();
                }
                Assert.Same(graphic.MatAt(Rot4.South), graphic.MatSingle);
            }
        }


        private class CorpseSource : ICorpseLiquidationPresentation
        {
            public Corpse LiquidatingCorpse { get; set; }
            public Thing LiquidationVat { get; set; }
            public float LiquidationProgress => .5f;
        }
        private class CorpsePresentation : MapComponent_CorpseLiquidationVisuals
        {
            public readonly HashSet<Corpse> hidden = new HashSet<Corpse>();
            public CorpsePresentation() : base(null) { }
            protected override bool IsVisibleCorpse(Corpse corpse) => corpse != null;
            protected override void SuppressDrawing(Corpse corpse) => hidden.Add(corpse);
            protected override void RestoreDrawing(Corpse corpse) => hidden.Remove(corpse);
        }
        [Fact]
        public void SimpleVatLeavesCorpseDrawingNativeWhileMissingDirectionRetainsFullPresentation()
        {
            var graphic = new Vat { load = path => path.EndsWith("_north", StringComparison.Ordinal) ? Material() : null };
            graphic.ApplySimpleGraphics(new GraphicRequest { path = "GeneFoundry/Buildings/GeneVat_Detailed" }, false);
            var simple = new CorpseSource {
                LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)),
                LiquidationVat = new Machine { graphic = graphic, Rotation = Rot4.North }
            };
            var fallback = new CorpseSource {
                LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)),
                LiquidationVat = new Machine { graphic = graphic, Rotation = Rot4.East }
            };
            var presentation = new CorpsePresentation();
            presentation.Synchronize(new[] { simple, fallback }, 100);
            Assert.DoesNotContain(simple.LiquidatingCorpse, presentation.hidden);
            Assert.Equal(-1, presentation.BubbleFrameFor(simple.LiquidatingCorpse, 145));
            Assert.Contains(fallback.LiquidatingCorpse, presentation.hidden);
            Assert.Equal(7, presentation.BubbleFrameFor(fallback.LiquidatingCorpse, 145));
            fallback.LiquidationVat.Rotation = Rot4.North;
            presentation.Synchronize(new[] { simple, fallback }, 145);
            Assert.Empty(presentation.hidden);
            Assert.Equal(-1, presentation.BubbleFrameFor(fallback.LiquidatingCorpse, 145));
        }

        [Fact]
        public void SimpleRecipeWorkSkipsOnlyNativeVisualEffectCallback()
        {
            int visuals = 0, otherTicks = 0;
            var graphic = new Vat { load = path => Material() };
            graphic.ApplySimpleGraphics(new GraphicRequest { path = "GeneFoundry/Buildings/GeneVat_Detailed" }, false);
            var building = new Machine { graphic = graphic, Rotation = Rot4.North };
            var job = new Verse.AI.Job { targetA = building };
            var pawn = new Pawn { jobs = null };
            pawn.jobs = new Verse.AI.Pawn_JobTracker(pawn);
            pawn.jobs.curJob = job;
            Toil toil = ToilMaker.MakeToil("GraphicsWorkEffectTest");
            toil.actor = pawn;
            toil.AddPreTickAction(() => otherTicks++);
            Verse.AI.ToilEffects.WithEffect(toil, () => { visuals++; return null; }, Verse.AI.TargetIndex.A);
            var work = toil.tickAction;
            var interval = toil.tickIntervalAction;
            var finish = toil.finishActions.ToArray();
            GeneFoundry.Jobs.JobDriver_ExtractMaterialBiomass.ConfigureWorkEffects(toil);
            foreach (var tick in toil.preTickActions) tick();
            Assert.Equal(0, visuals);
            Assert.Equal(1, otherTicks);
            Assert.Same(work, toil.tickAction);
            Assert.Same(interval, toil.tickIntervalAction);
            Assert.Equal(finish, toil.finishActions);
            // Missing Simple art retains vanilla effect ticks for that direction.
            graphic.load = path => null;
            graphic.ApplySimpleGraphics(new GraphicRequest { path = "GeneFoundry/Buildings/GeneVat_Detailed" }, false);
            foreach (var tick in toil.preTickActions) tick();
            Assert.Equal(1, visuals);
            Assert.Equal(2, otherTicks);
        }

        [Theory]
        [InlineData(true)] [InlineData(false)]
        public void GraphicsSettingRoundTripsWithoutReplacingReleasedSettings(bool detailed)
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var source = new GeneFoundrySettings { useDetailedGraphics = detailed,
                    processingMinutes = 180, minimumProcessingMinutes = 90, processorReductionMinutes = 10f,
                    corpseMinutesPer50Material = 30f, corpseSetupMinutes = 4f, corpseHandlingPercent = 40,
                    expertSpeedPercent = 180, refinementCleanlinessEnabled = false, refinementDirtyPenalty = 12,
                    refinementExposurePenalty = 7, dimLightPenalty = 8, geneFabricationResearchCost = 1700,
                    geneCategoryBaseCost = 700, geneResearchPerGene = 125, geneResearchPerComplexity = 300,
                    geneResearchPerArchite = 600, geneCategoryCostPercent = 30,
                    geneCategoryMaximumCost = 5000, geneCategoryMinimumCost = 1200 };
                Scribe.saver.InitSaving(path, "settings");
                source.ExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = Load(path);
                foreach (var field in typeof(GeneFoundrySettings).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Assert.Equal(field.GetValue(source), field.GetValue(loaded));
                // A released settings file has no graphics key. Its saved values must
                // remain intact and Detailed must default on even when loading into false.
                var xml = System.Xml.Linq.XDocument.Load(path);
                xml.Root.Element("useDetailedGraphics")?.Remove();
                xml.Save(path);
                loaded = Load(path);
                Assert.True(loaded.useDetailedGraphics);
                foreach (var field in typeof(GeneFoundrySettings).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    if (field.Name != "useDetailedGraphics") Assert.Equal(field.GetValue(source), field.GetValue(loaded));
                Assert.True(new GeneFoundrySettings().useDetailedGraphics);
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }
        [Fact]
        public void FixedReleasedSettingsPayloadRetainsEveryCustomValueAndDefaultsToDetailed()
        {
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var loaded = Load(Path.Combine(AppContext.BaseDirectory, "Fixtures/ReleasedSettings100.xml"));
                loaded.Validate();
                var expected = new GeneFoundrySettings {
                    processingMinutes = 2160, minimumProcessingMinutes = 1080, processorReductionMinutes = 108f,
                    corpseMinutesPer50Material = 30f, corpseSetupMinutes = 4f, corpseHandlingPercent = 40,
                    expertSpeedPercent = 180, refinementCleanlinessEnabled = false, refinementDirtyPenalty = 12,
                    refinementExposurePenalty = 7, dimLightPenalty = 8, geneFabricationResearchCost = 1700,
                    geneCategoryBaseCost = 700, geneResearchPerGene = 125, geneResearchPerComplexity = 300,
                    geneResearchPerArchite = 600, geneCategoryCostPercent = 30,
                    geneCategoryMaximumCost = 5000, geneCategoryMinimumCost = 1200
                };
                foreach (var field in typeof(GeneFoundrySettings).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Assert.Equal(field.GetValue(expected), field.GetValue(loaded));
            }
            finally { Scribe.ForceStop(); DeepProfiler.enabled = profiling; }
        }

        private static GeneFoundrySettings Load(string path)
        {
            var loaded = new GeneFoundrySettings { useDetailedGraphics = false };
            Scribe.loader.InitLoading(path);
            loaded.ExposeData();
            Scribe.loader.FinalizeLoading();
            return loaded;
        }
    }
}
#endif
