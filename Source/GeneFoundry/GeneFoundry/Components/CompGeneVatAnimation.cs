using System.Collections.Generic;
using GeneFoundry.Recipes;
using Verse;
using Verse.AI;
using RimWorld;

namespace GeneFoundry.Components
{
    public interface IGeneVatProcessing
    {
        bool ProcessingBiomass { get; }
    }

    public class CompProperties_GeneVatAnimation : CompProperties
    {
        public CompProperties_GeneVatAnimation() { compClass = typeof(CompGeneVatAnimation); }
    }

    // Transient presentation only: rebuilt by the work toil after loading a save.
    public class CompGeneVatAnimation : ThingComp
    {
        private Pawn worker;
        private JobDriver driver;
        private int startedAt;
        private readonly List<CompGeneVatIngredientVisual> ingredients = new List<CompGeneVatIngredientVisual>();

        public int FrameAt(int tick)
        {
            if (worker == null || worker.Destroyed || !worker.Spawned || worker.Map != parent.Map ||
                worker.Drafted || worker.jobs?.curDriver != driver ||
                !(driver is IGeneVatProcessing processing) || !processing.ProcessingBiomass ||
                !(parent is Building_WorkTable table) || !table.Spawned || !table.UsableForBillsAfterFueling() ||
                Graphic_StyledMachine.UsesSimple(parent))
                return -1;
            return GeneVatAnimationTiming.FrameAt(tick - startedAt);
        }

        public override void PostDraw()
        {
            // Corpse effects are drawn by the existing map presentation, once only.
            if (FrameAt(Find.TickManager.TicksGame) >= 0)
            {
                if (driver is ICorpseLiquidationPresentation) return;
                foreach (var ingredient in ingredients)
                    if (ingredient.Active && !ingredient.HideOnly)
                        Graphic_GeneVat.DrawIngredient(ingredient.parent,
                            MapComponent_CorpseLiquidationVisuals.MaterialContentsCenter(parent), parent.Rotation);
                MapComponent_CorpseLiquidationVisuals.DrawMaterialBubbles(parent, startedAt);
            }
            else RestoreIngredients();
        }

        public override void CompTick()
        {
            if (ingredients.Count > 0 && FrameAt(Find.TickManager.TicksGame) < 0) RestoreIngredients();
        }
        public override string CompInspectStringExtra() => parent.Spawned
            ? ProcessingEnvironment.Description(parent, false) : null;
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            RestoreIngredients();
            base.PostDeSpawn(map, mode);
        }
        private void RestoreIngredients()
        {
            foreach (var visual in ingredients)
            {
                visual.parent.AllComps.Remove(visual);
                if (visual.parent.Spawned)
                    visual.parent.Map.mapDrawer.MapMeshDirty(visual.parent.Position, MapMeshFlagDefOf.Things);
            }
            ingredients.Clear();
        }

        public void NotifyProcessing(Pawn pawn)
        {
            if (Graphic_StyledMachine.UsesSimple(parent))
            {
                RestoreIngredients();
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (worker != pawn || driver != pawn.jobs.curDriver || FrameAt(now) < 0)
                startedAt = now;
            if (driver != pawn.jobs.curDriver) RestoreIngredients();
            worker = pawn;
            driver = pawn.jobs.curDriver;
            if (ingredients.Count == 0)
                foreach (var entry in pawn.CurJob.placedThings ?? new List<ThingCountClass>())
                {
                    if (!(entry.thing is ThingWithComps food) || food.Destroyed || !food.Spawned || food.AllComps.Count == 0 ||
                        !ShouldTrackIngredient(food.def, driver is ICorpseLiquidationPresentation)) continue;
                    if (ingredients.Exists(x => x.parent == food)) continue;
                    var visual = new CompGeneVatIngredientVisual { parent = food, owner = this,
                        HideOnly = food.def.defName == "MedicineHerbal",
                        props = new CompProperties { compClass = typeof(CompGeneVatIngredientVisual) } };
                    food.AllComps.Add(visual);
                    ingredients.Add(visual);
                    food.Map.mapDrawer.MapMeshDirty(food.Position, MapMeshFlagDefOf.Things);
                }
        }

        internal static bool ShouldTrackIngredient(ThingDef def, bool corpseProcessing) =>
            def.defName == "MedicineHerbal" || (!corpseProcessing &&
                (def.IsMeat || def.IsWithinCategory(ThingCategoryDefOf.PlantFoodRaw)));
    }
    // Transient, per-stack drawing suppression; rebuilt after load, never a Def patch.
    public class CompGeneVatIngredientVisual : ThingComp
    {
        public CompGeneVatAnimation owner;
        public bool HideOnly;
        public bool Active => parent.Spawned && !parent.Destroyed && owner.parent.Spawned &&
            parent.Map == owner.parent.Map && owner.FrameAt(Find.TickManager.TicksGame) >= 0;
        public override bool DontDrawParent() => Active;
    }
}
