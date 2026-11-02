#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GeneFoundry.Recipes;
using Verse;
using UnityEngine;
using Xunit;
namespace GeneFoundry.Tests
{
    public class LiquidationMapVisualTests
    {
        private class Source : ICorpseLiquidationPresentation
        {
            public Corpse LiquidatingCorpse { get; set; }
            public Thing LiquidationVat { get; set; }
            public float LiquidationProgress { get; set; }
        }
        private class Presentation : MapComponent_CorpseLiquidationVisuals
        {
            public readonly HashSet<Corpse> hidden = new HashSet<Corpse>();
            public int starts, restores;
            public Presentation() : base(null) { }
            protected override bool IsVisibleCorpse(Corpse corpse) => corpse != null;
            protected override void SuppressDrawing(Corpse corpse) { hidden.Add(corpse); starts++; }
            protected override void RestoreDrawing(Corpse corpse) { hidden.Remove(corpse); restores++; }
        }
        [Theory]
        [InlineData(1f)] [InlineData(.25f)]
        public void NativeOrientationAndGraphicOffsetsAreMeasuredAndCentered(float scale)
        {
            // A 90-degree laying rotation plus a graphic offset. No map mutation.
            var native = new Matrix4x4 { m02 = 1, m11 = 1, m20 = -1, m33 = 1, m03 = 4, m23 = -2 };
            var mesh = new Bounds(new Vector3(1,0,2),new Vector3(4,0,2));
            var measured = default(Bounds);
            bool initialized = false;
            MapComponent_CorpseLiquidationVisuals.EncapsulateMesh(ref measured,ref initialized,mesh,native);
            Assert.True(initialized);
            Assert.Equal(2,measured.size.x,5);
            Assert.Equal(4,measured.size.z,5);
            Assert.Equal(6,measured.center.x,5);
            Assert.Equal(-3,measured.center.z,5);
            Matrix4x4 fitted = MapComponent_CorpseLiquidationVisuals.FitCorpseMatrix(measured,scale,native);
            var centered = default(Bounds);
            initialized = false;
            MapComponent_CorpseLiquidationVisuals.EncapsulateMesh(ref centered,ref initialized,mesh,fitted);
            Assert.Equal(0,centered.center.x,5);
            Assert.Equal(0,centered.center.z,5);
            Assert.Equal(2 * scale,centered.size.x,5);
            Assert.Equal(4 * scale,centered.size.z,5);
        }
        [Fact] public void InstalledWallLayerIsBelowVatAndVatRemainsBelowItemsAndPawns()
        {
            float vat = Altitudes.AltitudeFor(AltitudeLayer.BuildingOnTop);
            Assert.True(vat > Altitudes.AltitudeFor(AltitudeLayer.Building));
            Assert.True(vat < Altitudes.AltitudeFor(AltitudeLayer.Item));
            Assert.True(vat < Altitudes.AltitudeFor(AltitudeLayer.Pawn));
            Assert.InRange(MapComponent_CorpseLiquidationVisuals.BasinCenterZ,-.2f,0f);
            Assert.True(MapComponent_CorpseLiquidationVisuals.BasinWidthFraction < 1f);
            Assert.True(MapComponent_CorpseLiquidationVisuals.BasinHeightFraction < .5f);
        }
        [Fact] public void ActiveJobDrivesPresentationWithoutDependingOnBuildingClass()
        {
            var presentation = new Presentation();
            var corpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse));
            var source = new Source { LiquidatingCorpse = corpse };
            presentation.Synchronize(new[] { source });
            source.LiquidationProgress = .75f; presentation.Synchronize(new[] { source, source });
            Assert.Contains(corpse, presentation.hidden); Assert.Equal(1, presentation.starts);
            source.LiquidatingCorpse = null; presentation.Synchronize(new[] { source });
            Assert.Empty(presentation.hidden); Assert.Equal(1, presentation.restores);
            source.LiquidatingCorpse = corpse; presentation.Synchronize(new[] { source });
            Assert.Equal(2, presentation.starts); presentation.MapRemoved();
            Assert.Empty(presentation.hidden); Assert.Equal(2, presentation.restores);
        }
        [Fact] public void BubblePlaybackIsIndependentAndRemovedOnInterruptions()
        {
            var presentation = new Presentation();
            var first = new Source { LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)) };
            var second = new Source { LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)) };
            presentation.Synchronize(new[] { first }, 100);
            presentation.Synchronize(new[] { first, second }, 145);
            Assert.Equal(7, presentation.BubbleFrameFor(first.LiquidatingCorpse, 145));
            Assert.Equal(0, presentation.BubbleFrameFor(second.LiquidatingCorpse, 145));
            Assert.Equal(4, presentation.BubbleFrameFor(first.LiquidatingCorpse, 220));
            var corpse = first.LiquidatingCorpse;
            first.LiquidatingCorpse = null;
            presentation.Synchronize(new[] { first, second }, 220);
            Assert.Equal(-1, presentation.BubbleFrameFor(corpse, 220));
            first.LiquidatingCorpse = corpse;
            presentation.Synchronize(new[] { first, second }, 250);
            Assert.Equal(0, presentation.BubbleFrameFor(corpse, 250));
            Assert.Equal(1, presentation.BubbleFrameFor(second.LiquidatingCorpse, 250));
            presentation.MapRemoved();
            Assert.Equal(-1, presentation.BubbleFrameFor(corpse, 250));
        }
        [Fact] public void NewPresentationAfterLoadStartsWithNoVisualState()
        {
            var presentation = new Presentation();
            var source = new Source { LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)) };
            Assert.Equal(-1, presentation.BubbleFrameFor(source.LiquidatingCorpse, 1000));
            presentation.Synchronize(new[] { source }, 1000);
            Assert.Equal(0, presentation.BubbleFrameFor(source.LiquidatingCorpse, 1000));
        }
        [Fact] public void MultipleVatsRestoreTheirCorpseDrawingsIndependently()
        {
            var presentation = new Presentation();
            var first = new Source { LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)) };
            var second = new Source { LiquidatingCorpse = (Corpse)RuntimeHelpers.GetUninitializedObject(typeof(Corpse)) };
            presentation.Synchronize(new[] { first, second });
            presentation.Synchronize(new[] { second });
            Assert.DoesNotContain(first.LiquidatingCorpse, presentation.hidden);
            Assert.Contains(second.LiquidatingCorpse, presentation.hidden);
            presentation.MapRemoved(); Assert.Empty(presentation.hidden);
        }
    }
}
#endif
