using UnityEngine;
using Verse;

namespace GeneFoundry.Components
{
    // Keep vanilla directional meshes/material caching. Only replace an available
    // Simple direction; all Detailed fallback materials and geometry stay native.
    public abstract class Graphic_StyledMachine : Graphic_Multi
    {
        private readonly Material[] simpleMaterials = new Material[4];
        public override Material MatNorth => simpleMaterials[0] ?? base.MatNorth;
        public override Material MatEast => simpleMaterials[1] ?? base.MatEast;
        public override Material MatSouth => simpleMaterials[2] ?? base.MatSouth;
        public override Material MatWest => simpleMaterials[3] ?? base.MatWest;
        private static readonly string[] Directions = { "north", "east", "south", "west" };
        public override bool ShouldDrawRotated => false;

        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            ApplySimpleGraphics(req, GeneFoundryMod.UseDetailedGraphics);
        }

        internal void ApplySimpleGraphics(GraphicRequest req, bool useDetailedGraphics)
        {
            for (int direction = 0; direction < 4; direction++)
            {
                simpleMaterials[direction] = null;
                if (useDetailedGraphics) continue;
                string simplePath = req.path.Substring(0, req.path.Length - "Detailed".Length) +
                    "Simple_" + Directions[direction];
                Material material = LoadSimpleMaterial(req, simplePath);
                if (ReferenceEquals(material, null)) continue;
                simpleMaterials[direction] = material;
            }
        }

        protected virtual Material LoadSimpleMaterial(GraphicRequest req, string simplePath)
        {
            // Optional artwork must not report missing-texture errors.
            if (ContentFinder<Texture2D>.Get(simplePath, false) == null) return null;
            return GraphicDatabase.Get<Graphic_Single>(simplePath, req.shader, req.drawSize,
                req.color, req.colorTwo).MatSingle;
        }

        internal bool UsesOnlySimpleGraphics => UsesSimple(Rot4.North) && UsesSimple(Rot4.East) &&
            UsesSimple(Rot4.South) && UsesSimple(Rot4.West);
        internal bool UsesSimple(Rot4 direction) => !ReferenceEquals(simpleMaterials[direction.AsInt], null);
        internal static bool UsesSimple(Thing thing) => thing != null &&
            thing.Graphic is Graphic_StyledMachine graphic && graphic.UsesSimple(thing.Rotation);
    }

    public class Graphic_BiomassHopper : Graphic_StyledMachine
    {
        public override Graphic GetColoredVersion(Shader shader, Color color, Color colorTwo)
            => GraphicDatabase.Get<Graphic_BiomassHopper>(path, shader, drawSize, color, colorTwo, data);
    }

    public class Graphic_BiomassRefinery : Graphic_StyledMachine
    {
        public override Graphic GetColoredVersion(Shader shader, Color color, Color colorTwo)
            => GraphicDatabase.Get<Graphic_BiomassRefinery>(path, shader, drawSize, color, colorTwo, data);
    }
}
