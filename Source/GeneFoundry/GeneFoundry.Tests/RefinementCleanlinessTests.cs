using GeneFoundry.Recipes;
using Xunit;

namespace GeneFoundry.Tests
{
    public class RefinementCleanlinessTests
    {
        [Theory]
        [InlineData(-5d, 0.15f)]
        [InlineData(-2d, 0.15f)]
        [InlineData(-1d, 0.075f)]
        [InlineData(-0.5d, 0.0375f)]
        [InlineData(0d, 0f)]
        [InlineData(0.3d, 0f)]
        [InlineData(0.6d, 0f)]
        [InlineData(2d, 0f)]
        public void DefaultAdjustmentsInterpolateAndSaturate(double cleanliness, float expected)
        {
            Assert.Equal(expected, BiomassCalculations.RefinementCleanlinessAdjustment(cleanliness, false), 6);
        }

        [Theory]
        [InlineData(-10d)]
        [InlineData(0d)]
        [InlineData(10d)]
        public void ExposureReplacesRoomAdjustmentInsteadOfStacking(double cleanliness)
        {
            Assert.Equal(0.10f, BiomassCalculations.RefinementCleanlinessAdjustment(cleanliness, true), 6);
        }

        [Theory]
        [InlineData(-2d, false)]
        [InlineData(0.6d, false)]
        [InlineData(-2d, true)]
        public void DisabledInfluenceIgnoresDirtSterilityAndExposure(double cleanliness, bool exposed)
        {
            Assert.Equal(0f, BiomassCalculations.RefinementCleanlinessAdjustment(cleanliness, exposed, false));
        }

        [Fact]
        public void CustomSettingsControlEachEffectIndependently()
        {
            Assert.Equal(0.2f, BiomassCalculations.RefinementCleanlinessAdjustment(-1, false,
                dirtyPenaltyPoints: 40), 6);
            Assert.Equal(0f, BiomassCalculations.RefinementCleanlinessAdjustment(0.3, false), 6);
            Assert.Equal(0.3f, BiomassCalculations.RefinementCleanlinessAdjustment(0.6, true,
                exposurePenaltyPoints: 30), 6);
            Assert.Equal(0f, BiomassCalculations.RefinementCleanlinessAdjustment(-2, false,
                dirtyPenaltyPoints: 0));
        }

        [Theory]
        [InlineData(0.12f, 0.15f, 0.27f)]
        [InlineData(0.12f, 0f, 0.12f)]
        [InlineData(0.12f, -0.05f, 0.07f)]
        [InlineData(0f, 0.15f, 0.15f)]
        [InlineData(0f, -0.05f, 0f)]
        [InlineData(0.9f, 0.5f, 1f)]
        public void FinalRiskAddsPercentagePointsAndClamps(float operatorRisk, float adjustment, float expected)
        {
            Assert.Equal(expected, BiomassCalculations.CombinedRefinementFailure(operatorRisk, adjustment), 6);
        }

        [Fact]
        public void IncreasingCleanlinessNeverIncreasesFailureRisk()
        {
            float previous = 1f;
            for (int step = -30; step <= 20; step++)
            {
                float adjustment = BiomassCalculations.RefinementCleanlinessAdjustment(step / 10d, false);
                float risk = BiomassCalculations.CombinedRefinementFailure(0.12f, adjustment);
                Assert.True(risk <= previous);
                previous = risk;
            }
        }
    }
}
