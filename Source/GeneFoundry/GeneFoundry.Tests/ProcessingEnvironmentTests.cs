using GeneFoundry.Recipes;
using GeneFoundry.Fabrication;
using Xunit;

namespace GeneFoundry.Tests
{
    public class ProcessingEnvironmentTests
    {
        [Theory]
        [InlineData(1f, 0f)] [InlineData(.3f, 0f)] [InlineData(.15f, .05f)]
        [InlineData(0f, .1f)] [InlineData(-1f, .1f)]
        public void LightPenaltyTapersToZeroAtThirtyPercent(float light, float expected) =>
            Assert.Equal(expected, BiomassCalculations.DimLightAdjustment(light), 6);

        [Theory]
        [InlineData(-2, false, 1f, true, true, .15f)]
        [InlineData(-1, false, 1f, true, true, .075f)]
        [InlineData(1, false, 1f, true, true, 0f)]
        [InlineData(-2, false, 0f, true, true, .25f)]
        [InlineData(-2, true, 0f, true, true, .2f)]
        [InlineData(-2, true, 1f, false, false, .1f)]
        [InlineData(-2, false, 0f, true, false, .1f)]
        [InlineData(-2, false, 1f, false, true, 0f)]
        public void ExposureReplacesDirtAndLightStacksIndependently(double dirt, bool outdoors,
            float light, bool includeDirt, bool enabled, float expected) =>
            Assert.Equal(expected, BiomassCalculations.ProcessingEnvironmentAdjustment(
                dirt, outdoors, light, includeDirt, enabled), 6);

        [Theory]
        [InlineData(100, .1f, 90)] [InlineData(100, .2f, 80)]
        [InlineData(10000, .1f, 9000)]
        [InlineData(25, .1f, 22)] [InlineData(1, .2f, 1)]
        public void ExtractionEnvironmentAddsWastageWhilePreservingOneBiomass(int baseline, float penalty, int expected) =>
            Assert.Equal(expected, BiomassCalculations.ExtractionOutput(baseline, 20, 20, 20, .5, penalty));

        [Fact]
        public void FabricationCanExceedBaseSkillRiskAndWeightsConditionsByWork()
        {
            float dirtyDark = BiomassCalculations.ProcessingEnvironmentAdjustment(-2, false, 0, true);
            float badRisk = BiomassCalculations.CombinedRefinementFailure(.3f, dirtyDark);
            Assert.Equal(.55f, badRisk, 6);
            Assert.Equal(.425f, FabricationCalculations.WeightedFailure(50 * badRisk + 50 * .3f, 100), 6);
            Assert.Equal(1f, FabricationCalculations.WeightedFailure(200, 100));
            Assert.Equal(1f, BiomassCalculations.CombinedRefinementFailure(.8f, dirtyDark));
        }

        [Fact]
        public void PenaltySettingsAreBoundedAndCanBeDisabled()
        {
            Assert.Equal(0f, BiomassCalculations.ProcessingEnvironmentAdjustment(-2, true, 0, true,
                dirtyPenaltyPoints: 0, exposurePenaltyPoints: 0, dimLightPenaltyPoints: 0));
            Assert.Equal(1f, BiomassCalculations.ProcessingEnvironmentAdjustment(-2, true, 0, true,
                exposurePenaltyPoints: 999, dimLightPenaltyPoints: 999));
            Assert.Equal(0f, BiomassCalculations.DimLightAdjustment(0, -10));
        }
    }
}
