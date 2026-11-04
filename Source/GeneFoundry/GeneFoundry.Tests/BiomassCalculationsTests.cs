using System;
using GeneFoundry.Recipes;
using Xunit;

namespace GeneFoundry.Tests
{
    public class BiomassCalculationsTests
    {
        private static int Work(float material, float decay = 1f, float speed = 1f,
            float minutesPer50 = 24f, float setupMinutes = 2.4f, int handling = 30)
        {
            return BiomassCalculations.CorpseWorkTicks(material, speed, decay,
                minutesPer50, setupMinutes, handling);
        }

        [Theory]
        [InlineData(0,0,0,1f)]
        [InlineData(10,6,4,1f)]
        [InlineData(5,3,2,1f)]
        [InlineData(20,6,4,1.4f)]
        [InlineData(10,20,4,1.4f)]
        [InlineData(10,6,20,1.2f)]
        [InlineData(20,20,20,2f)]
        public void IndependentSpeedProgress(int c,int m,int i,float expected) =>
            Assert.Equal(expected, BiomassCalculations.ExtractionSkillSpeed(c,m,i),5);

        [Theory]
        [InlineData(150,1.5f)] [InlineData(200,2f)] [InlineData(300,3f)]
        public void MaximumSpeedSetting(int maximum,float expected)
        {
            Assert.Equal(expected,BiomassCalculations.ExtractionSkillSpeed(20,20,20,maximum),5);
            Assert.Equal(1f,BiomassCalculations.ExtractionSkillSpeed(10,6,4,maximum));
        }

        [Theory]
        [InlineData(5,6,4,0d,22)] [InlineData(5,6,4,1d,20)]
        [InlineData(10,6,4,0d,25)] [InlineData(10,6,4,1d,25)]
        [InlineData(20,20,20,0d,25)] [InlineData(20,20,20,1d,25)]
        [InlineData(5,20,20,1d,20)]
        [InlineData(0,0,0,0d,12)] [InlineData(0,0,0,1d,1)]
        public void OneRollWastageBoundaries(int c,int m,int i,double roll,int expected) =>
            Assert.Equal(expected,BiomassCalculations.ExtractionOutput(25,c,m,i,roll));

        [Fact]
        public void MinimumOutputAndVanillaCeilingMatchDesiredRecovery()
        {
            Assert.Equal(1,BiomassCalculations.ExtractionOutput(1,0,0,0,1));
            foreach (int baseline in new[] {7,9,20,25})
                for(int output=1;output<=baseline;output++)
                    Assert.Equal(output,(int)Math.Ceiling(baseline * BiomassCalculations.MaterialYieldEfficiency(output/(float)baseline)));
        }

        [Theory]
        [InlineData(2000f, 1f, 2000)]
        [InlineData(2000f, 2f, 1000)]
        [InlineData(2000f, 0.5f, 4000)]
        [InlineData(0f, 1f, 1)]
        [InlineData(-10f, 1f, 1)]
        public void RecoveryTimeUsesRemovalWorkAndMedicalSpeed(float work, float speed, int expected)
        {
            Assert.Equal(expected, BiomassCalculations.MedicalRecoveryTicks(work, speed));
        }

        [Theory]
        [InlineData(0.6f, 0.6f, 1f)]
        [InlineData(1.1f, 1.1f, 1f)]
        [InlineData(0.45f, 0.6f, 0.75f)]
        [InlineData(0.825f, 1.1f, 0.75f)]
        public void RemovingCleanlinessPreservesOtherEnvironmentalFactors(
            float environment, float cleanliness, float expected)
        {
            Assert.Equal(expected, BiomassCalculations.WithoutCleanliness(environment, cleanliness), 5);
        }

        [Theory]
        [InlineData(100f, 1f, 33)]
        [InlineData(100f, 0.5f, 16)]
        [InlineData(100f, 0.1f, 3)]
        [InlineData(28f, 1f, 9)]
        [InlineData(28f, 0.5f, 4)]
        [InlineData(28f, 0.1f, 1)]
        [InlineData(560f, 1f, 184)]
        [InlineData(560f, 0.5f, 92)]
        [InlineData(560f, 0.1f, 18)]
        public void YieldIncludesWholeBodyBonusAndDecay(float material, float decay, int expected)
        {
            Assert.Equal(expected, BiomassCalculations.CorpseYield(material, decay));
        }

        [Theory]
        [InlineData(0f, 1f)]
        [InlineData(0f, 0.1f)]
        [InlineData(0.1f, 0.1f)]
        [InlineData(1f, 1f)]
        [InlineData(-10f, 0.1f)]
        public void EveryEligibleCorpseYieldsAtLeastOne(float material, float decay)
        {
            Assert.Equal(1, BiomassCalculations.CorpseYield(material, decay));
        }

        [Fact]
        public void MissingMaterialStillReceivesWholeBodyBonus()
        {
            // An initially 100-material body has lost 20 material in missing parts.
            Assert.Equal(26, BiomassCalculations.CorpseYield(80f, 1f));
            Assert.Equal(33, BiomassCalculations.CorpseYield(80f, 1f, true));
            Assert.True(BiomassCalculations.CorpseYield(80f, 1f) > 80f * 0.25f * 0.60f);
        }

