#if RIMWORLD_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
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
        public class BillData : IExposable
        {
            public Bill_GeneFabrication bill = new Bill_GeneFabrication();
            public void ExposeData() => bill.ExposeGeneData();
        }
        public class LinkedWorkData : IExposable
        {
            public Bill_GeneFabrication bill;
            public UnfinishedGenePack item;
            public void ExposeData() { bill.ExposeGeneData(); item.ExposeJobData(); }
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
            protected override Thing MakeRecoveredMaterial(ThingDef def, int count) => new Material { def = def, stackCount = count };
        }
        private static ThingDef ItemDef(string name)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name; def.stackLimit = 1; return def;
        }
        private static HeadlessItem Paid(float total = 100)
        {
            return new HeadlessItem { committed = true, totalWork = total, paidBiomass = 0, paidCapsules = 0, genes = new List<GeneDef>
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
        [Fact] public void FirstWorkerOwnsPaidWorkAndOtherPawnsCannotTakeOver()
        {
            var item = Paid();
            var first = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            var second = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            Assert.True(item.CanBeWorkedBy(first)); Assert.True(item.CanBeWorkedBy(second));
            Assert.False(item.TryClaim(null));
            item.AddWork(40, .3f); // Also covers migration of existing, unowned work.
            Assert.True(item.TryClaim(first));
            Assert.True(item.TryClaim(first)); Assert.False(item.TryClaim(second));
            Assert.Same(first, item.creator); Assert.Equal(40, item.workDone);
            var bench = new HeadlessBench(); bench.GetDirectlyHeldThings().TryAdd(item, false);
            Assert.True(bench.CanBeWorkedBy(first)); Assert.False(bench.CanBeWorkedBy(second));
            item.creator = null; // A missing owner reference must not transfer ownership.
            Assert.False(item.CanBeWorkedBy(second)); Assert.False(item.TryClaim(second));
        }
        [Fact] public void SettingAsidePreservesThePackOwnerAndBillWhileFreeingTheBench()
        {
            var bench = new HeadlessBench(); var item = Paid();
            var alice = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn)); alice.Name = new NameSingle("Alice");
            var bob = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn)); bob.Name = new NameSingle("Bob");
            item.TryClaim(alice); item.AddWork(40, .3f); item.fabricator = bench;
            var first = new Bill_GeneFabrication { genes = item.genes }; first.BindUnfinished(item);
            var next = new Bill_GeneFabrication { genes = item.genes };
            bench.billStack.AddBill(first); bench.billStack.AddBill(next);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(Building_GeneFabricator).GetField("activeBill", flags).SetValue(bench, first);
            typeof(Building_GeneFabricator).GetField("currentItem", flags).SetValue(bench, item);
            typeof(Building_GeneFabricator).GetField("selected", flags).SetValue(bench, new List<GeneDef>(item.genes));
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            Assert.False(bench.DetachUnfinished(item)); // Must be physically removed first.
            bench.GetDirectlyHeldThings().Remove(item); // Vanilla carrying performs this transfer.
            int generation = bench.Generation;
            Assert.True(bench.DetachUnfinished(item));
            Assert.Null(bench.Unfinished); Assert.Null(bench.ActiveBill); Assert.False(bench.Pending);
            Assert.NotEqual(generation, bench.Generation);
            Assert.Same(item, first.unfinished); Assert.Same(first, item.bill); Assert.Equal(40, item.workDone);
            Assert.True(first.CanBeScheduledFor(alice)); Assert.False(first.CanBeScheduledFor(bob));
            Assert.True(next.CanBeScheduledFor(bob));
            Assert.Contains("VanillaGene", item.GetInspectString()); Assert.Contains("ModdedGene", item.GetInspectString());
            Assert.Contains("Alice", item.GetInspectString());
            Assert.Equal("Alice", first.OwnerLabel); Assert.Equal("Not started", next.OwnerLabel);
        }
        [Fact] public void MissingPaidPackCannotRestartItsBillOrChargeMaterialsAgain()
        {
            var bench = new HeadlessBench(); var bill = new Bill_GeneFabrication { genes = Paid().genes };
            bill.BindUnfinished(Paid()); bill.unfinished = null;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(Building_GeneFabricator).GetField("activeBill", flags).SetValue(bench, bill);
            typeof(Building_GeneFabricator).GetField("selected", flags).SetValue(bench, bill.genes);
            Assert.True(bill.HasStarted); Assert.False(bill.ShouldDoNow());
            Assert.False(bench.CommitDeliveredMaterials()); Assert.Equal(0, bench.unfinishedCreated);
        }
        [Fact] public void ParkedPackAndBillReferencesRoundTripTogether()
        {
            var previous = DefDatabase<GeneDef>.AllDefsListForReading.ToList();
            string path = Path.GetTempFileName(); bool profiling = DeepProfiler.enabled; DeepProfiler.enabled = false;
            try
            {
                var recipe = (RecipeDef)RuntimeHelpers.GetUninitializedObject(typeof(RecipeDef)); recipe.defName = "GF_FabricateSelectedGenes";
                var source = new LinkedWorkData { item = Paid(), bill = new Bill_GeneFabrication { recipe = recipe } };
                var owner = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
                owner.def = ItemDef("Human"); owner.def.category = ThingCategory.Pawn; owner.thingIDNumber = 9876; owner.Name = new NameSingle("Alice");
                source.bill.genes = source.item.genes; Assert.True(source.item.TryClaim(owner));
                source.bill.BindUnfinished(source.item); source.item.AddWork(40, .3f);
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(source.item.genes);
                Scribe.saver.InitSaving(path, "work");
                Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(source.bill, "bill");
                Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(source.item, "item");
                Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(owner, "mapPawn");
                source.ExposeData(); Scribe.saver.FinalizeSaving();
                var loaded = new LinkedWorkData { item = Paid(), bill = new Bill_GeneFabrication { recipe = recipe } };
                Scribe.loader.InitLoading(path); Scribe.loader.curParent = loaded; Scribe.loader.curPathRelToParent = null;
                loaded.ExposeData(); Scribe.loader.crossRefs.RegisterForCrossRefResolve(loaded); Scribe.loader.initer.RegisterForPostLoadInit(loaded);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Scribe.loader.crossRefs);
                directory.RegisterLoaded(loaded.bill); directory.RegisterLoaded(loaded.item); directory.RegisterLoaded(owner); Scribe.loader.FinalizeLoading();
                Assert.Same(loaded.item, loaded.bill.unfinished); Assert.Same(loaded.bill, loaded.item.bill);
                Assert.True(loaded.bill.HasStarted); Assert.True(loaded.bill.ShouldDoNow()); Assert.Equal(40, loaded.item.workDone);
                Assert.Same(owner, loaded.item.creator); Assert.True(loaded.bill.CanBeScheduledFor(owner));
                Assert.False(loaded.bill.CanBeScheduledFor((Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn))));
            }
            finally
            {
                Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling;
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(previous);
            }
        }
        [Fact] public void OrphanedUnfinishedPackRetainsMaterialRecovery()
        {
            var item = Paid();
            Assert.True(item.Valid);
            Assert.Null(item.fabricator);
            item.paidBiomass = 20; item.paidCapsules = 4;
            Assert.True(item.CanRecoverOnDiscard);
            Assert.Equal(20, item.DiscardBiomass);
            Assert.Equal(4, item.DiscardCapsules);
        }
        [Fact] public void InvalidUnfinishedPackRetainsRecordedMaterialRecovery()
        {
            var item = Paid();
            item.totalWork = 0;
            item.fabricator = new HeadlessBench();
            Assert.False(item.Valid);
            item.paidBiomass = 20; item.paidCapsules = 4;
            Assert.True(item.CanRecoverOnDiscard);
            Assert.Equal(20, item.DiscardBiomass);
            Assert.Equal(4, item.DiscardCapsules);
        }
        [Fact] public void CancellingParkedWorkRecoversMaterialsOnceWithoutCancellingCurrentWork()
        {
            var previous = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
            try
            {
                var biomass = ItemDef("GF_RefinedBiomass"); biomass.stackLimit = 75;
                var capsule = ItemDef("ArchiteCapsule"); capsule.stackLimit = 10;
                DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(new[] { biomass, capsule });
                var bench = new HeadlessBench(); var parked = Paid(); parked.fabricator = bench;
                parked.paidBiomass = 20; parked.paidCapsules = 4;
                var oldBill = new Bill_GeneFabrication { genes = parked.genes }; oldBill.BindUnfinished(parked);
                var current = Paid(); var currentBill = new Bill_GeneFabrication { genes = current.genes }; currentBill.BindUnfinished(current);
                bench.billStack.AddBill(oldBill); bench.billStack.AddBill(currentBill);
                bench.GetDirectlyHeldThings().TryAdd(current, false);
                typeof(Building_GeneFabricator).GetField("activeBill", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(bench, currentBill);
                parked.totalWork = 0; // Invalid work still refunds its recorded paid ingredients.
                parked.Discard(); parked.Discard();
                Assert.Same(current, bench.Unfinished); Assert.Same(currentBill, bench.ActiveBill);
                Assert.Equal(2, bench.billStack.Count); Assert.Contains(currentBill, bench.billStack.Bills); Assert.Contains(oldBill, bench.billStack.Bills);
                Assert.False(oldBill.HasStarted); Assert.Null(oldBill.unfinished); Assert.True(oldBill.ShouldDoNow());
                Assert.True(parked.destroyed); Assert.False(parked.Valid);
                Assert.Equal(15, bench.GetDirectlyHeldThings().Where(t => t.def == biomass).Sum(t => t.stackCount));
                Assert.Equal(3, bench.GetDirectlyHeldThings().Where(t => t.def == capsule).Sum(t => t.stackCount));
            }
            finally { DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(previous); }
        }
        [Fact] public void UnstartedBillsHaveNoPawnRestrictionButUnfinishedWorkRetainsItsCreator()
        {
            var bill = new Bill_GeneFabrication { genes = Paid().genes };
            var alice = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn)); alice.Name = new NameSingle("Alice");
            var bob = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn)); bob.Name = new NameSingle("Bob");
            Assert.True(bill.CanBeScheduledFor(alice)); Assert.True(bill.CanBeScheduledFor(bob));
            Assert.Equal("Not started", bill.OwnerLabel);
            var item = Paid(); bill.BindUnfinished(item); Assert.True(item.TryClaim(bob));
            Assert.False(bill.CanBeScheduledFor(alice)); Assert.True(bill.CanBeScheduledFor(bob));
            bill.ReleaseUnfinished(item);
            Assert.True(bill.CanBeScheduledFor(alice)); Assert.True(bill.CanBeScheduledFor(bob));
        }
        [Fact] public void DestroyedUnfinishedWorkResetsItsBillWithoutRefundOrCompletion()
        {
            var bench = new HeadlessBench(); var item = Paid(); item.fabricator = bench;
            var bill = new Bill_GeneFabrication { genes = item.genes }; bill.BindUnfinished(item); bench.billStack.AddBill(bill);
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(Building_GeneFabricator).GetField("activeBill", flags).SetValue(bench, bill);
            typeof(Building_GeneFabricator).GetField("selected", flags).SetValue(bench, new List<GeneDef>(item.genes));
            int generation = bench.Generation;
            bench.NotifyUnfinishedDestroyed(item); // Called by the item's real Destroy hook before native destruction.
            Assert.Same(bill, Assert.Single(bench.billStack.Bills)); Assert.False(bill.HasStarted); Assert.True(bill.ShouldDoNow());
            Assert.Equal(item.genes, bill.genes); Assert.Null(bench.ActiveBill); Assert.False(bench.Pending);
            Assert.Empty(bench.GetDirectlyHeldThings()); Assert.NotEqual(generation, bench.Generation); Assert.Equal(0, bench.outputs);
        }
        [Fact] public void BillIsOnePackAndCannotRunAgainAfterCompletion()
        {
            var bench = new HeadlessBench();
            var bill = new Bill_GeneFabrication { genes = Paid().genes };
            bench.billStack.AddBill(bill);
            Assert.True(bill.ShouldDoNow()); Assert.False(bill.PawnAllowedToStartAnew(null));
            bill.suspended = true; Assert.False(bill.ShouldDoNow());
            bill.suspended = false; Assert.True(bill.ShouldDoNow());
            bill.Notify_IterationCompleted(null, new List<Thing>());
            Assert.Empty(bench.billStack.Bills); Assert.False(bill.ShouldDoNow());
            bill.Notify_IterationCompleted(null, new List<Thing>());
            Assert.Empty(bench.billStack.Bills);
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void CompletedBillLeavesQueueOnceOnSuccessOrFailure(bool failure)
        {
            var bench = new HeadlessBench(); var item = Paid();
            var bill = new Bill_GeneFabrication { genes = new List<GeneDef>(item.genes) };
            var next = new Bill_GeneFabrication { genes = new List<GeneDef>(item.genes) };
            bench.billStack.AddBill(bill); bench.billStack.AddBill(next);
            typeof(Building_GeneFabricator).GetField("activeBill", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(bench, bill);
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            item.AddWork(100, failure ? .3f : 0);
            Assert.True(bench.Complete(0));
            Assert.Null(bench.ActiveBill); Assert.False(bill.ShouldDoNow());
            Assert.Same(next, Assert.Single(bench.billStack.Bills));
            Assert.False(bench.Complete(0)); Assert.True(next.ShouldDoNow());
            Assert.Equal(failure ? 0 : 1, bench.outputs);
        }
        [Fact] public void DeletingActiveBillCancelsPaidItemWithoutTouchingOtherEntries()
        {
            var bench = new HeadlessBench(); var item = Paid();
            var active = new Bill_GeneFabrication { genes = item.genes };
            var next = new Bill_GeneFabrication { genes = item.genes };
            bench.billStack.AddBill(active); bench.billStack.AddBill(next);
            typeof(Building_GeneFabricator).GetField("activeBill", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(bench, active);
            bench.GetDirectlyHeldThings().TryAdd(item, false);
            int generation = bench.Generation;
            bench.billStack.Delete(active);
            Assert.Null(bench.ActiveBill); Assert.True(item.destroyed); Assert.Null(bench.Unfinished);
            Assert.Same(next, Assert.Single(bench.billStack.Bills));
            Assert.NotEqual(generation, bench.Generation); Assert.Equal(0, bench.outputs);
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
        [Fact] public void GenePriceOrderUsesDescendingBiomassThenNameWithStableTies()
        {
            var cheap = new GeneDef { defName = "Cheap", label = "Alpha", biostatCpx = 0 };
            var first = new GeneDef { defName = "First", label = "Alpha", biostatCpx = 2 };
            var second = new GeneDef { defName = "Second", label = "beta", biostatCpx = 2 };
            var tie = new GeneDef { defName = "Third", label = "beta", biostatCpx = 2 };
            var expensive = new GeneDef { defName = "Expensive", label = "Zulu", biostatCpx = 4 };
            Assert.Equal(new[] { expensive, first, second, tie, cheap },
                FabricationGeneCatalog.ByCost(new[] { tie, cheap, second, expensive, first }));
            Assert.Equal(51, Building_GeneFabricator.Biomass(new[] {
                new GeneDef { biostatCpx = -5 }, new GeneDef { biostatCpx = 2 }, new GeneDef { biostatCpx = 3 } }));
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
            Assert.False(bench.TryCommit(null));
            bench.Work(null, 100); // Must reject before reading a pawn or consuming anything.
            Assert.Equal(0, item.workDone);
            Assert.Same(item, bench.Unfinished);
        }
        private class Material : Thing
        {
            public bool destroyed;
            public int splitCount;
            public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { destroyed = true; }
            public override Thing SplitOff(int count)
            {
                splitCount += count;
                if (count >= stackCount) return base.SplitOff(count);
                // Only replace Unity's ThingMaker for partial-stack fixture creation.
                stackCount -= count;
                return new Material { def = def, stackCount = count };
            }
        }
        [Fact] public void DiscountedCommitKeepsExcessBiomassAndChargesAllArchiteCapsules()
        {
            var previous = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
            try
            {
                var biomass = ItemDef("GF_RefinedBiomass"); biomass.stackLimit = 75;
                var capsule = ItemDef("ArchiteCapsule"); capsule.stackLimit = 10;
                DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(new[] { biomass, capsule });
                var bench = new HeadlessBench();
                var genes = new List<GeneDef> { new GeneDef { biostatCpx = 1, biostatArc = 1 }, new GeneDef { biostatCpx = 0, biostatArc = 2 } };
                typeof(Building_GeneFabricator).GetField("selected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(bench, genes);
                var delivered = new Material { def = biomass, stackCount = 30 };
                var capsules = new Material { def = capsule, stackCount = 3 };
                bench.GetDirectlyHeldThings().TryAdd(delivered, false); bench.GetDirectlyHeldThings().TryAdd(capsules, false);
                Assert.Equal(24, bench.Required(biomass)); Assert.Equal(3, bench.Required(capsule));
                Assert.True(bench.CommitDeliveredMaterials());
                Assert.Equal(24, delivered.splitCount); Assert.Equal(6, delivered.stackCount); Assert.False(delivered.destroyed);
                Assert.Equal(3, capsules.splitCount); Assert.True(capsules.destroyed);
                Assert.Equal(6000, bench.Unfinished.totalWork); // Work receives no discount.
                Assert.True(bench.CommitDeliveredMaterials()); Assert.Equal(24, delivered.splitCount);
            }
            finally { DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(previous); }
        }
        [Fact] public void BillQueueRetainsNativeOrderingAndSuspension()
        {
            var bench = new HeadlessBench();
            var first = new Bill_GeneFabrication { genes = Paid().genes };
            var second = new Bill_GeneFabrication { genes = Paid().genes };
            bench.billStack.AddBill(first); bench.billStack.AddBill(second);
            Assert.Same(first, bench.billStack.FirstShouldDoNow);
            first.suspended = true; Assert.Same(second, bench.billStack.FirstShouldDoNow);
            first.suspended = false; bench.billStack.Reorder(second, -1);
            Assert.Same(second, bench.billStack.FirstShouldDoNow);
            second.Notify_IterationCompleted(null, new List<Thing>());
            Assert.Same(first, bench.billStack.FirstShouldDoNow);
        }
        [Theory]
        [InlineData(false)] [InlineData(true)]
        public void BillGeneSelectionsAndCompletedStateRoundTrip(bool completed)
        {
            var previous = DefDatabase<GeneDef>.AllDefsListForReading.ToList();
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled; DeepProfiler.enabled = false;
            try
            {
                var data = new BillData { bill = new Bill_GeneFabrication { genes = Paid().genes } };
                var expected = data.bill.genes.ToList();
                if (completed) data.bill.Notify_IterationCompleted(null, new List<Thing>());
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(expected);
                Scribe.saver.InitSaving(path, "fabrication"); Scribe_Deep.Look(ref data, "bill"); Scribe.saver.FinalizeSaving();
                // Old explicit assignments must be ignored, even when the referenced pawn
                // is absent. Only a physical unfinished pack's creator remains a restriction.
                var xml = XDocument.Load(path);
                xml.Root.Element("bill").Add(new XElement("gfBillAssignedPawn", "Thing_Human9999"));
                xml.Save(path);
                data = new BillData(); Scribe.loader.InitLoading(path); Scribe.EnterNode("bill");
                Scribe.loader.curParent = data; Scribe.loader.curPathRelToParent = null; data.ExposeData();
                Scribe.loader.crossRefs.RegisterForCrossRefResolve(data); Scribe.loader.initer.RegisterForPostLoadInit(data);
                Scribe.ExitNode(); Scribe.loader.FinalizeLoading();
                Assert.Equal(expected, data.bill.genes); Assert.Equal(!completed, data.bill.ShouldDoNow());
                Assert.Equal(!completed, data.bill.CanBeScheduledFor((Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn))));
            }
            finally
            {
                Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling;
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(previous);
            }
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
                Assert.False(bench.Pending);
                Assert.Equal(15, bench.GetDirectlyHeldThings().Where(t => t.def == biomass).Sum(t => t.stackCount));
                Assert.Equal(2, bench.GetDirectlyHeldThings().Where(t => t.def == capsule).Sum(t => t.stackCount));
            }
            finally { DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(previous); }
        }
        [Theory]
        [InlineData(false, .3f)] [InlineData(true, .3f)] [InlineData(true, .55f)]
        public void ScribeRoundTripPreservesSelectedGenesProgressRiskAndOwner(bool owned, float risk)
        {
            var previous = DefDatabase<GeneDef>.AllDefsListForReading.ToList();
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled; DeepProfiler.enabled = false;
            try
            {
                var item = Paid(); item.AddWork(90, risk);
                var owner = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
                owner.def = ItemDef("Human"); owner.def.category = ThingCategory.Pawn; owner.thingIDNumber = 1234;
                if (owned) Assert.True(item.TryClaim(owner));
                DefDatabase<GeneDef>.Clear(); DefDatabase<GeneDef>.Add(item.genes);
                var data = new JobData { item = item };
                Scribe.saver.InitSaving(path,"fabrication");
                // The pawn is supplied by the map in a real load; this fixture round-trips
                // the item's reference without running the entire Pawn/Unity serializer.
                Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(owner, "mapPawn");
                Scribe_Deep.Look(ref data,"job"); Scribe.saver.FinalizeSaving();
                data = new JobData();
                Scribe.loader.InitLoading(path); Scribe.EnterNode("job");
                Scribe.loader.curParent = data; Scribe.loader.curPathRelToParent = null; data.ExposeData();
                Scribe.loader.crossRefs.RegisterForCrossRefResolve(data);
                Scribe.loader.initer.RegisterForPostLoadInit(data);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Scribe.loader.crossRefs);
                directory.RegisterLoaded(owner);
                Scribe.ExitNode(); Scribe.loader.FinalizeLoading(); var loaded = data.item;
                Assert.True(loaded.Valid, $"committed={loaded.committed}, total={loaded.totalWork}, done={loaded.workDone}, risk={loaded.riskWork}, genes={string.Join(",", loaded.genes?.Select(g => g?.defName ?? "null") ?? new string[0])}; XML={File.ReadAllText(path)}");
                Assert.Equal(item.genes, loaded.genes);
                Assert.Equal(90,loaded.workDone);
                Assert.Equal(90 * risk,loaded.riskWork,4);
                if (owned)
                {
                    Assert.Same(owner, loaded.creator);
                    Assert.True(loaded.CanBeWorkedBy(owner));
                    Assert.False(loaded.CanBeWorkedBy((Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn))));
                }
                loaded.AddWork(10,0);
                Assert.Equal(.9f * risk,loaded.Failure,5);
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
