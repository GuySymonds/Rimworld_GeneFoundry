using System.Collections.Generic;
using GeneFoundry.Research;
using Xunit;

namespace GeneFoundry.Tests
{
    public class GeneResearchCalculationsTests
    {
        [Theory]
        [InlineData(5, 0, 0, 1000)]
        [InlineData(5, 10, 0, 1000)]
        [InlineData(10, 20, 0, 1375)]
        [InlineData(5, 10, 3, 1125)]
        [InlineData(1, 0, 1, 1000)]
        [InlineData(0, 0, 0, 1000)]
        [InlineData(-10, -20, -30, 1000)]
        [InlineData(21, 21, 0, 1700)]
        public void DefaultCostCombinesQuantityComplexityAndArchite(int count, long complexity,
            long archite, int expected)
        {
            Assert.Equal(expected, GeneResearchCalculations.CategoryCost(count, complexity, archite));
        }

        [Fact]
        public void EveryCostTermCanBeConfiguredIndependently()
        {
            Assert.Equal(10 + 3 * 20 + 5 * 30 + 2 * 40,
                GeneResearchCalculations.CategoryCost(3, 5, 2, 10, 20, 30, 40, 100, 4000, 1));
            Assert.Equal(1000, GeneResearchCalculations.CategoryCost(50, 100, 20, 0, 0, 0, 0));
            Assert.Equal(1000, GeneResearchCalculations.CategoryCost(50, 100, 20, -1, -2, -3, -4));
        }

        [Fact]
        public void LargeModListsCannotOverflowIntoCheapResearch()
        {
            Assert.Equal(4000, GeneResearchCalculations.CategoryCost(
                int.MaxValue, long.MaxValue, long.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
            Assert.Equal(int.MaxValue, GeneResearchCalculations.CategoryCost(
                int.MaxValue, long.MaxValue, long.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue,
                int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void AddingGenesComplexityOrArchiteIncreasesCostBelowTheCap()
        {
            int baseline = GeneResearchCalculations.CategoryCost(5, 10, 3);
            Assert.Equal(baseline + 25, GeneResearchCalculations.CategoryCost(6, 10, 3));
            Assert.Equal(baseline + 50, GeneResearchCalculations.CategoryCost(5, 11, 3));
            Assert.Equal(baseline + 125, GeneResearchCalculations.CategoryCost(5, 10, 4));
        }

        [Theory]
        [InlineData(15999, 25, 4000, 4000)]
        [InlineData(16000, 25, 4000, 4000)]
        [InlineData(16001, 25, 4000, 4000)]
        [InlineData(19700, 25, 4000, 4000)]
        [InlineData(19700, 25, 6000, 4925)]
        [InlineData(6800, 50, 4000, 3400)]
        [InlineData(6800, 100, 10000, 6800)]
        [InlineData(1, 1, 4000, 1)]
        [InlineData(100, 0, 4000, 1)]
        [InlineData(100, 100, 0, 1)]
        public void PercentageRoundingAndMaximumApplyInOrder(int raw, int percent, int maximum, int expected)
        {
            Assert.Equal(expected, GeneResearchCalculations.CategoryCost(0, 0, 0,
                raw, 0, 0, 0, percent, maximum, 1));
        }

        [Theory]
        [InlineData(3999, 4000, 1000, 1000)]
        [InlineData(4000, 4000, 1000, 1000)]
        [InlineData(4001, 4000, 1000, 1001)]
        [InlineData(0, 4000, 1500, 1500)]
        [InlineData(100000, 500, 1000, 1000)]
        [InlineData(100, 0, 0, 1)]
        public void MinimumIsAppliedAfterScalingAndConflictingMaximumIsRaised(
            int raw, int maximum, int minimum, int expected)
        {
            Assert.Equal(expected, GeneResearchCalculations.CategoryCost(0, 0, 0,
                raw, 0, 0, 0, 25, maximum, minimum));
        }

        public static IEnumerable<object[]> UnlockCases()
        {
            foreach (bool foundation in new[] { false, true })
            foreach (bool category in new[] { false, true })
            foreach (bool archogenetics in new[] { false, true })
            {
                yield return new object[] { foundation, category, 0, archogenetics, foundation && category };
                yield return new object[] { foundation, category, 1, archogenetics, foundation && category && archogenetics };
            }
        }

        [Theory]
        [MemberData(nameof(UnlockCases))]
        public void ArchiteAlwaysRequiresVanillaArchogeneticsAndBothFabricationProjects(
            bool foundation, bool category, int archite, bool archogenetics, bool expected)
        {
            Assert.Equal(expected, GeneResearchCalculations.IsUnlocked(foundation, category, archite, archogenetics));
        }
    }
}
