using System.Linq;
using System.Collections.Generic;
using GeneFoundry.Jobs;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneFoundry.Recipes
{
    // Presentation only: the corpse stays spawned, selectable and biologically unchanged.
    // Replace its ordinary draw registration while extraction is active, then restore it.
    public class Building_GeneVat : Building_WorkTable
    {
        private Corpse visualCorpse;
        private JobDriver_ExtractCorpseBiomass visualDriver;
        private readonly List<PawnGraphicDrawRequest> requests = new List<PawnGraphicDrawRequest>();
        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) { RestoreCorpseDrawing(); return; }
            var driver = Map.mapPawns.AllPawnsSpawned.Select(p => p.jobs?.curDriver)
                .OfType<JobDriver_ExtractCorpseBiomass>().FirstOrDefault(d => d.job.targetA.Thing == this && d.LiquidatingCorpse != null);
            Corpse corpse = driver?.LiquidatingCorpse;
            if (corpse != visualCorpse)
            {
                RestoreCorpseDrawing();
                if (corpse != null && corpse.Spawned && corpse.Map == Map)
                {
                    visualCorpse = corpse;
                    Map.dynamicDrawManager.DeRegisterDrawable(corpse);
                }
            }
            visualDriver = driver;
        }
        private void RestoreCorpseDrawing()
        {
            if (visualCorpse != null && !visualCorpse.Destroyed && visualCorpse.Spawned &&
                !visualCorpse.Map.dynamicDrawManager.DrawThings.Contains(visualCorpse))
                visualCorpse.Map.dynamicDrawManager.RegisterDrawable(visualCorpse);
            visualCorpse = null; visualDriver = null;
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            RestoreCorpseDrawing();
            base.DeSpawn(mode);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            var corpse = visualCorpse;
            if (corpse == null || corpse.Destroyed || !corpse.Spawned || corpse.Map != Map) return;
            bool active = visualDriver?.LiquidatingCorpse == corpse;
            float progress = active ? visualDriver.LiquidationProgress : 0;
            float scale = BiomassCalculations.LiquidationVisualScale(progress);
            Pawn pawn = corpse.InnerPawn;
            var renderer = pawn.Drawer.renderer;
            var parms = PawnDrawParms.DefaultFor(pawn);
            parms.posture = pawn.GetPosture();
            parms.facing = renderer.LayingFacing();
            int stage = BiomassCalculations.LiquidationVisualStage((int)corpse.GetRotStage(), progress);
            parms.rotDrawMode = stage >= 2 ? RotDrawMode.Dessicated : stage == 1 ? RotDrawMode.Rotting : RotDrawMode.Fresh;
            Vector3 position = corpse.DrawPos + pawn.ageTracker.CurLifeStage.bodyDrawOffset * scale;
            parms.matrix = Matrix4x4.TRS(position, Quaternion.AngleAxis(renderer.BodyAngle(parms.flags), Vector3.up), Vector3.one * scale);
            renderer.renderTree.EnsureInitialized(parms.flags);
            requests.Clear();
            renderer.renderTree.rootNode?.AppendRequests(parms, requests);
            foreach (var request in requests)
            {
                if (!renderer.renderTree.TryGetMatrix(request.node, parms, out Matrix4x4 matrix)) continue;
                Graphic graphic = VisualGraphic(request.node, pawn, stage);
                Material material = graphic?.MatAt(parms.facing, pawn) ?? request.material;
                Mesh mesh = request.node is PawnRenderNode_AnimalPart && graphic != null ? graphic.MeshAt(parms.facing) : request.mesh;
                if (mesh != null && material != null) GenDraw.DrawMeshNowOrLater(mesh, matrix, material, false);
            }
        }
        private static Graphic VisualGraphic(PawnRenderNode node, Pawn pawn, int stage)
        {
            if (!(node is PawnRenderNode_AnimalPart) && !(node is PawnRenderNode_Body) && !(node is PawnRenderNode_Head)) return null;
            // GraphicFor reads the actual corpse rot stage. Select stage artwork explicitly
            // instead of changing CompRottable, even temporarily, or dirtying shared graphics.
            Graphic graphic = node.PrimaryGraphic;
            if (node is PawnRenderNode_AnimalPart)
            {
                var life = pawn.ageTracker.CurKindLifeStage;
                bool female = pawn.gender == Gender.Female;
                GraphicData data = stage >= 2 ? (female ? life.femaleDessicatedBodyGraphicData : null) ?? life.dessicatedBodyGraphicData :
                    stage == 1 ? (female ? life.femaleRottingGraphicData : null) ?? life.rottingGraphicData : null;
                if (data != null) graphic = data.GraphicColoredFor(pawn);
            }
            else if (stage >= 2 && node is PawnRenderNode_Body && pawn.story?.bodyType?.bodyDessicatedGraphicPath != null)
                graphic = GraphicDatabase.Get<Graphic_Multi>(pawn.story.bodyType.bodyDessicatedGraphicPath, ShaderDatabase.Cutout);
            else if (stage >= 2 && node is PawnRenderNode_Head)
                graphic = HeadTypeDefOf.Skull.GetGraphic(pawn, Color.white);
            if (stage == 1 && graphic != null && (node is PawnRenderNode_Body || node is PawnRenderNode_Head || node is PawnRenderNode_AnimalPart))
                graphic = graphic.GetColoredVersion(graphic.Shader, PawnRenderUtility.GetRottenColor(graphic.Color), PawnRenderUtility.GetRottenColor(graphic.ColorTwo));
            return graphic;
        }
    }
}
