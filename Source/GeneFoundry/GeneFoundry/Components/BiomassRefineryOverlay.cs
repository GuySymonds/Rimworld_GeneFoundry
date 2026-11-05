using UnityEngine;
using Verse;

namespace GeneFoundry.Components
{
    internal static class BiomassRefineryOverlay
    {
        private const string TextureRoot = "GeneFoundry/Buildings/BiomassRefineryFrames/";

        // EDIT THESE FOUR QUADS. Coordinates are (map X, map Z), in tiles relative
        // to parent.DrawPos. +X is right; +Z is up the map. Order: TL, TR, BL, BR.
        // Move all corners equally to position; spread them to scale; move individual
        // corners to skew/warp. These are initial fits to the supplied chassis art.
        // All 26 materials share these four meshes; artwork is never rotated again.
        private static readonly Vector2[][] VatOverlayTransforms =
        {
            // North
            new[] { new Vector2(-.40f, .95f), new Vector2(1.25f, .95f), new Vector2(-.40f, -.8f), new Vector2(1.25f, -.8f) },
            // East — right-hand refinery
            new[] { new Vector2(-.82f, .5f), new Vector2(.83f, .5f), new Vector2(-.82f, -1.19f), new Vector2(.83f, -1.19f) },
            // South — bottom refinery
            new[] { new Vector2(-1.22f, .95f), new Vector2(.43f, .95f), new Vector2(-1.22f, -.68f), new Vector2(.43f, -.68f) },
            // West — left-hand refinery
            new[] { new Vector2(-.88f, 1.22f), new Vector2(.83f, 1.22f), new Vector2(-.88f, -.44f), new Vector2(.83f, -.44f) }
        };

        private static Mesh[] meshes;
        private static Material[] materials;
        private static Material[][] preparationMaterials;

        private static void EnsureResources()
        {
            if (materials != null) return;
            materials = new Material[26];
            for (int i = 0; i < materials.Length; i++)
            {
                string name = i == 0 ? "BiomassRefinery_Detailed_Clean" : i == 1 ? "BiomassRefinery_Detailed_Dirty" :
                    i < 10 ? "BiomassRefinery_Detailed_Filling_" + (i - 2).ToString("00") : "BiomassRefinery_Detailed_Processing_" + (i - 10).ToString("00");
                Texture2D texture = ContentFinder<Texture2D>.Get(TextureRoot + name, false);
                if (texture == null)
                {
                    Log.ErrorOnce("GeneFoundry: missing refinery overlay " + TextureRoot + name, 1745100 + i);
                    continue;
                }
                materials[i] = MaterialPool.MatFrom(new MaterialRequest(texture, ShaderDatabase.Transparent, Color.white));
            }
            meshes = new Mesh[4];
            for (int direction = 0; direction < meshes.Length; direction++)
            {
                Vector2[] corners = VatOverlayTransforms[direction];
                var vertices = new Vector3[4];
                for (int i = 0; i < 4; i++) vertices[i] = new Vector3(corners[i].x, 0f, corners[i].y);
                var mesh = new Mesh { name = "GeneFoundry refinery overlay " + direction };
                mesh.vertices = vertices;
                mesh.uv = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
                mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
                mesh.RecalculateBounds();
                meshes[direction] = mesh;
            }
        }

        internal static void Draw(Thing parent, int frame)
        {
            EnsureResources();
            if (materials[frame] == null) return;
            GenDraw.DrawMeshNowOrLater(meshes[parent.Rotation.AsInt], parent.DrawPos + Vector3.up * .002f,
                Quaternion.identity, materials[frame], false);
        }

        internal static void DrawPreparation(Thing parent, int frame)
        {
            if (preparationMaterials == null)
            {
                preparationMaterials = new Material[4][];
                string[] directions = { "north", "east", "south", "west" };
                for (int direction = 0; direction < 4; direction++)
                {
                    preparationMaterials[direction] = new Material[8];
                    for (int index = 0; index < 8; index++)
                    {
                        string path = TextureRoot + $"BiomassRefinery_Detailed_Preparation_{index:00}_{directions[direction]}";
                        Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
                        if (texture == null)
                        {
                            Log.ErrorOnce("GeneFoundry: missing refinery preparation overlay " + path, 1745200 + direction * 8 + index);
                            continue;
                        }
                        preparationMaterials[direction][index] = MaterialPool.MatFrom(
                            new MaterialRequest(texture, ShaderDatabase.Transparent, Color.white));
                    }
                }
            }
            Material material = preparationMaterials[parent.Rotation.AsInt][frame];
            if (material == null) return;
            // Preparation artwork uses the full directional chassis canvas, not the vat quad.
            Graphic graphic = parent.Graphic;
            GenDraw.DrawMeshNowOrLater(graphic.MeshAt(parent.Rotation),
                parent.DrawPos + graphic.DrawOffset(parent.Rotation) + Vector3.up * .004f,
                Quaternion.identity, material, false);
        }
    }
}
