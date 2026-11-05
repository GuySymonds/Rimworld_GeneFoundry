using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Components;
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
        // Conservative interior rectangles measured in each final directional canvas.
        // Coordinates are pixels from the top-left; returned bounds use map X/Z offsets.
        internal static Rect BasinBounds(Rot4 direction, Vector2 drawSize)
        {
            bool horizontal = direction.IsHorizontal;
            float imageWidth = horizontal ? 1448f : 1086f;
            float imageHeight = horizontal ? 1086f : 1448f;
            float width = horizontal ? drawSize.y : drawSize.x;
            float height = horizontal ? drawSize.x : drawSize.y;
            Rect pixels;
            switch (direction.AsInt)
            {
                case 1: pixels = Rect.MinMaxRect(170f, 210f, 910f, 820f); break;
                case 2: pixels = Rect.MinMaxRect(170f, 210f, 920f, 800f); break;
                case 3: pixels = Rect.MinMaxRect(540f, 210f, 1290f, 820f); break;
                default: pixels = Rect.MinMaxRect(160f, 600f, 920f, 1190f); break;
            }
            return Rect.MinMaxRect((pixels.xMin / imageWidth - .5f) * width,
                (.5f - pixels.yMax / imageHeight) * height,
                (pixels.xMax / imageWidth - .5f) * width,
                (.5f - pixels.yMin / imageHeight) * height);
        }
        internal static Rect BubbleBounds(Rot4 direction, Vector2 drawSize)
        {
            // Place one foreground band at the basin edge nearest the interaction cell.
            // The directional transition's bubbles stay behind the animal; this
            // separate foreground band sits above it, closer to the operator.
            bool horizontal = direction.IsHorizontal;
            Rect basin = BasinBounds(direction, drawSize);
            const float inset = .06f;
            float width = Mathf.Min((horizontal ? basin.height : basin.width) - inset * 2f,
                ((horizontal ? basin.width : basin.height) - inset * 2f) * 500f / 203f);
            float height = width * 203f / 500f;
            Vector2 size = horizontal ? new Vector2(height, width) : new Vector2(width, height);
            Vector2 center = basin.center;
            switch (direction.AsInt)
            {
                case 1: center.x = basin.xMin + inset + size.x * .5f; break;
                case 2: center.y = basin.yMax - inset - size.y * .5f; break;
                case 3: center.x = basin.xMax - inset - size.x * .5f; break;
                default: center.y = basin.yMin + inset + size.y * .5f; break;
            }
            return new Rect(center - size * .5f, size);
        }
        private static Vector2 BasinSize(Thing vat)
        {
            Rect bounds = BasinBounds(vat.Rotation, vat.Graphic.drawSize);
            return vat.Rotation.IsHorizontal ? new Vector2(bounds.height, bounds.width) : bounds.size;
        }
        private static Vector3 BasinCenter(Thing vat)
        {
            Vector2 center = BasinBounds(vat.Rotation, vat.Graphic.drawSize).center;
            return vat.DrawPos + vat.Graphic.DrawOffset(vat.Rotation) + new Vector3(center.x, 0f, center.y);
        }

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
            public float top;
        }
        private readonly Dictionary<Corpse, Presentation> active = new Dictionary<Corpse, Presentation>();
        private readonly Dictionary<Corpse, ICorpseLiquidationPresentation> wanted = new Dictionary<Corpse, ICorpseLiquidationPresentation>();
        private readonly List<Corpse> retired = new List<Corpse>();
        private readonly List<PawnGraphicDrawRequest> requests = new List<PawnGraphicDrawRequest>();
        private static Material[] bubbleMaterials;
        private static bool bubblesLoaded;
        private static Mesh bubbleMesh;

        // Graphics are fixed for this session. Recompute after loading each map;
        // this is transient presentation state, never part of the save contract.
        private bool? detailedPresentationRequired;
        protected virtual Graphic VatGraphic =>
            DefDatabase<ThingDef>.GetNamedSilentFail("GF_GeneVat")?.graphicData?.Graphic;

        public MapComponent_CorpseLiquidationVisuals(Map map) : base(map) { }
        public override void MapComponentTick()
        {
            if (!detailedPresentationRequired.HasValue)
                detailedPresentationRequired = !(VatGraphic is Graphic_StyledMachine graphic) ||
                    !graphic.UsesOnlySimpleGraphics;
            if (!detailedPresentationRequired.Value) return;
            ScanProcessingPawns();
        }
        protected virtual void ScanProcessingPawns() => Synchronize(map.mapPawns.AllPawnsSpawned
            .Select(p => p.jobs?.curDriver).OfType<ICorpseLiquidationPresentation>(), Find.TickManager.TicksGame);
        internal void Synchronize(IEnumerable<ICorpseLiquidationPresentation> sources, int tick = 0)
        {
            wanted.Clear();
            foreach (var source in sources)
            {
                Corpse corpse = source.LiquidatingCorpse;
                if (corpse != null && IsVisibleCorpse(corpse) &&
                    !Graphic_StyledMachine.UsesSimple(source.LiquidationVat)) wanted[corpse] = source;
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
                if (pair.Value.source.LiquidatingCorpse != pair.Key || pair.Value.source.LiquidationVat == null ||
                    Graphic_StyledMachine.UsesSimple(pair.Value.source.LiquidationVat))
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
            Quaternion rotation = Graphic_GeneVat.Orientation(vat.Rotation);
            Vector3 center = BasinCenter(vat);
            center.y += .01f;
            Matrix4x4 translation = Matrix4x4.TRS(center, rotation, Vector3.one);
            foreach (Part part in state.parts)
                if (part.stage == stage)
                    GenDraw.DrawMeshNowOrLater(part.mesh, translation * part.matrix, Graphic_GeneVat.ContentsMaterial(part.material), false);
            DrawBubbles(vat, state.top, state.startedAt);
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
                    // Keep the renderer's life-stage/body-size mesh. Replacing it with
                    // Graphic.MeshAt can shrink animals before fitting even begins.
                    Mesh mesh = request.mesh;
                    if (mesh == null || material == null) continue;
                    state.parts.Add(new Part { matrix = matrix, mesh = mesh, material = material, stage = stage });
                    EncapsulateMesh(ref bounds[stage], ref haveBounds[stage], mesh.bounds, matrix);
                }
            }
            if (state.parts.Count == 0) return false;
            state.basinSize = BasinSize(vat);
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
                state.basinSize.x, state.basinSize.y);
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
            Vector2 center = BubbleBounds(vat.Rotation, vat.Graphic.drawSize).center;
            return vat.DrawPos + vat.Graphic.DrawOffset(vat.Rotation) + new Vector3(center.x, .02f, center.y) +
                Graphic_GeneVat.Orientation(vat.Rotation) * new Vector3(0f, 0f, .25f);
        }
        internal static void DrawMaterialBubbles(Thing vat, int startedAt) => DrawBubbles(vat, .04f, startedAt);
        private static void DrawBubbles(Thing vat, float top, int startedAt)
        {
            if (!bubblesLoaded)
            {
                bubblesLoaded = true;
                bubbleMaterials = new Material[8];
                for (int i = 0; i < 8; i++)
                {
                    string path = "GeneFoundry/Effects/BubbleMass/BubbleMass_" + i.ToString("00");
                    Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
                    if (texture == null)
                    {
                        Log.ErrorOnce("GeneFoundry: missing BubbleMass texture " + path + "; overlay disabled.", 1745040 + i);
                        bubbleMaterials = null;
                        return;
                    }
                    bubbleMaterials[i] = MaterialPool.MatFrom(new MaterialRequest(texture, ShaderDatabase.Transparent) { renderQueue = Graphic_GeneVat.BubblesQueue });
                }
                bubbleMesh = MeshPool.GridPlane(Vector2.one);
            }
            if (bubbleMaterials == null) return;
            Rect bounds = BubbleBounds(vat.Rotation, vat.Graphic.drawSize);
            float width = vat.Rotation.IsHorizontal ? bounds.height : bounds.width;
            float height = vat.Rotation.IsHorizontal ? bounds.width : bounds.height;
            Vector3 center = vat.DrawPos + vat.Graphic.DrawOffset(vat.Rotation) +
                new Vector3(bounds.center.x, top + .01f, bounds.center.y);
            Quaternion rotation = Graphic_GeneVat.Orientation(vat.Rotation);
            int frame = BiomassCalculations.BubbleMassFrame(Find.TickManager.TicksGame - startedAt);
            GenDraw.DrawMeshNowOrLater(bubbleMesh,
                Matrix4x4.TRS(center, rotation, new Vector3(width, 1f, height)),
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
