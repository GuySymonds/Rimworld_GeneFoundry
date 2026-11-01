#if RIMWORLD_TESTS
using System.Reflection;
using System.Runtime.CompilerServices;
using GeneFoundry.Jobs;
using Verse;
using Verse.AI;
using Xunit;

namespace GeneFoundry.Tests
{
    // Opt-in tests use the installed game's actual managed API without starting Unity.
    public class GameIngredientTrackingTests
    {
        private static T Uninitialized<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

        [Fact]
        public void CustomBillJobRecordsDeliveredIngredientsWithoutVanillaJobIdentity()
        {
            Job job = Uninitialized<Job>();
            job.def = Uninitialized<JobDef>();
            job.def.defName = "GF_ExtractMaterialBiomass";
            Thing meat = Uninitialized<Thing>();
            meat.stackCount = 50;
            GeneVatIngredientPlacement.RecordPlacement(job, meat, 50);
            ThingCountClass receipt = Assert.Single(job.placedThings);
            Assert.Same(meat, receipt.thing);
            Assert.Equal(50, receipt.Count);
            Assert.Equal("GF_ExtractMaterialBiomass", job.def.defName);
        }

        [Fact]
        public void SplitDeliveriesMergedIntoOneStackRetainTheirFullDeliveredCount()
        {
            Job job = Uninitialized<Job>();
            Thing stack = Uninitialized<Thing>();
            stack.stackCount = 50;
            GeneVatIngredientPlacement.RecordPlacement(job, stack, 20);
            GeneVatIngredientPlacement.RecordPlacement(job, stack, 30);
            Assert.Equal(50, Assert.Single(job.placedThings).Count);
        }

        [Fact]
        public void MeatAndHerbReceiptsRemainSeparate()
        {
            Job job = Uninitialized<Job>();
            Thing meat = Uninitialized<Thing>();
            Thing herb = Uninitialized<Thing>();
            meat.stackCount = 50;
            herb.stackCount = 1;
            GeneVatIngredientPlacement.RecordPlacement(job, meat, 50);
            GeneVatIngredientPlacement.RecordPlacement(job, herb, 1);
            Assert.Equal(2, job.placedThings.Count);
            Assert.Equal(50, job.placedThings[0].Count);
            Assert.Equal(1, job.placedThings[1].Count);
        }

        [Fact]
        public void MissingIngredientTrackingCannotCompleteARecipe()
        {
            Assert.False(GeneVatIngredientPlacement.HasFullRecipeCost(Uninitialized<Job>()));
        }

        [Theory]
        [InlineData(0, 100, 99)]
        [InlineData(1, 99, 100)]
        [InlineData(2, 100, 101)]
        [InlineData(3, 101, 100)]
        public void CorpseCellIsFrontCentreInsideVat(int rotation, int x, int z)
        {
            Thing vat = Uninitialized<Thing>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Thing).GetField("positionInt", flags).SetValue(vat, new IntVec3(100, 0, 100));
            typeof(Thing).GetField("rotationInt", flags).SetValue(vat, new Rot4(rotation));
            IntVec3 cell = GeneVatIngredientPlacement.CorpseCell(vat);
            Assert.Equal(new IntVec3(x, 0, z), cell);
            CellRect footprint = GenAdj.OccupiedRect(new IntVec3(100, 0, 100), new Rot4(rotation), new IntVec2(3, 3));
            Assert.True(footprint.Contains(cell));
            Assert.False(footprint.Contains(vat.Position - new Rot4(rotation).FacingCell * 2));
        }
    }
}
#endif
