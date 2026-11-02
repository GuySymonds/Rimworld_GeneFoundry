#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Reflection;
using GeneFoundry.Components;
using GeneFoundry.Recipes;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    public class MaterialVisualTests
    {
        [Fact]
        public void TransparentTransitionsStayBelowContentsAndForegroundBubbles()
        {
            Assert.True(Graphic_GeneVat.TransitionQueue < Graphic_GeneVat.ContentsQueue);
            Assert.True(Graphic_GeneVat.ContentsQueue < Graphic_GeneVat.BubblesQueue);
            // The base graphic's material selection remains native and static.
            Assert.NotEqual(typeof(Graphic_GeneVat), typeof(Graphic_GeneVat).GetMethod("MatAt").DeclaringType);
        }

        [Fact]
        public void SharedGraphicAndAnimationUseEveryBuildingRotation()
        {
            Assert.Equal(typeof(Graphic_Single), typeof(Graphic_GeneVat).BaseType);
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory,
                "Fixtures/Source/Recipes/Graphic_GeneVat.cs"));
            Assert.DoesNotContain("rot != Rot4.North", source);
            Assert.Contains("Quaternion rotation = QuatFromRot(rot)", source);
            Assert.Contains("MeshAt(rot)", source);
        }

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
