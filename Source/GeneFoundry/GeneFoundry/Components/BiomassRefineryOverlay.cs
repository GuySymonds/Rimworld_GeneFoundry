using UnityEngine;
using Verse;

namespace GeneFoundry.Components
{
    internal static class BiomassRefineryOverlay
    {
        private const string TextureRoot = "GeneFoundry/Buildings/BioMassRefineryFramesAndOverlays/";

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

        private static void EnsureResources()
        {
            if (materials != null) return;
            materials = new Material[26];
            for (int i = 0; i < materials.Length; i++)
            {
                string name = i == 0 ? "CleanVat" : i == 1 ? "DirtyVat" :
                    i < 10 ? "VatFilling" + (i - 2).ToString("00") : "VatProcess" + (i - 10).ToString("00");
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
    }
}
