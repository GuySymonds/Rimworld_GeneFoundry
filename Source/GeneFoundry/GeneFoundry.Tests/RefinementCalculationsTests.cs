using GeneFoundry.Recipes;
using Xunit;

namespace GeneFoundry.Tests
{
    public class RefinementCalculationsTests
    {
        [Theory]
        [InlineData(5, 3, 2, 0.30f)]
        [InlineData(10, 3, 2, 0.12f)]
        [InlineData(10, 8, 2, 0.045f)]
        [InlineData(10, 8, 6, 0f)]
        [InlineData(20, 20, 20, 0f)]
        public void ExpertRewardsPreserveExistingReliabilityThresholds(int intellectual, int crafting,
            int medicine, float expected)
        {
            Assert.Equal(expected, BiomassCalculations.RefinementFailureChance(intellectual, crafting, medicine), 5);
        }

        [Theory]
        [InlineData(5, 3, 2, 2500)]
        [InlineData(10, 8, 6, 1250)]
        [InlineData(20, 20, 20, 938)]
        [InlineData(30, 30, 30, 938)]
        public void DefaultPreparationRetainsBaselineAndAddsExpertSpeed(int intellectual, int crafting,
            int medicine, int ticks)
        {
            Assert.Equal(ticks, BiomassCalculations.RefinementPreparationTicks(
                intellectual, crafting, medicine, 60, 30));
        }

        [Theory]
        [InlineData(150, 1250)]
        [InlineData(200, 938)]
        [InlineData(300, 625)]
        public void ExpertSpeedSettingCanDisableOrIncreaseBonus(int speed, int ticks)
        {
            Assert.Equal(ticks, BiomassCalculations.RefinementPreparationTicks(20, 20, 20, 60, 30, speed));
            Assert.Equal(1250, BiomassCalculations.RefinementPreparationTicks(10, 8, 6, 60, 30, speed));
        }

        [Theory]
        [InlineData(10, 8, 6, 0d)]
        [InlineData(20, 8, 6, 0.60d)]
        [InlineData(10, 20, 6, 0.25d)]
        [InlineData(10, 8, 20, 0.15d)]
        [InlineData(15, 14, 13, 0.50d)]
        [InlineData(20, 20, 20, 1d)]
        public void ExpertSkillsUseExistingWeightsAndIndependentThresholds(int intellectual, int crafting,
            int medicine, double fraction)
        {
            Assert.Equal(fraction, BiomassCalculations.RefinementExpertProgress(intellectual, crafting, medicine), 6);
        }

        [Theory]
        [InlineData(0, 2d)]
        [InlineData(20, 2.4d)]
        [InlineData(50, 3d)]
        public void ExpectedYieldReflectsConfigurableExpertBonus(int bonus, double output)
        {
            Assert.Equal(output, BiomassCalculations.RefinementExpectedOutput(2, 20, 20, 20, bonus), 6);
            Assert.Equal(2d, BiomassCalculations.RefinementExpectedOutput(2, 10, 8, 6, bonus), 6);
        }

        [Theory]
        [InlineData(0d, 3)]
        [InlineData(0.3999d, 3)]
        [InlineData(0.4d, 2)]
        [InlineData(0.9999d, 2)]
        public void TwentyPercentBonusMeansFortyPercentChanceOfThirdItem(double roll, int output)
        {
            Assert.Equal(output, BiomassCalculations.RoundRefinementOutput(2.4d, roll));
        }

        [Fact]
        public void FractionalRoundingHasTheIntendedAverageAndNoGuaranteedInflation()
        {
            int total = 0;
            for (int index = 0; index < 10000; index++)
                total += BiomassCalculations.RoundRefinementOutput(2.4d, (index + 0.5d) / 10000d);
            Assert.Equal(24000, total);
            Assert.Equal(2, BiomassCalculations.RoundRefinementOutput(2d, 0d));
            Assert.Equal(3, BiomassCalculations.RoundRefinementOutput(3d, 0d));
        }
    }
}
