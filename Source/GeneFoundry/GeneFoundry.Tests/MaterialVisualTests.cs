#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Reflection;
using GeneFoundry.Components;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    public class MaterialVisualTests
    {
        [Fact]
        public void DespawningVatRestoresIngredientGraphicsWithoutMovingOrConsumingThem()
        {
            var vat = new CompGeneVatAnimation { parent = new ThingWithComps() };
            var food = new ThingWithComps();
            typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(food, new List<ThingComp>());
            food.stackCount = 50;
            IntVec3 position = food.Position;
            var visual = new CompGeneVatIngredientVisual { parent = food, owner = vat };
            food.AllComps.Add(visual);
            var tracked = (List<CompGeneVatIngredientVisual>)typeof(CompGeneVatAnimation)
                .GetField("ingredients", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(vat);
            tracked.Add(visual);
            Assert.False(visual.DontDrawParent());
            vat.PostDeSpawn(null);
            Assert.DoesNotContain(visual, food.AllComps);
            Assert.Empty(tracked);
            Assert.Equal(position, food.Position);
            Assert.Equal(50, food.stackCount);
        }
    }
}
#endif
