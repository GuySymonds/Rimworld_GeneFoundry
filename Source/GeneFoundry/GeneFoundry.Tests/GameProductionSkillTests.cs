#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Reflection;
using GeneFoundry.Fabrication;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameProductionSkillTests
    {
        static GameProductionSkillTests() => InitializeSkills();
        internal static void InitializeSkills()
        {
            // DefOf initialization normally runs during game startup. Suppress only
            // its startup-order warning, whose Unity logger is unavailable headlessly.
            var binding = typeof(DefOfHelper).GetField("bindingNow", BindingFlags.Static | BindingFlags.NonPublic);
            bool previous = (bool)binding.GetValue(null);
            try
            {
                binding.SetValue(null, true);
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SkillDefOf).TypeHandle);
            }
            finally { binding.SetValue(null, previous); }
        }

        [Theory]
        [InlineData(2, false, false)]
        [InlineData(3, false, true)]
        [InlineData(20, true, false)]
        public void CookingQualificationRejectsDisabledSkillsAndEnforcesMinimum(int level, bool disabled, bool expected)
        {
            var previous = SkillDefOf.Cooking;
            try
            {
                SkillDefOf.Cooking = new SkillDef { defName = "Cooking" };
                var pawn = Worker(SkillDefOf.Cooking, level, disabled);
                Assert.Equal(expected, GeneVatSkills.Qualified(pawn));
            }
            finally { SkillDefOf.Cooking = previous; }
        }

        [Theory]
        [InlineData(9, false, false)]
        [InlineData(10, false, true)]
        [InlineData(20, true, false)]
        public void CraftingQualificationNeedsNoSecondarySkillRecords(int level, bool disabled, bool expected)
        {
            var previous = SkillDefOf.Crafting;
            try
            {
                SkillDefOf.Crafting = new SkillDef { defName = "Crafting" };
                var pawn = Worker(SkillDefOf.Crafting, level, disabled);
                Assert.Equal(expected, FabricationSkills.Qualified(pawn));
            }
            finally { SkillDefOf.Crafting = previous; }
        }

        private static Pawn Worker(SkillDef governing, int level, bool disabled)
        {
            var pawn = new Pawn();
            pawn.skills = new Pawn_SkillTracker(pawn);
            var skill = new SkillRecord(pawn, governing) { Level = level };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SkillRecord).GetField("cachedTotallyDisabled", flags).SetValue(skill, disabled ? BoolUnknown.True : BoolUnknown.False);
            typeof(SkillRecord).GetField("cachedPermanentlyDisabled", flags).SetValue(skill, BoolUnknown.False);
            typeof(SkillRecord).GetField("aptitudeCached", flags).SetValue(skill, (int?)0);
            pawn.skills.skills = new List<SkillRecord> { skill };
            return pawn;
        }
    }
}
#endif
