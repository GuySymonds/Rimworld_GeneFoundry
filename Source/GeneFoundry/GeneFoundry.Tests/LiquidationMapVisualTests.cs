#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GeneFoundry.Recipes;
using Verse;
using Xunit;
namespace GeneFoundry.Tests
{
    public class LiquidationMapVisualTests
    {
        private class Source : ICorpseLiquidationPresentation
        {
            public Corpse LiquidatingCorpse { get; set; }
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
