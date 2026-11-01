using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace GeneFoundry.Recipes
{
    public interface ICorpseLiquidationPresentation
    {
        Corpse LiquidatingCorpse { get; }
        float LiquidationProgress { get; }
    }

    public class MapComponent_CorpseLiquidationVisuals : MapComponent
    {
        private readonly Dictionary<Corpse, ICorpseLiquidationPresentation> active = new Dictionary<Corpse, ICorpseLiquidationPresentation>();
        private readonly List<PawnGraphicDrawRequest> requests = new List<PawnGraphicDrawRequest>();
        public MapComponent_CorpseLiquidationVisuals(Map map) : base(map) { }
        public override void MapComponentTick() => Synchronize(map.mapPawns.AllPawnsSpawned
            .Select(p => p.jobs?.curDriver).OfType<ICorpseLiquidationPresentation>());
        internal void Synchronize(IEnumerable<ICorpseLiquidationPresentation> sources)
        {
            var wanted = new Dictionary<Corpse, ICorpseLiquidationPresentation>();
            foreach (var source in sources)
                if (source.LiquidatingCorpse != null && IsVisibleCorpse(source.LiquidatingCorpse)) wanted[source.LiquidatingCorpse] = source;
            foreach (var corpse in active.Keys.ToList())
                if (!wanted.ContainsKey(corpse)) { RestoreDrawing(corpse); active.Remove(corpse); }
            foreach (var pair in wanted)
            {
                if (!active.ContainsKey(pair.Key)) SuppressDrawing(pair.Key);
                active[pair.Key] = pair.Value;
            }
        }
        protected virtual bool IsVisibleCorpse(Corpse corpse) => corpse != null && !corpse.Destroyed && corpse.Spawned && corpse.Map == map;
        protected virtual void SuppressDrawing(Corpse corpse) => map.dynamicDrawManager.DeRegisterDrawable(corpse);
        protected virtual void RestoreDrawing(Corpse corpse)
        {
            if (IsVisibleCorpse(corpse) && !map.dynamicDrawManager.DrawThings.Contains(corpse)) map.dynamicDrawManager.RegisterDrawable(corpse);
        }
        public override void MapRemoved()
        {
            foreach (var corpse in active.Keys) RestoreDrawing(corpse);
            active.Clear();
        }
        public override void MapComponentDraw()
        {
            foreach (var pair in active)
                if (IsVisibleCorpse(pair.Key) && !pair.Key.Position.Fogged(map)) DrawCorpse(pair.Key, pair.Value);
        }
        private void DrawCorpse(Corpse corpse, ICorpseLiquidationPresentation source)
        {
            if (corpse == null || corpse.Destroyed || !corpse.Spawned) return;
            bool active = source?.LiquidatingCorpse == corpse;
            float progress = active ? source.LiquidationProgress : 0;
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
