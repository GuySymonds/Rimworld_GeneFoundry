#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using GeneFoundry.Recipes;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameCorpseFilterTests
    {
        [Fact]
        public void OrganicDefinitionAllowListExcludesUnknownRobotsAndPreservesCatalyst()
        {
            var binding = typeof(DefOfHelper).GetField("bindingNow", BindingFlags.Static | BindingFlags.NonPublic);
            bool wasBinding = (bool)binding.GetValue(null);
            try
            {
                binding.SetValue(null, true);
                RuntimeHelpers.RunClassConstructor(typeof(FleshTypeDefOf).TypeHandle);
            }
            finally { binding.SetValue(null, wasBinding); }
            var flesh = new FleshTypeDef { defName = "ModdedOrganic", isOrganic = true };
            var machine = new FleshTypeDef { defName = "UnknownRobot", isOrganic = false };
            var organicCorpse = Def("OrganicCorpse");
            var robotCorpse = Def("RobotCorpse");
            var unknownCorpse = Def("UnknownCorpse");
            var medicine = Def("MedicineHerbal");
            var organic = Race(flesh, organicCorpse);
            var robot = Race(machine, robotCorpse);
            var recipe = new RecipeDef {
                ingredients = new List<IngredientCount> { new IngredientCount(), new IngredientCount() },
                fixedIngredientFilter = new ThingFilter(), defaultIngredientFilter = new ThingFilter()
            };
            foreach (var filter in new[] { recipe.ingredients[0].filter, recipe.ingredients[1].filter,
                recipe.fixedIngredientFilter, recipe.defaultIngredientFilter })
                typeof(ThingFilter).GetField("allowedDefs", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(filter, new HashSet<ThingDef>());
            recipe.ingredients[0].filter.SetAllow(robotCorpse, true);
            recipe.ingredients[0].filter.SetAllow(unknownCorpse, true);
            recipe.ingredients[1].filter.SetAllow(medicine, true);
            recipe.fixedIngredientFilter.SetAllow(robotCorpse, true);
            recipe.defaultIngredientFilter.SetAllow(robotCorpse, true);
            recipe.defaultIngredientFilter.SetAllow(organicCorpse, true);
            recipe.defaultIngredientFilter.SetAllow(medicine, true);

            CorpseBiomassUtility.ConfigureRecipeFilters(recipe, new[] { organic, robot, Def("MissingRace") });
            CorpseBiomassUtility.ConfigureRecipeFilters(recipe, new[] { organic, robot });
            Assert.True(recipe.ingredients[0].filter.Allows(organicCorpse));
            Assert.False(recipe.ingredients[0].filter.Allows(robotCorpse));
            Assert.False(recipe.ingredients[0].filter.Allows(unknownCorpse));
            Assert.False(recipe.ingredients[0].filter.Allows(medicine));
            Assert.True(recipe.fixedIngredientFilter.Allows(organicCorpse));
            Assert.True(recipe.fixedIngredientFilter.Allows(medicine));
            Assert.False(recipe.fixedIngredientFilter.Allows(robotCorpse));
            Assert.True(recipe.defaultIngredientFilter.Allows(organicCorpse));
            Assert.True(recipe.defaultIngredientFilter.Allows(medicine));
            Assert.False(recipe.defaultIngredientFilter.Allows(robotCorpse));
            Assert.False(CorpseBiomassUtility.CanProcess((RaceProperties)null));
        }

        private static ThingDef Def(string name)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name;
            def.virtualDefs = new List<ThingDef>();
            return def;
        }

        private static ThingDef Race(FleshTypeDef flesh, ThingDef corpse)
        {
            var race = new RaceProperties { corpseDef = corpse };
            typeof(RaceProperties).GetField("fleshType", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(race, flesh);
            var def = Def("Race_" + corpse.defName);
            def.race = race;
            return def;
        }
    }
}
#endif
