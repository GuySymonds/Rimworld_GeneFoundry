#if RIMWORLD_TESTS
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GeneFoundry.Components;
using GeneFoundry.Jobs;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;
using Verse.AI;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameBiomassBalanceTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static FieldInfo Field(string name) => typeof(CompBiomassRefiner).GetField(name, Private);

        [Theory]
        [InlineData(360, 120, 20)]
        [InlineData(2160, 1080, 108)]
        [InlineData(180, 90, 10)]
        public void ProcessingSettingsRoundTripWithoutReplacingSavedValues(int minutes, int minimum, int reduction)
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var source = new GeneFoundrySettings { processingMinutes = minutes,
                    minimumProcessingMinutes = minimum, processorReductionMinutes = reduction };
                Scribe.saver.InitSaving(path, "settings");
                source.ExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = new GeneFoundrySettings();
                Scribe.loader.InitLoading(path);
                loaded.ExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.Equal(minutes, loaded.processingMinutes);
                Assert.Equal(minimum, loaded.minimumProcessingMinutes);
                Assert.Equal(reduction, loaded.processorReductionMinutes);
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        private static T Uninitialized<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

        [Theory]
        [InlineData("<processorReductionPercent>10</processorReductionPercent>", 2160, 1080, 216f)]
        [InlineData("<processingMinutes>180</processingMinutes><minimumProcessingMinutes>60</minimumProcessingMinutes>", 180, 60, 9f)]
        [InlineData("<processingMinutes>360</processingMinutes><minimumProcessingMinutes>120</minimumProcessingMinutes><processorReductionPercent>5</processorReductionPercent>", 360, 120, 18f)]
        public void LegacyPercentageSettingsKeepEquivalentDurations(string fields, int minutes, int minimum, float reduction)
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                File.WriteAllText(path, "<settings>" + fields + "</settings>");
                var loaded = new GeneFoundrySettings();
                Scribe.loader.InitLoading(path);
                loaded.ExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.Equal(minutes, loaded.processingMinutes);
                Assert.Equal(minimum, loaded.minimumProcessingMinutes);
                Assert.Equal(reduction, loaded.processorReductionMinutes);
                Scribe.saver.InitSaving(path, "settings");
                loaded.ExposeData();
                Scribe.saver.FinalizeSaving();
                var reloaded = new GeneFoundrySettings();
                Scribe.loader.InitLoading(path);
                reloaded.ExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.Equal(reduction, reloaded.processorReductionMinutes);
                Assert.Equal(minutes, reloaded.processingMinutes);
                Assert.Equal(minimum, reloaded.minimumProcessingMinutes);
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        private class BiomassStack : Thing
        {
            public override Thing SplitOff(int count)
            {
                stackCount -= count;
                return new BiomassStack { def = def, stackCount = count };
            }
            public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { }
        }

        [Theory]
        [InlineData(44, false)]
        [InlineData(45, true)]
        [InlineData(50, true)]
        public void RefineryRequiresAndConsumesExactlyFortyFiveFromHoppers(int available, bool sufficient)
        {
            Game previous = Current.Game;
            try
            {
                var map = Uninitialized<Map>();
                map.info = Uninitialized<MapInfo>();
                typeof(MapInfo).GetField("sizeInt", Private).SetValue(map.info, new IntVec3(5, 1, 5));
                map.cellIndices = new CellIndices(5, 5);
                map.thingGrid = new ThingGrid(map);
                var game = Uninitialized<Game>();
                typeof(Game).GetField("maps", Private).SetValue(game, new List<Map> { map });
                Current.Game = game;
                var parent = Uninitialized<ThingWithComps>();
                parent.def = Uninitialized<ThingDef>();
                parent.def.size = new IntVec2(1, 1);
                typeof(Thing).GetField("positionInt", Private).SetValue(parent, new IntVec3(2, 0, 2));
                typeof(Thing).GetField("mapIndexOrState", Private).SetValue(parent, (sbyte)0);
                var hopper = Uninitialized<Building_Storage>();
                hopper.def = Uninitialized<ThingDef>();
                hopper.def.defName = "GF_BiomassHopper";
                var hopperCell = new IntVec3(2, 0, 1);
                typeof(Thing).GetField("positionInt", Private).SetValue(hopper, hopperCell);
                map.thingGrid.ThingsListAt(hopperCell).Add(hopper);
                var biomass = Uninitialized<ThingDef>();
                var stack = new BiomassStack { def = biomass, stackCount = available };
                map.thingGrid.ThingsListAt(hopperCell).Add(stack);
                var refinery = new CompBiomassRefiner { parent = parent };
                refinery.Initialize(new CompProperties_BiomassRefiner { inputDef = biomass });
                var consume = typeof(CompBiomassRefiner).GetMethod("TryConsumeInputs", Private);
                Assert.Equal(sufficient, consume.Invoke(refinery, new object[] { false }));
                Assert.Equal(available, stack.stackCount);
                Assert.Equal(sufficient, consume.Invoke(refinery, new object[] { true }));
                Assert.Equal(sufficient ? available - 45 : available, stack.stackCount);
            }
            finally { Current.Game = previous; }
        }

        [Fact]
        public void EnvironmentSettingsPersistAndClampTheNewLightPenalty()
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var settings = new GeneFoundrySettings {
                    refinementDirtyPenalty = 20, refinementExposurePenalty = 12, dimLightPenalty = 7 };
                Scribe.saver.InitSaving(path, "settings");
                settings.ExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = new GeneFoundrySettings();
                Scribe.loader.InitLoading(path);
                loaded.ExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.Equal(20, loaded.refinementDirtyPenalty);
                Assert.Equal(12, loaded.refinementExposurePenalty);
                Assert.Equal(7, loaded.dimLightPenalty);
                loaded.dimLightPenalty = 100;
                loaded.Validate();
                Assert.Equal(50, loaded.dimLightPenalty);
                loaded.dimLightPenalty = -10;
                loaded.Validate();
                Assert.Equal(0, loaded.dimLightPenalty);
                Assert.Equal(10, new GeneFoundrySettings().dimLightPenalty);
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        [Theory]
        [InlineData(0,360f)] [InlineData(1,340f)]
        [InlineData(11,140f)] [InlineData(12,120f)]
        [InlineData(10,160f)] [InlineData(20,120f)]
        public void ActualRefineryUsesIndependentProcessorTiming(int processors,float minutes)
        {
            var settings = GeneFoundryMod.Settings;
            int previous = settings.processingMinutes, minimum = settings.minimumProcessingMinutes;
            float reduction = settings.processorReductionMinutes;
            try
            {
                var defaults = new GeneFoundrySettings();
                Assert.Equal(360, defaults.processingMinutes);
                Assert.Equal(120, defaults.minimumProcessingMinutes);
                Assert.Equal(20f, defaults.processorReductionMinutes);
                settings.processingMinutes=defaults.processingMinutes; settings.minimumProcessingMinutes=defaults.minimumProcessingMinutes;
                settings.processorReductionMinutes=defaults.processorReductionMinutes;
                var comp=new CompBiomassRefiner();
                Assert.Equal(minutes*2500f/60f,(float)typeof(CompBiomassRefiner).GetMethod("ProcessingDurationTicks",Private)
                    .Invoke(comp,new object[]{processors}));
            }
            finally
            {
                settings.processingMinutes=previous; settings.minimumProcessingMinutes=minimum;
                settings.processorReductionMinutes=reduction;
            }
        }

        [Theory]
        [InlineData(37)] // Partway through filling frame 03.
        [InlineData(157)] // Partway through processing frame 15, just before wrap.
        public void PowerOutageFreezesVisualClockAcrossSaveAndResumesWithinFrame(int elapsed)
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var source = new CompBiomassRefiner();
                Field("processing").SetValue(source, true);
                Field("vatHasProcessed").SetValue(source, true);
                Field("vatStartedAt").SetValue(source, 1000);
                Field("batchProgress").SetValue(source, .25d);
                int tick = 1000 + elapsed;
                int frozen = source.VatFrameAt(tick);
                for (int i = 0; i < 123; i++)
                {
                    source.TickVatAnimation(false);
                    Assert.Equal(frozen, source.VatFrameAt(++tick));
                }
                Scribe.saver.InitSaving(path, "refinery");
                source.PostExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = new CompBiomassRefiner();
                Scribe.loader.InitLoading(path);
                loaded.PostExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.Equal(frozen, loaded.VatFrameAt(tick));
                for (int i = 0; i < 20; i++)
                {
                    loaded.TickVatAnimation(false);
                    Assert.Equal(frozen, loaded.VatFrameAt(++tick));
                }
                for (int i = 1; i <= 10; i++)
                {
                    loaded.TickVatAnimation(true);
                    Assert.Equal(BiomassRefineryAnimation.FrameAt(true, true, elapsed + i), loaded.VatFrameAt(++tick));
                }
                Assert.Equal(.25d, Field("batchProgress").GetValue(loaded));
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        [Theory]
        [InlineData(false, false, 0, 0)]
        [InlineData(false, true, 0, 1)]
        [InlineData(true, true, 40, 6)]
        [InlineData(true, true, 240, 10)]
        public void RefineryVisualStateSurvivesScribe(bool active, bool used, int elapsed, int expected)
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var source = new CompBiomassRefiner();
                Field("processing").SetValue(source, active);
                Field("vatHasProcessed").SetValue(source, used);
                Field("vatStartedAt").SetValue(source, 1000);
                Field("batchProgress").SetValue(source, .25d);
                Scribe.saver.InitSaving(path, "refinery");
                source.PostExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = new CompBiomassRefiner();
                Scribe.loader.InitLoading(path);
                loaded.PostExposeData();
                Scribe.loader.FinalizeLoading();
                Assert.Equal(expected, loaded.VatFrameAt(1000 + elapsed));
                Assert.Equal(.25d, Field("batchProgress").GetValue(loaded));
                // Every existing end path clears processing; a started chamber stays dirty.
                Field("processing").SetValue(loaded, false);
                Assert.Equal(used ? 1 : 0, loaded.VatFrameAt(1000 + elapsed));
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void RealBatchScribePreservesCapturedRiskProgressAndCompletionRoll(bool rolled)
        {
            string path=Path.GetTempFileName();
            bool profiling=DeepProfiler.enabled; DeepProfiler.enabled=false;
            try
            {
                var source=new CompBiomassRefiner();
                Field("batchFailureChance").SetValue(source,.375f);
                Field("batchLightLevel").SetValue(source,.15f);
                Field("batchProgress").SetValue(source,1d);
                Field("processing").SetValue(source,true);
                Field("completionRollMade").SetValue(source,rolled);
                Field("powerOutageTicks").SetValue(source,4999);
                Field("vatHasProcessed").SetValue(source,true);
                Field("vatStartedAt").SetValue(source,12000);
                Scribe.saver.InitSaving(path,"refinery"); source.PostExposeData(); Scribe.saver.FinalizeSaving();
                var loaded=new CompBiomassRefiner();
                Scribe.loader.InitLoading(path); loaded.PostExposeData(); Scribe.loader.FinalizeLoading();
                Assert.Equal(.375f,Field("batchFailureChance").GetValue(loaded));
                Assert.Equal(.15f,Field("batchLightLevel").GetValue(loaded));
                Assert.Equal(1d,Field("batchProgress").GetValue(loaded));
                Assert.Equal(true,Field("processing").GetValue(loaded));
                Assert.Equal(rolled,Field("completionRollMade").GetValue(loaded));
                Assert.Equal(4999,Field("powerOutageTicks").GetValue(loaded));
                Assert.Equal(true,Field("vatHasProcessed").GetValue(loaded));
                Assert.Equal(12000,Field("vatStartedAt").GetValue(loaded));
                Assert.Equal(1,loaded.VatFrameAt(12040)); // Completed output pending is dirty.
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled=profiling; }
        }

        [Theory]
        [InlineData(25,20)] [InlineData(25,22)] [InlineData(8,1)] [InlineData(20,20)]
        public void MaterialStatReadsCachedCompletionWithoutRerolling(int baseline,int output)
        {
            var pawn=new Pawn();
            pawn.skills=new Pawn_SkillTracker(pawn);
            pawn.jobs=new Pawn_JobTracker(pawn);
            var driver=new JobDriver_ExtractMaterialBiomass();
            var bill=(Bill_Production)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Bill_Production));
            bill.recipe=new RecipeDef {
                products=new System.Collections.Generic.List<ThingDefCountClass> { new ThingDefCountClass((ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)),baseline) }
            };
            driver.job=new Job { bill=bill };
            pawn.jobs.curDriver=driver;
            typeof(JobDriver_ExtractMaterialBiomass).GetField("completedOutput",Private).SetValue(driver,output);
            var stat=new StatPart_GeneVatYield();
            for(int attempt=0;attempt<10;attempt++)
            {
                float efficiency=1f;
                stat.TransformValue(StatRequest.For(pawn),ref efficiency);
                Assert.Equal(output,(int)Math.Ceiling(baseline*efficiency));
            }
        }
    }
}
#endif
