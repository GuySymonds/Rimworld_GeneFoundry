using System.Collections.Generic;
using GeneFoundry.Research;
using Xunit;

namespace GeneFoundry.Tests
{
    public class GeneResearchCalculationsTests
    {
        [Theory]
        [InlineData(5, 0, 0, 1000)]
        [InlineData(5, 10, 0, 3000)]
        [InlineData(10, 20, 0, 5500)]
        [InlineData(5, 10, 3, 4500)]
        [InlineData(1, 0, 1, 1100)]
        [InlineData(0, 0, 0, 500)]
        [InlineData(-10, -20, -30, 500)]
        public void DefaultCostCombinesQuantityComplexityAndArchite(int count, long complexity,
            long archite, int expected)
        {
            Assert.Equal(expected, GeneResearchCalculations.CategoryCost(count, complexity, archite));
        }

        [Fact]
        public void EveryCostTermCanBeConfiguredIndependently()
        {
            Assert.Equal(10 + 3 * 20 + 5 * 30 + 2 * 40,
                GeneResearchCalculations.CategoryCost(3, 5, 2, 10, 20, 30, 40));
            Assert.Equal(1, GeneResearchCalculations.CategoryCost(50, 100, 20, 0, 0, 0, 0));
            Assert.Equal(1, GeneResearchCalculations.CategoryCost(50, 100, 20, -1, -2, -3, -4));
        }

        [Fact]
        public void LargeModListsCannotOverflowIntoCheapResearch()
        {
            Assert.Equal(int.MaxValue, GeneResearchCalculations.CategoryCost(
                int.MaxValue, long.MaxValue, long.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void AddingGenesComplexityOrArchiteAlwaysIncreasesDefaultCost()
        {
            int baseline = GeneResearchCalculations.CategoryCost(5, 10, 3);
            Assert.Equal(baseline + 100, GeneResearchCalculations.CategoryCost(6, 10, 3));
            Assert.Equal(baseline + 200, GeneResearchCalculations.CategoryCost(5, 11, 3));
            Assert.Equal(baseline + 500, GeneResearchCalculations.CategoryCost(5, 10, 4));
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
