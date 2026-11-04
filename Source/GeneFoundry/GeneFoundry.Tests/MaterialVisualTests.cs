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
            Assert.Equal(typeof(Graphic_Multi), typeof(Graphic_GeneVat).BaseType);
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory,
                "Fixtures/Source/Recipes/Graphic_GeneVat.cs"));
            Assert.DoesNotContain("rot != Rot4.North", source);
            Assert.Contains("Material[] set = frames?[rot.AsInt]", source);
            Assert.Contains("GenDraw.DrawMeshNowOrLater(MeshAt(rot)", source);
            Assert.DoesNotContain("MakeVialMesh", source);
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
            Assert.False(graphic.ShouldDrawRotated);
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
        public void ActualMaterialProcessingCenterFitsEachDirectionalBasin(int direction)
        {
            var vat = new OrientedVat { Rotation = new Rot4(direction) };
            var actual = MapComponent_CorpseLiquidationVisuals.MaterialContentsCenter(vat);
            var basin = MapComponent_CorpseLiquidationVisuals.BasinBounds(vat.Rotation, vat.Graphic.drawSize);
            Assert.True(basin.Contains(new UnityEngine.Vector2(actual.x, actual.z)));
            Assert.Equal(.02f, actual.y, 5);
            switch (direction)
            {
                case 0: Assert.True(basin.center.y < 0f); break;
                case 1: Assert.True(basin.center.x < 0f); break;
                case 2: Assert.True(basin.center.y > 0f); break;
                case 3: Assert.True(basin.center.x > 0f); break;
            }
        }

        [Theory]
        [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
        public void BubbleOverlayFitsInsideEachMeasuredBasinWithoutStretching(int direction)
        {
            var rot = new Rot4(direction);
            var size = new UnityEngine.Vector2(3f, 3.6f);
            var basin = MapComponent_CorpseLiquidationVisuals.BasinBounds(rot, size);
            var bubbles = MapComponent_CorpseLiquidationVisuals.BubbleBounds(rot, size);
            Assert.True(bubbles.xMin >= basin.xMin - .00001f && bubbles.xMax <= basin.xMax + .00001f);
            Assert.True(bubbles.yMin >= basin.yMin - .00001f && bubbles.yMax <= basin.yMax + .00001f);
            Assert.True(bubbles.width > 0f && bubbles.height > 0f);
            Assert.Equal(500f / 203f, rot.IsHorizontal ? bubbles.height / bubbles.width : bubbles.width / bubbles.height, 4);
            // The foreground edge follows the operator, not a baked texture centre.
            switch (direction)
            {
                case 0: Assert.Equal(basin.yMin + .06f, bubbles.yMin, 5); break;
                case 1: Assert.Equal(basin.xMin + .06f, bubbles.xMin, 5); break;
                case 2: Assert.Equal(basin.yMax - .06f, bubbles.yMax, 5); break;
                case 3: Assert.Equal(basin.xMax - .06f, bubbles.xMax, 5); break;
            }
        }

        [Fact]
        public void HerbalMedicineIsTrackedForBothKindsOfProcessing()
        {
            var herb = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            herb.defName = "MedicineHerbal";
            Assert.True(CompGeneVatAnimation.ShouldTrackIngredient(herb, false));
            Assert.True(CompGeneVatAnimation.ShouldTrackIngredient(herb, true));
        }

        [Fact]
        public void MaterialAnimationHookMatchesInstalledRecipeWorkToil()
        {
            // If the vanilla work toil changes name, the processing notification
            // would never run and the vat would remain on its idle graphic.
            var toil = Verse.AI.Toils_Recipe.DoRecipeWork();
            Assert.Equal(nameof(Verse.AI.Toils_Recipe.DoRecipeWork), toil.debugName);
        }

        [Theory]
        [InlineData(0, "north")] [InlineData(1, "east")]
        [InlineData(2, "south")] [InlineData(3, "west")]
        public void FrameSelectionKeepsTheSameClockButUsesTheDirection(int direction, string suffix)
        {
            for (int frame = 0; frame < 8; frame++)
                Assert.Equal($"GeneFoundry/Buildings/GeneVatFrames/vat_{frame:00}_{suffix}",
                    Graphic_GeneVat.FramePath(frame, new Rot4(direction)));
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
