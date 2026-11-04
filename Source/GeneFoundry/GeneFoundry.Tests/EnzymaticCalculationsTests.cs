using GeneFoundry.Recipes;
using Xunit;

namespace GeneFoundry.Tests
{
    public class EnzymaticCalculationsTests
    {
        [Theory]
        [InlineData(-10f, 1)]
        [InlineData(0f, 1)]
        [InlineData(28f, 1)]
        [InlineData(50f, 1)]
        [InlineData(50.01f, 2)]
        [InlineData(100f, 2)]
        [InlineData(100.01f, 3)]
        [InlineData(560f, 12)]
        public void CatalystCostRoundsUpPerFiftyMaterialWithMinimumOne(float material, int expected)
        {
            Assert.Equal(expected, BiomassCalculations.EnzymaticCatalystCount(material));
        }

        [Theory]
        [InlineData(100f, 1f, 41)]
        [InlineData(100f, 0.5f, 20)]
        [InlineData(100f, 0.1f, 4)]
        [InlineData(28f, 1f, 11)]
        [InlineData(28f, 0.5f, 5)]
        [InlineData(28f, 0.1f, 1)]
        [InlineData(560f, 1f, 231)]
        [InlineData(560f, 0.5f, 115)]
        [InlineData(560f, 0.1f, 23)]
        [InlineData(0f, 0.1f, 1)]
        public void EnzymaticYieldAppliesDryMassAndSeventyFivePercentBeforeFinalRounding(float material, float decay, int expected)
        {
            Assert.Equal(expected, BiomassCalculations.CorpseYield(material, decay, enzymatic: true));
        }

        [Fact]
        public void EnzymesUseFixedRecoveryWithoutChangingCatalystCost()
        {
            Assert.Equal(231, BiomassCalculations.CorpseYield(560, 1, true));
            Assert.Equal(23, BiomassCalculations.CorpseYield(560, 0.1f, true));
            Assert.Equal(12, BiomassCalculations.EnzymaticCatalystCount(560));
        }

        [Theory]
        [InlineData(true, false, false, true)]
        [InlineData(false, false, false, false)]
        [InlineData(true, true, false, false)]
        [InlineData(true, false, true, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        public void OnlyOrganicNonMachineBodiesAreEligible(bool organicFlesh, bool mechanoid, bool drone, bool eligible)
        {
            Assert.Equal(eligible, BiomassCalculations.IsOrganicCorpse(organicFlesh, mechanoid, drone));
        }
    }
}
