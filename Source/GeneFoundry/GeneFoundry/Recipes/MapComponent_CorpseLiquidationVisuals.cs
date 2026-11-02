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
        Thing LiquidationVat { get; }
        float LiquidationProgress { get; }
    }

    public class MapComponent_CorpseLiquidationVisuals : MapComponent
    {
        // Usable rectangle inside the basin's chamfered metal edges, measured
        // on the unchanged 1086x1448 north image: (110,590)..(980,1240).
        internal const float BasinWidthFraction = 870f / 1086f;
        internal const float BasinHeightFraction = 650f / 1448f;
        internal const float BasinCenterX = 545f / 1086f - .5f;
        internal const float BasinCenterZ = .5f - 915f / 1448f;
        internal const float BasinMargin = .025f;

        private sealed class Part
        {
            public Matrix4x4 matrix;
            public Mesh mesh;
            public Material material;
            public int stage;
        }
        private sealed class Presentation
        {
            public ICorpseLiquidationPresentation source;
            public int startedAt;
            public readonly List<Part> parts = new List<Part>();
            public Vector2 basinSize;
            public Vector3 basinOffset;
            public float top;
        }
        private readonly Dictionary<Corpse, Presentation> active = new Dictionary<Corpse, Presentation>();
        private readonly Dictionary<Corpse, ICorpseLiquidationPresentation> wanted = new Dictionary<Corpse, ICorpseLiquidationPresentation>();
        private readonly List<Corpse> retired = new List<Corpse>();
        private readonly List<PawnGraphicDrawRequest> requests = new List<PawnGraphicDrawRequest>();
        private static Material[] bubbleMaterials;
        private static bool bubblesLoaded;
        private static Mesh bubbleMesh;

        public MapComponent_CorpseLiquidationVisuals(Map map) : base(map) { }
        public override void MapComponentTick() => Synchronize(map.mapPawns.AllPawnsSpawned
            .Select(p => p.jobs?.curDriver).OfType<ICorpseLiquidationPresentation>(), Find.TickManager.TicksGame);
        internal void Synchronize(IEnumerable<ICorpseLiquidationPresentation> sources, int tick = 0)
        {
            wanted.Clear();
            foreach (var source in sources)
            {
                Corpse corpse = source.LiquidatingCorpse;
                if (corpse != null && IsVisibleCorpse(corpse)) wanted[corpse] = source;
            }
            retired.Clear();
            foreach (var corpse in active.Keys)
                if (!wanted.ContainsKey(corpse)) retired.Add(corpse);
            foreach (var corpse in retired) { RestoreDrawing(corpse); active.Remove(corpse); }
            foreach (var pair in wanted)
            {
                if (!active.TryGetValue(pair.Key, out Presentation state)) SuppressDrawing(pair.Key);
                if (state == null || state.source != pair.Value)
                    active[pair.Key] = new Presentation { source = pair.Value, startedAt = tick };
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
            wanted.Clear();
            retired.Clear();
        }
        public override void MapComponentDraw()
        {
            foreach (var pair in active)
            {
                if (!IsVisibleCorpse(pair.Key) || pair.Key.Position.Fogged(map)) continue;
                // Interruptions/power changes can happen while paused, before the next
                // synchronization tick. Draw normally until registration is restored.
                if (pair.Value.source.LiquidatingCorpse != pair.Key || pair.Value.source.LiquidationVat == null)
                    pair.Key.DrawNowAt(pair.Key.DrawPos);
                else DrawCorpse(pair.Key, pair.Value);
            }
        }
        internal int BubbleFrameFor(Corpse corpse, int tick) =>
            active.TryGetValue(corpse, out Presentation state) ? BiomassCalculations.BubbleMassFrame(tick - state.startedAt) : -1;
        private void DrawCorpse(Corpse corpse, Presentation state)
        {
            Thing vat = state.source.LiquidationVat;
            if (state.parts.Count == 0 && !PrepareCorpse(corpse, vat, state)) return;
            int stage = BiomassCalculations.LiquidationVisualStage((int)corpse.GetRotStage(), state.source.LiquidationProgress);
            Vector3 center = vat.DrawPos + vat.Graphic.DrawOffset(vat.Rotation) + state.basinOffset;
            center.y += .01f;
            Matrix4x4 translation = Matrix4x4.Translate(center);
            foreach (Part part in state.parts)
                if (part.stage == stage)
                    GenDraw.DrawMeshNowOrLater(part.mesh, translation * part.matrix, part.material, false);
            DrawBubbles(state.basinSize, state.top, state.startedAt, center);
        }
        private bool PrepareCorpse(Corpse corpse, Thing vat, Presentation state)
        {
            Pawn pawn = corpse.InnerPawn;
            var renderer = pawn.Drawer.renderer;
            var parms = PawnDrawParms.DefaultFor(pawn);
            parms.posture = pawn.GetPosture();
            parms.facing = renderer.LayingFacing();
            // Capture native orientation, life-stage and node offsets at natural size.
            // This pose and its fit remain fixed when the visual rot stage changes.
            parms.matrix = Matrix4x4.TRS(pawn.ageTracker.CurLifeStage.bodyDrawOffset,
                Quaternion.AngleAxis(renderer.BodyAngle(parms.flags), Vector3.up), Vector3.one);
            renderer.renderTree.EnsureInitialized(parms.flags);
            var bounds = new Bounds[3];
            var haveBounds = new bool[3];
            for (int stage = 0; stage < 3; stage++)
            {
                parms.rotDrawMode = stage == 2 ? RotDrawMode.Dessicated : stage == 1 ? RotDrawMode.Rotting : RotDrawMode.Fresh;
                requests.Clear();
                renderer.renderTree.rootNode?.AppendRequests(parms, requests);
                foreach (var request in requests)
                {
                    if (request.mesh == null || !renderer.renderTree.TryGetMatrix(request.node, parms, out Matrix4x4 matrix)) continue;
                    Graphic graphic = VisualGraphic(request.node, pawn, stage);
                    Material material = graphic?.MatAt(parms.facing, pawn) ?? request.material;
                    Mesh mesh = request.node is PawnRenderNode_AnimalPart && graphic != null
                        ? graphic.MeshAt(parms.facing) : request.mesh;
                    if (mesh == null || material == null) continue;
                    state.parts.Add(new Part { matrix = matrix, mesh = mesh, material = material, stage = stage });
                    EncapsulateMesh(ref bounds[stage], ref haveBounds[stage], mesh.bounds, matrix);
                }
            }
            if (state.parts.Count == 0) return false;
            Vector2 drawSize = vat.Graphic.drawSize;
            state.basinSize = new Vector2(drawSize.x * BasinWidthFraction - 2f * BasinMargin,
                drawSize.y * BasinHeightFraction - 2f * BasinMargin);
            state.basinOffset = new Vector3(drawSize.x * BasinCenterX, 0f, drawSize.y * BasinCenterZ);
            // Use one scale for every stage, even when skeleton artwork has different
            // dimensions. Centre each stage's native bounds on the same basin centre.
            float width = 0f, height = 0f;
            for (int stage = 0; stage < 3; stage++)
            {
                if (!haveBounds[stage]) continue;
                width = Mathf.Max(width, bounds[stage].size.x);
                height = Mathf.Max(height, bounds[stage].size.z);
                state.top = Mathf.Max(state.top, bounds[stage].max.y);
            }
            float scale = BiomassCalculations.LiquidationVisualScale(width, height,
                state.basinSize.x * 2f, state.basinSize.y * 2f);
            foreach (Part part in state.parts)
                part.matrix = FitCorpseMatrix(bounds[part.stage], scale, part.matrix);
            return true;
        }
        internal static Matrix4x4 FitCorpseMatrix(Bounds bounds, float scale, Matrix4x4 native)
            => Matrix4x4.Scale(new Vector3(scale, 1f, scale)) *
                Matrix4x4.Translate(new Vector3(-bounds.center.x, 0f, -bounds.center.z)) * native;
        // Transform every bounds corner, including orientation and node offsets.
        // Mesh bounds are conservative: no texture readbacks or biological-size guesses.
        internal static void EncapsulateMesh(ref Bounds bounds, ref bool initialized, Bounds mesh, Matrix4x4 matrix)
        {
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(new Vector3(
                    (corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                    (corner & 2) == 0 ? mesh.min.y : mesh.max.y,
                    (corner & 4) == 0 ? mesh.min.z : mesh.max.z));
                if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                else bounds.Encapsulate(point);
            }
        }
        internal static Vector3 MaterialContentsCenter(Thing vat)
        {
            Vector2 size = vat.Graphic.drawSize;
            float basinWidth = size.x * BasinWidthFraction - 2f * BasinMargin;
            float basinHeight = size.y * BasinHeightFraction - 2f * BasinMargin;
            float bubbleHeight = Mathf.Min(basinWidth, basinHeight * 500f / 203f) * 203f / 500f;
            return vat.DrawPos + vat.Graphic.DrawOffset(vat.Rotation) +
                new Vector3(size.x * BasinCenterX, .02f,
                    size.y * BasinCenterZ - (basinHeight - bubbleHeight) * .5f);
        }
        internal static void DrawMaterialBubbles(Thing vat, int startedAt)
        {
            Vector2 size = vat.Graphic.drawSize;
            Vector2 basin = new Vector2(size.x * BasinWidthFraction - 2f * BasinMargin,
                size.y * BasinHeightFraction - 2f * BasinMargin);
            Vector3 center = vat.DrawPos + vat.Graphic.DrawOffset(vat.Rotation) +
                new Vector3(size.x * BasinCenterX, .01f, size.y * BasinCenterZ);
            DrawBubbles(basin, .04f, startedAt, center);
        }
        private static void DrawBubbles(Vector2 basinSize, float top, int startedAt, Vector3 center)
        {
            if (!bubblesLoaded)
            {
                bubblesLoaded = true;
                bubbleMaterials = new Material[8];
                for (int i = 0; i < 8; i++)
                {
                    string path = "GeneFoundry/Effects/BubbleMass/" + i.ToString("00");
                    Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
                    if (texture == null)
                    {
                        Log.ErrorOnce("GeneFoundry: missing BubbleMass texture " + path + "; overlay disabled.", 1745040 + i);
                        bubbleMaterials = null;
                        return;
                    }
                    bubbleMaterials[i] = MaterialPool.MatFrom(new MaterialRequest(texture, ShaderDatabase.Transparent));
                }
                bubbleMesh = MeshPool.GridPlane(Vector2.one);
            }
            if (bubbleMaterials == null) return;
            // All supplied canvases are 500x203; fit without stretching or frame jitter.
            float width = Mathf.Min(basinSize.x, basinSize.y * 500f / 203f);
            // Anchor the overlay to the basin's lower interior edge.
            float height = width * 203f / 500f;
            center.z -= (basinSize.y - height) * .5f;
            center.y += top + .01f;
            int frame = BiomassCalculations.BubbleMassFrame(Find.TickManager.TicksGame - startedAt);
            GenDraw.DrawMeshNowOrLater(bubbleMesh,
                Matrix4x4.TRS(center, Quaternion.identity, new Vector3(width, 1f, height)),
                bubbleMaterials[frame], false);
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
