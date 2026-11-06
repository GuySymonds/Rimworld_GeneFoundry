#if RIMWORLD_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GeneFoundry.Components;
using GeneFoundry.Jobs;
using RimWorld;
using Verse;
using Verse.AI;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameRefineryLifecycleTests : IDisposable
    {
        static GameRefineryLifecycleTests() => GameProductionSkillTests.InitializeSkills();

        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static T Uninitialized<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        private static FieldInfo BatchField(string name) => typeof(CompBiomassRefiner).GetField(name, Private);
        private readonly Game previousGame = Current.Game;
        private readonly SkillDef previousIntellectual = SkillDefOf.Intellectual;

        public GameRefineryLifecycleTests()
        {
            SkillDefOf.Intellectual = new SkillDef { defName = "Intellectual" };
            var game = Uninitialized<Game>();
            typeof(Game).GetField("maps", Private).SetValue(game, new List<Map> { Uninitialized<Map>() });
            Current.Game = game;
        }

        public void Dispose()
        {
            Current.Game = previousGame;
            SkillDefOf.Intellectual = previousIntellectual;
        }

        // Replace only map placement and player notifications. Tick dispatch, saved
        // state, completion risk, power accounting and output receipts stay real.
        private class HeadlessRefinery : CompBiomassRefiner
        {
            public bool spaceAvailable;
            public int placementAttempts, outputCount, losses;
            protected override bool TryPlaceOutput()
            {
                placementAttempts++;
                if (!spaceAvailable) return false;
                outputCount += ((CompProperties_BiomassRefiner)props).outputCount;
                return true;
            }
            protected override void NotifyBatchLost(string message) { losses++; }
        }

        private static HeadlessRefinery Batch(out CompPowerTrader power, double progress = 1d)
        {
            var parent = Uninitialized<ThingWithComps>();
            typeof(Thing).GetField("mapIndexOrState", Private).SetValue(parent, (sbyte)0);
            power = new CompPowerTrader { parent = parent };
            typeof(ThingWithComps).GetField("comps", Private).SetValue(parent, new List<ThingComp> { power });
            var refinery = new HeadlessRefinery { parent = parent };
            refinery.Initialize(new CompProperties_BiomassRefiner());
            BatchField("processing").SetValue(refinery, true);
            BatchField("batchProgress").SetValue(refinery, progress);
            BatchField("completionRollMade").SetValue(refinery, true);
            BatchField("vatHasProcessed").SetValue(refinery, true);
            return refinery;
        }

        [Fact]
        public void CompletedOutputSurvivesLongOutageAndPlacesExactlyOnceWithoutReroll()
        {
            var refinery = Batch(out var power);
            BatchField("batchFailureChance").SetValue(refinery, 1f); // A reroll would lose this batch.
            BatchField("powerOutageTicks").SetValue(refinery, 4999);
            for (int i = 0; i < 6000; i++) refinery.CompTick();
            Assert.Equal(6000, refinery.placementAttempts);
            Assert.Equal(0, refinery.losses);
            Assert.Equal(0, refinery.outputCount);
            Assert.Equal(0, BatchField("powerOutageTicks").GetValue(refinery));
            Assert.Equal(true, BatchField("processing").GetValue(refinery));
            Assert.Equal(1d, BatchField("batchProgress").GetValue(refinery));
            Assert.Equal(-100f, power.PowerOutput);
            Assert.False(refinery.CanStartBatch());
            Assert.Contains("Output waiting for space", refinery.CompInspectStringExtra());
            typeof(CompPowerTrader).GetField("powerOnInt", Private).SetValue(power, true);
            Assert.False(refinery.AcceptsProcessorAssistance);
            typeof(CompPowerTrader).GetField("powerOnInt", Private).SetValue(power, false);
            refinery.spaceAvailable = true;
            refinery.CompTick(); // Placement does not need restored electricity.
            for (int i = 0; i < 20; i++) refinery.CompTick();
            Assert.Equal(10, refinery.outputCount);
            Assert.Equal(6001, refinery.placementAttempts);
            Assert.Equal(false, BatchField("processing").GetValue(refinery));
            Assert.Equal(0, refinery.losses);
        }

        [Fact]
        public void NewlyCompletedReactionRecordsSuccessBeforeWaitingAtIdlePower()
        {
            var refinery = Batch(out var power);
            BatchField("completionRollMade").SetValue(refinery, false);
            typeof(CompPowerTrader).GetField("powerOnInt", Private).SetValue(power, true);
            refinery.CompTick(); // Zero failure chance; placement remains blocked.
            Assert.Equal(true, BatchField("completionRollMade").GetValue(refinery));
            Assert.Equal(1, refinery.placementAttempts);
            Assert.Equal(-100f, power.PowerOutput);
            Assert.False(refinery.AcceptsProcessorAssistance);
            BatchField("batchFailureChance").SetValue(refinery, 1f);
            refinery.spaceAvailable = true;
            refinery.CompTick();
            refinery.CompTick();
            Assert.Equal(10, refinery.outputCount);
            Assert.Equal(0, refinery.losses);
        }

        [Fact]
        public void PendingOutputSaveLoadKeepsSuccessAndClearsOldOutageBeforeRetry()
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                var source = Batch(out _);
                BatchField("batchFailureChance").SetValue(source, 1f);
                BatchField("powerOutageTicks").SetValue(source, 4999);
                Scribe.saver.InitSaving(path, "refinery");
                source.PostExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = Batch(out _);
                Scribe.loader.InitLoading(path);
                loaded.PostExposeData();
                Scribe.loader.FinalizeLoading();
                for (int i = 0; i < 5001; i++) loaded.CompTick();
                Assert.Equal(0, loaded.losses);
                Assert.Equal(true, BatchField("completionRollMade").GetValue(loaded));
                loaded.spaceAvailable = true;
                loaded.CompTick();
                loaded.CompTick();
                Assert.Equal(10, loaded.outputCount);
                Assert.Equal(0, BatchField("powerOutageTicks").GetValue(loaded));
                Assert.Equal(1, loaded.VatFrameAt(10000));
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        [Fact]
        public void IncompleteReactionStillSpoilsAtExactlyTwoHoursWithoutPower()
        {
            var refinery = Batch(out _, .5d);
            BatchField("powerOutageTicks").SetValue(refinery, 4998);
            refinery.CompTick();
            Assert.Equal(0, refinery.losses);
            Assert.Equal(4999, BatchField("powerOutageTicks").GetValue(refinery));
            refinery.CompTick();
            Assert.Equal(1, refinery.losses);
            Assert.Equal(false, BatchField("processing").GetValue(refinery));
            Assert.Equal(0, refinery.placementAttempts);
        }

        private class PreparationDriver : JobDriver_PrepareBiomassRefinement
        {
            public List<Toil> CreateToils() => MakeNewToils().ToList();
        }
        private class PreparationBill : Bill
        {
            public override bool ShouldDoNow() => !suspended;
        }

        private static PreparationDriver ReloadPreparation(PreparationDriver source)
        {
            string path = Path.GetTempFileName();
            bool profiling = DeepProfiler.enabled;
            DeepProfiler.enabled = false;
            try
            {
                Scribe.saver.InitSaving(path, "preparation");
                source.ExposeData();
                Scribe.saver.FinalizeSaving();
                var loaded = new PreparationDriver();
                Scribe.loader.InitLoading(path);
                Scribe.loader.curParent = loaded;
                Scribe.loader.curPathRelToParent = null;
                loaded.ExposeData();
                Scribe.loader.crossRefs.RegisterForCrossRefResolve(loaded);
                Scribe.loader.initer.RegisterForPostLoadInit(loaded);
                Scribe.loader.FinalizeLoading();
                // The containing pawn/job tracker owns these links in a live save.
                loaded.pawn = source.pawn;
                loaded.job = source.job;
                return loaded;
            }
            finally { Scribe.ForceStop(); File.Delete(path); DeepProfiler.enabled = profiling; }
        }

        [Theory]
        [InlineData("skill disabled")]
        [InlineData("skill below minimum")]
        [InlineData("power loss")]
        [InlineData("suspended bill")]
        [InlineData("deleted bill")]
        [InlineData("missing bill")]
        [InlineData("drafted worker")]
        [InlineData("despawned workstation")]
        public void PreparationStopsAndAwardsNoXpOrConsumesInputsWhenInvalidated(string change)
        {
            var bench = new Building_WorkTable();
            bench.def = Uninitialized<ThingDef>();
            bench.def.building = new BuildingProperties();
            typeof(Thing).GetField("mapIndexOrState", Private).SetValue(bench, (sbyte)0);
            var power = new CompPowerTrader { parent = bench };
            typeof(CompPowerTrader).GetField("powerOnInt", Private).SetValue(power, true);
            typeof(Building_WorkTable).GetField("powerComp", Private).SetValue(bench, power);
            var refinery = new CompBiomassRefiner { parent = bench };
            typeof(ThingWithComps).GetField("comps", Private).SetValue(bench, new List<ThingComp> { refinery });
            var bill = new PreparationBill { recipe = new RecipeDef(), billStack = bench.billStack };
            var pawn = new Pawn();
            pawn.skills = new Pawn_SkillTracker(pawn);
            var skill = new SkillRecord(pawn, SkillDefOf.Intellectual) { Level = 5 };
            typeof(SkillRecord).GetField("cachedTotallyDisabled", Private).SetValue(skill, BoolUnknown.False);
            typeof(SkillRecord).GetField("cachedPermanentlyDisabled", Private).SetValue(skill, BoolUnknown.False);
            typeof(SkillRecord).GetField("aptitudeCached", Private).SetValue(skill, (int?)0);
            pawn.skills.skills.Add(skill);
            pawn.drafter = new Pawn_DraftController(pawn);
            var catalyst = Uninitialized<Thing>();
            catalyst.stackCount = 1;
            var driver = new PreparationDriver { pawn = pawn, job = new Job { targetA = bench, targetB = catalyst, bill = bill } };
            // A restored duration avoids definition discovery, which belongs to game startup.
            typeof(JobDriver_PrepareBiomassRefinement).GetField("preparationTicks", Private).SetValue(driver, 100);
            driver.ticksLeftThisToil = 37;
            driver = ReloadPreparation(driver);
            Assert.Equal(100, typeof(JobDriver_PrepareBiomassRefinement).GetField("preparationTicks", Private).GetValue(driver));
            Assert.Equal(37, driver.ticksLeftThisToil);
            var toils = driver.CreateToils();
            var preparation = Assert.Single(toils, t => t.defaultCompleteMode == ToilCompleteMode.Delay);
            var learn = Assert.Single((List<Action<int>>)typeof(Toil).GetField("preTickIntervalActions", Private | BindingFlags.Public).GetValue(preparation),
                callback => ReferenceEquals(callback.Target, driver)); // Exclude Unity's progress-bar callback.
            var stop = ((List<Func<JobCondition>>)typeof(JobDriver).GetField("globalFailConditions", Private | BindingFlags.Public).GetValue(driver)).Last();
            Assert.True(driver.CanContinuePreparation);
            Assert.Equal(JobCondition.Ongoing, stop());
            float xp = pawn.skills.skills.Sum(s => s.xpSinceLastLevel);
            switch (change)
            {
                case "skill disabled": typeof(SkillRecord).GetField("cachedTotallyDisabled", Private).SetValue(skill, BoolUnknown.True); break;
                case "skill below minimum": skill.Level = 4; break;
                case "power loss": typeof(CompPowerTrader).GetField("powerOnInt", Private).SetValue(power, false); break;
                case "suspended bill": bill.suspended = true; break;
                case "deleted bill": bench.billStack.AddBill(bill); bench.billStack.Delete(bill); break;
                case "missing bill": driver.job.bill = null; break;
                case "drafted worker": typeof(Pawn_DraftController).GetField("draftedInt", Private).SetValue(pawn.drafter, true); break;
                case "despawned workstation": typeof(Thing).GetField("mapIndexOrState", Private).SetValue(bench, (sbyte)-1); break;
            }
            Assert.False(driver.CanContinuePreparation);
            Assert.Equal(JobCondition.Incompletable, stop());
            learn(600);
            Assert.Equal(xp, pawn.skills.skills.Sum(s => s.xpSinceLastLevel));
            Assert.Equal(1, catalyst.stackCount);
            Assert.Equal(false, BatchField("processing").GetValue(refinery));
            Assert.Equal(false, BatchField("completionRollMade").GetValue(refinery));
        }
    }
}
#endif
