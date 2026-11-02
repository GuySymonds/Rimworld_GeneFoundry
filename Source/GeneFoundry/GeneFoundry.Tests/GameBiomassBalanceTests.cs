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
                Scribe.saver.InitSaving(path,"refinery"); source.PostExposeData(); Scribe.saver.FinalizeSaving();
                var loaded=new CompBiomassRefiner();
                Scribe.loader.InitLoading(path); loaded.PostExposeData(); Scribe.loader.FinalizeLoading();
                Assert.Equal(.375f,Field("batchFailureChance").GetValue(loaded));
                Assert.Equal(1d,Field("batchProgress").GetValue(loaded));
                Assert.Equal(true,Field("processing").GetValue(loaded));
                Assert.Equal(rolled,Field("completionRollMade").GetValue(loaded));
                Assert.Equal(4999,Field("powerOutageTicks").GetValue(loaded));
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
