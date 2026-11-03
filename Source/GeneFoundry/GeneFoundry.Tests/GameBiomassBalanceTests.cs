#if RIMWORLD_TESTS
using System;
using System.IO;
using System.Reflection;
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
        [InlineData(0,90000f)] [InlineData(1,85500f)]
        [InlineData(10,45000f)] [InlineData(20,45000f)]
        public void ActualRefineryUsesIndependentProcessorTiming(int processors,float ticks)
        {
            var settings = GeneFoundryMod.Settings;
            int previous = settings.processingMinutes, minimum = settings.minimumProcessingMinutes,
                reduction = settings.processorReductionPercent;
            try
            {
                settings.processingMinutes=2160; settings.minimumProcessingMinutes=1080;
                settings.processorReductionPercent=5;
                var comp=new CompBiomassRefiner();
                Assert.Equal(ticks,(float)typeof(CompBiomassRefiner).GetMethod("ProcessingDurationTicks",Private)
                    .Invoke(comp,new object[]{processors}));
            }
            finally
            {
                settings.processingMinutes=previous; settings.minimumProcessingMinutes=minimum;
                settings.processorReductionPercent=reduction;
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
