using System.Collections.Generic;
using GeneFoundry.Components;
using UnityEngine;
using Verse;

namespace GeneFoundry.Recipes
{
    // Directional base artwork and matching directional animation canvases.
    public class Graphic_GeneVat : Graphic_StyledMachine
    {
        // Processing follows the building, independently of GraphicData.drawRotated.
        public override bool ShouldDrawRotated => false;
        internal static Quaternion Orientation(Rot4 rotation)
        {
            double halfAngle = rotation.AsAngle * System.Math.PI / 360d;
            return new Quaternion(0f, (float)System.Math.Sin(halfAngle), 0f, (float)System.Math.Cos(halfAngle));
        }

        internal const int TransitionQueue = 3000;
        internal const int ContentsQueue = 3001;
        internal const int BubblesQueue = 3002;
        private static readonly Dictionary<Material, Material> contentsMaterials = new Dictionary<Material, Material>();
        private Material[][] frames;
        private static readonly string[] Directions = { "north", "east", "south", "west" };
        internal static string FramePath(int frame, Rot4 rotation) =>
            $"GeneFoundry/Buildings/GeneVatFrames/GeneVat_Detailed_Transition_{frame:00}_{Directions[rotation.AsInt]}";

        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            frames = new Material[4][];
            for (int direction = 0; direction < 4; direction++)
            {
                if (UsesSimple(new Rot4(direction))) continue;
                var set = new Material[8];
                for (int i = 0; i < set.Length; i++)
                {
                    string framePath = FramePath(i, new Rot4(direction));
                    Texture2D texture = ContentFinder<Texture2D>.Get(framePath, false);
                    if (texture == null)
                    {
                        Log.ErrorOnce("GeneFoundry: missing directional transition " + framePath +
                            "; using the idle image for this direction.", 1745020 + direction);
                        set = null;
                        break;
                    }
                    set[i] = MaterialPool.MatFrom(new MaterialRequest(texture, ShaderDatabase.Transparent, req.color)
                    {
                        renderQueue = TransitionQueue
                    });
                }
                frames[direction] = set;
            }
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            Material[] set = frames?[rot.AsInt];
            if (set == null || thing == null) return;
            int frame = thing.TryGetComp<CompGeneVatAnimation>()?.FrameAt(Find.TickManager.TicksGame) ?? -1;
            if (frame < 0) return;
            // Directional canvases already include their orientation; match the base mesh exactly.
            Quaternion rotation = extraRotation == 0f ? Quaternion.identity : Quaternion.Euler(Vector3.up * extraRotation);
            Vector3 position = loc + DrawOffset(rot) + Vector3.up * .002f;
            GenDraw.DrawMeshNowOrLater(MeshAt(rot), position, rotation, set[frame], false);
        }

        // Preserve every shader property on native corpse/item materials, without
        // changing the shared originals or cloning them every drawing frame.
        internal static Material ContentsMaterial(Material source)
        {
            if (source == null) return null;
            if (!contentsMaterials.TryGetValue(source, out Material material))
            {
                material = new Material(source) { renderQueue = ContentsQueue };
                contentsMaterials.Add(source, material);
            }
            return material;
        }

        internal static void DrawIngredient(Thing ingredient, Vector3 center, Rot4 vatRotation)
        {
            Graphic graphic = ingredient.Graphic;
            if (graphic is Graphic_StackCount stack) graphic = stack.SubGraphicFor(ingredient);
            Rot4 rot = ingredient.Rotation;
            Quaternion orientation = Orientation(vatRotation);
            GenDraw.DrawMeshNowOrLater(graphic.MeshAt(rot), center + orientation * graphic.DrawOffset(rot),
                orientation * graphic.QuatFromRot(rot), ContentsMaterial(graphic.MatAt(rot, ingredient)), false);
        }

        public override Graphic GetColoredVersion(Shader shader, Color color, Color colorTwo)
            => GraphicDatabase.Get<Graphic_GeneVat>(path, shader, drawSize, color, colorTwo, data);
    }
}
