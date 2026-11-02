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
            Assert.Contains("Quaternion rotation = Orientation(rot)", source);
            Assert.Contains("MeshAt(rot)", source);
        }

        [Theory]
        [InlineData(0, 0f, -1f)]
        [InlineData(1, -1f, 0f)]
        [InlineData(2, 0f, 1f)]
        [InlineData(3, 1f, 0f)]
        public void ProcessingBasinAndOverlayShareBuildingOrientationEvenWhenGraphicDataDisablesRotation(
            int direction, float expectedX, float expectedZ)
        {
            var graphic = new Graphic_GeneVat { data = new GraphicData { drawRotated = false } };
            Assert.True(graphic.ShouldDrawRotated);
            var rotation = Graphic_GeneVat.Orientation(new Rot4(direction));
            // The basin's lower edge, animation forward axis and contents use this same transform.
            var lowerEdge = rotation * new UnityEngine.Vector3(0f, .02f, -1f);
            Assert.Equal(expectedX, lowerEdge.x, 5);
            Assert.Equal(expectedZ, lowerEdge.z, 5);
            Assert.Equal(.02f, lowerEdge.y, 5);
            var rightEdge = rotation * new UnityEngine.Vector3(1f, 0f, 0f);
            Assert.Equal(-expectedZ, rightEdge.x, 5);
            Assert.Equal(expectedX, rightEdge.z, 5);
        }

        private class OrientedVat : Thing
        {
            private readonly Graphic_GeneVat graphic = new Graphic_GeneVat {
                drawSize = new UnityEngine.Vector2(3f, 3.6f),
                data = new GraphicData { drawRotated = false }
            };
            public override Graphic Graphic => graphic;
            public override UnityEngine.Vector3 DrawPos => UnityEngine.Vector3.zero;
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void ActualMaterialProcessingCenterRotatesWithTheVat(int direction)
        {
            var vat = new OrientedVat { Rotation = Rot4.North };
            var north = MapComponent_CorpseLiquidationVisuals.MaterialContentsCenter(vat);
            vat.Rotation = new Rot4(direction);
            var actual = MapComponent_CorpseLiquidationVisuals.MaterialContentsCenter(vat);
            var expected = Graphic_GeneVat.Orientation(vat.Rotation) * north;
            Assert.Equal(expected.x, actual.x, 5);
            Assert.Equal(expected.y, actual.y, 5);
            Assert.Equal(expected.z, actual.z, 5);
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