        [Theory]
        [InlineData(28f, 9, 11)]
        [InlineData(140f, 46, 57)]
        [InlineData(336f, 110, 138)]
        [InlineData(560f, 184, 231)]
        [InlineData(200f, 66, 82)]
        public void DryMassTargetsAreFlooredBeforeOperatorWastage(float meat, int basic, int enzymatic)
        {
            Assert.Equal(basic, BiomassCalculations.CorpseYield(meat, 1f));
            Assert.Equal(enzymatic, BiomassCalculations.CorpseYield(meat, 1f, true));
            Assert.Equal(basic, BiomassCalculations.ExtractionOutput(basic, 10, 6, 4, 1));
            Assert.Equal(enzymatic, BiomassCalculations.ExtractionOutput(enzymatic, 10, 6, 4, 1));
        }

        [Fact]
        public void DryMassOutputStillReceivesExistingSkillAndEnvironmentWastage()
        {
            int baseline = BiomassCalculations.CorpseYield(140f, 1f);
            Assert.Equal(41, BiomassCalculations.ExtractionOutput(baseline, 5, 6, 4, 0));
            Assert.Equal(36, BiomassCalculations.ExtractionOutput(baseline, 5, 6, 4, 1));
            Assert.Equal(41, BiomassCalculations.ExtractionOutput(baseline, 10, 6, 4, 0, .1f));
        }

        [Fact]
        public void ExistingCorpseTimingMeetsElephantTargetWithoutFlatteningMaterialOrSkillScaling()
        {
            int elephantTicks = (Work(560f) + 1) / 2; // Existing job halves configured work.
            Assert.InRange(elephantTicks, 1, 5000); // Two in-game hours.
            Assert.True((Work(28f) + 1) / 2 < elephantTicks);
            Assert.True((Work(140f) + 1) / 2 < elephantTicks);
            Assert.True((Work(560f, speed: 2f) + 1) / 2 < elephantTicks);
        }

        [Fact]
        public void GreenSkinDescriptionEstimatesUseUnchangedFabricationAndSequentialProduction()
        {
            Assert.Equal(10, Fabrication.FabricationCalculations.Biomass(new[] { 0 }));
            double fabricationMinutes = Fabrication.FabricationCalculations.Work(1, 0) /
                Fabrication.FabricationCalculations.Speed(10, 6, 4) * 60d / 2500d;
            double sharedMinutes = 90d + 360d + fabricationMinutes;
            Assert.Equal(580d, Math.Round(7 * 500d * 60d / 2500d + sharedMinutes));
            Assert.Equal(527d, Math.Round(((Work(140f) + 1) / 2) * 60d / 2500d + sharedMinutes));
            Assert.Equal(602d, Math.Round(((Work(560f) + 1) / 2) * 60d / 2500d + sharedMinutes));
        }

        [Theory]
        [InlineData(0.5f, 0.65)]
        [InlineData(0.1f, 0.37)]
        public void DefaultHandlingRetainsThirtyPercentOfFreshWork(float decay, double fraction)
        {
            Assert.InRange(Work(560f, decay), Work(560f) * fraction - 1d,
                Work(560f) * fraction + 1d);
        }

        [Theory]
        [InlineData(0.1f)]
        [InlineData(0.5f)]
        [InlineData(1f)]
        public void HandlingOnlySettingMakesTimeIndependentOfDecay(float decay)
        {
            Assert.Equal(Work(560f, handling: 100), Work(560f, decay, handling: 100));
        }

        [Theory]
        [InlineData(0.1f)]
        [InlineData(0.5f)]
        public void NoHandlingSettingMakesTimeProportionalToRecovery(float decay)
        {
            Assert.InRange(Work(560f, decay, handling: 0),
                Work(560f, handling: 0) * decay - 1d,
                Work(560f, handling: 0) * decay + 1d);
        }

        [Fact]
        public void LargeDesiccatedBodyTakesLongerThanFreshBodyWithEqualYield()
        {
            Assert.Equal(BiomassCalculations.CorpseYield(560f, 0.1f),
                BiomassCalculations.CorpseYield(56f, 1f));
            Assert.True(Work(560f, 0.1f) > Work(56f));
        }

        [Fact]
        public void LargeBodiesHaveBetterFreshThroughputThanSmallBodies()
        {
            double smallRate = BiomassCalculations.CorpseYield(28f, 1f) / (double)Work(28f);
            double largeRate = BiomassCalculations.CorpseYield(560f, 1f) / (double)Work(560f);
            Assert.True(largeRate > smallRate);
        }

        [Theory]
        [InlineData(10f)]
        [InlineData(50f)]
        [InlineData(560f)]
        [InlineData(10000f)]
        public void BatchEfficiencyNeverExceedsTwentyFivePercentWorkReduction(float material)
        {
            double unscaledTicks = (2.4d + material / 50d * 24d) * 2500d / 60d;
            Assert.InRange(Work(material), Math.Ceiling(unscaledTicks * 0.75d) - 1d,
                Math.Ceiling(unscaledTicks) + 1d);
        }

        [Fact]
        public void ReferenceAmountUsesConfiguredMinutes()
        {
            Assert.Equal(1000, Work(50f, minutesPer50: 24f, setupMinutes: 0f));
            Assert.Equal(500, Work(50f, minutesPer50: 12f, setupMinutes: 0f));
            Assert.Equal(1125, Work(50f, minutesPer50: 24f, setupMinutes: 3f));
        }

        [Fact]
        public void DoubleWorkSpeedHalvesProcessingTime()
        {
            Assert.InRange(2 * Work(560f, speed: 2f) - Work(560f), 0, 1);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        public void NonPositiveWorkSpeedCannotProduceInvalidDuration(float speed)
        {
            Assert.True(Work(50f, speed: speed) > 0);
        }

        [Fact]
        public void ZeroMaterialAndSetupStillRequireOneTick()
        {
            Assert.Equal(1, Work(0f, setupMinutes: 0f));
        }
    }
}
