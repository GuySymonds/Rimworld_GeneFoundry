#if RIMWORLD_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GeneFoundry.Fabrication;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameFabricationTests
    {
        // Suppress only Unity destruction/notifications and ThingMaker DLC startup. The
        // production owner, completion, vanilla Genepack.Initialize and job math remain real.
        private class HeadlessItem : UnfinishedGenePack
        {
            public bool destroyed;
            public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { destroyed = true; }
        }
        private class HeadlessOwner : ThingOwner<Thing>
        {
            public HeadlessOwner(IThingHolder holder) : base(holder) { }
            protected override void NotifyAdded(Thing item) { }
            protected override void NotifyRemoved(Thing item) { }
        }
        public class JobData : IExposable
        {
            public UnfinishedGenePack item = new UnfinishedGenePack();
            public void ExposeData() => item.ExposeJobData();
        }
        private class HeadlessBench : Building_GeneFabricator
        {
            public HeadlessBench()
            {
                typeof(Building_GeneFabricator).GetField("contents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, new HeadlessOwner(this));
            }
            public int outputs, failures;
            public int unfinishedCreated;
            protected override UnfinishedGenePack MakeUnfinished()
            {
                unfinishedCreated++; return new HeadlessItem { def = ItemDef("GF_UnfinishedGenePack") };
            }
            protected override Genepack MakeOutput()
            {
                outputs++;
                var result = (Genepack)RuntimeHelpers.GetUninitializedObject(typeof(Genepack));
                result.def = ItemDef("Genepack");
                result.stackCount = 1;
                return result;
            }
            protected override void NotifyFailure() { failures++; }
        }
        private static ThingDef ItemDef(string name)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name; def.stackLimit = 1; return def;
        }
        private static HeadlessItem Paid(float total = 100)
        {
            return new HeadlessItem { committed = true, totalWork = total, genes = new List<GeneDef>
                { new GeneDef { defName = "VanillaGene" }, new GeneDef { defName = "ModdedGene" } },
                def = ItemDef("GF_UnfinishedGenePack") };
        }
        [Fact] public void DeveloperSpawnedItemCannotEarnProgressOrComplete()
        {
            var item = new UnfinishedGenePack();
            Assert.False(item.Valid);
            Assert.Equal(0, item.AddWork(100, 0));
            var bench = new HeadlessBench();
            Assert.False(bench.Complete(1));
            Assert.Equal(0, bench.outputs);
        }
        [Fact] public void ChangingWorkersWeightsActualWorkAndClampsFinalTick()
        {
            var item = Paid();
            Assert.Equal(90, item.AddWork(90, .3f));
            Assert.Equal(10, item.AddWork(40, 0));
            Assert.Equal(100, item.workDone);
            Assert.Equal(.27f, item.Failure, 5);
            Assert.Equal(0, item.AddWork(100, .3f));
            Assert.Equal(.27f, item.Failure, 5);
        }
        [Theory]
        [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1)]
        public void InvalidWorkCannotCorruptProgress(float work)
        {
            var item = Paid();
            Assert.Equal(0, item.AddWork(work, .3f));
            Assert.Equal(0, item.workDone);
            Assert.True(item.Valid);
        }
        [Fact] public void MissingRemovedOrDuplicateGenesInvalidatePaidState()
        {
            var item = Paid(); item.genes.Add(null); Assert.False(item.Valid);
            item = Paid(); item.genes.Add(item.genes[0]); Assert.False(item.Valid);
            item = Paid(); item.riskWork = float.NaN; Assert.False(item.Valid);
            item = Paid(); item.totalWork = float.PositiveInfinity; Assert.False(item.Valid);
        }
        [Fact] public void IncompleteStateCannotProduceOutput()
        {
            var bench = new HeadlessBench(); var item = Paid();
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            item.AddWork(99, 0);
            Assert.False(bench.Complete(1));
            Assert.Same(item, bench.Unfinished);
            Assert.Equal(0, bench.outputs);
        }
        [Fact] public void SuccessUsesVanillaMultiGenePackAndRetainsExactlyOneUnplacedOutput()
        {
            var bench = new HeadlessBench(); var item = Paid();
            GeneDef[] genes = item.genes.ToArray();
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            item.AddWork(100, 0);
            Assert.True(bench.Complete(0)); // Zero risk never fails, even for a zero roll.
            Assert.Null(bench.Unfinished);
            var output = Assert.IsType<Genepack>(Assert.Single(bench.GetDirectlyHeldThings()));
            Assert.Equal(genes, output.GeneSet.GenesListForReading);
            Assert.True(item.destroyed);
            Assert.False(bench.Complete(0));
            Assert.Equal(1, bench.outputs);
            Assert.Equal(0, bench.failures);
        }
        [Fact] public void FailureDestroysUnfinishedAndNotifiesExactlyOnceWithoutOutput()
        {
            var bench = new HeadlessBench(); var item = Paid();
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            item.AddWork(100, .3f);
            Assert.True(bench.Complete(.1f));
            Assert.Empty(bench.GetDirectlyHeldThings());
            Assert.True(item.destroyed);
            Assert.False(bench.Complete(.9f));
            Assert.Equal(0, bench.outputs);
            Assert.Equal(1, bench.failures);
        }
        [Fact] public void CostsSumPositiveStatsIndependentlyIncludingModdedArchites()
        {
            var genes = new[] { new GeneDef { biostatCpx = -5, biostatArc = -2 },
                new GeneDef { biostatCpx = 2, biostatArc = 1 }, new GeneDef { biostatCpx = 3, biostatArc = 2 } };
            Assert.Equal(5, Building_GeneFabricator.Complexity(genes));
            Assert.Equal(3, Building_GeneFabricator.Archites(genes));
        }
        [Fact] public void UnpoweredUnspawnedBenchCannotConsumeOrAdvance()
        {
            var bench = new HeadlessBench(); var item = Paid();
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            Assert.False(bench.CanWork);
            Assert.False(bench.TryCommit());
            bench.Work(null, 100); // Must reject before reading a pawn or consuming anything.
            Assert.Equal(0, item.workDone);
            Assert.Same(item, bench.Unfinished);
        }
        private class Material : Thing
        {
            public bool destroyed;
            public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { destroyed = true; }
        }
        [Fact] public void MaterialsCommitOnlyWhenCompleteAndNeverChargeTwice()
        {
            var previous = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
            try
            {
                var biomass = ItemDef("GF_RefinedBiomass"); biomass.stackLimit = 75;
                var capsule = ItemDef("ArchiteCapsule"); capsule.stackLimit = 10;
                DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(new[] { biomass, capsule });
                var bench = new HeadlessBench();
                var genes = new List<GeneDef> { new GeneDef { defName = "Modded", biostatCpx = 2, biostatArc = 3 } };
                typeof(Building_GeneFabricator).GetField("selected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(bench, genes);
                var first = new Material { def = biomass, stackCount = 12 };
                var second = new Material { def = biomass, stackCount = 8 };
                var arc = new Material { def = capsule, stackCount = 3 };
                bench.GetDirectlyHeldThings().TryAdd(first, false);
                Assert.Equal(8, bench.Missing(biomass));
                Assert.False(bench.CommitDeliveredMaterials()); Assert.False(first.destroyed);
                bench.GetDirectlyHeldThings().TryAdd(second, false);
                Assert.False(bench.CommitDeliveredMaterials()); Assert.False(second.destroyed);
                bench.GetDirectlyHeldThings().TryAdd(arc, false);
                Assert.True(bench.CommitDeliveredMaterials());
                Assert.True(first.destroyed); Assert.True(second.destroyed); Assert.True(arc.destroyed);
                Assert.Equal(genes, bench.Unfinished.genes);
                Assert.Equal(4500, bench.Unfinished.totalWork);
                Assert.True(bench.CommitDeliveredMaterials());
                Assert.Equal(1, bench.unfinishedCreated);
                Assert.Single(bench.GetDirectlyHeldThings());
                bench.Unfinished.AddWork(100, .3f);
                Assert.True(bench.CommitDeliveredMaterials());
                Assert.Equal(100, bench.Unfinished.workDone);
                int generation = bench.Generation; bench.Cancel();
                Assert.True(bench.Generation != generation);
                Assert.False(bench.Pending); Assert.Empty(bench.GetDirectlyHeldThings());
            }
            finally { DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(previous); }
        }
        [Fact] public void ScribeRoundTripPreservesSelectedGenesProgressAndWeightedRisk()
        {
            var previous = DefDatabase<GeneDef>.AllDefsListForReading.ToList();
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled; DeepProfiler.enabled = false;
            try
            {
                var item = Paid(); item.AddWork(90, .3f);
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(item.genes);
                var data = new JobData { item = item };
                Scribe.saver.InitSaving(path,"fabrication"); Scribe_Deep.Look(ref data,"job"); Scribe.saver.FinalizeSaving();
                data = new JobData();
                Scribe.loader.InitLoading(path); Scribe.EnterNode("job");
                Scribe.loader.curParent = data; data.ExposeData();
                Scribe.loader.crossRefs.RegisterForCrossRefResolve(data);
                Scribe.loader.initer.RegisterForPostLoadInit(data);
                Scribe.ExitNode(); Scribe.loader.FinalizeLoading(); var loaded = data.item;
                Assert.True(loaded.Valid, $"committed={loaded.committed}, total={loaded.totalWork}, done={loaded.workDone}, risk={loaded.riskWork}, genes={string.Join(",", loaded.genes?.Select(g => g?.defName ?? "null") ?? new string[0])}; XML={File.ReadAllText(path)}");
                Assert.Equal(item.genes, loaded.genes);
                Assert.Equal(90,loaded.workDone);
                Assert.Equal(27,loaded.riskWork,4);
                loaded.AddWork(10,0);
                Assert.Equal(.27f,loaded.Failure,5);
            }
            finally
            {
                Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling;
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(previous);
            }
        }
    }
}
#endif
