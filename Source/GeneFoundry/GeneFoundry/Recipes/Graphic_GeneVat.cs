using GeneFoundry.Components;
using UnityEngine;
using Verse;

namespace GeneFoundry.Recipes
{
    // Use the native Multi mesh, offsets, shadow and rotation; replace only its north material.
    public class Graphic_GeneVat : Graphic_Multi
    {
        private Material[] frames;

        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            frames = new Material[8];
            for (int i = 0; i < frames.Length; i++)
            {
                string framePath = "GeneFoundry/Buildings/GeneVatFrames/" + i.ToString("00");
                Texture2D texture = ContentFinder<Texture2D>.Get(framePath, false);
                if (texture == null)
                {
                    Log.ErrorOnce("GeneFoundry: missing Gene Vat animation texture " + framePath + "; using the idle image.", 1745020 + i);
                    frames = null;
                    return;
                }
                frames[i] = MaterialPool.MatFrom(new MaterialRequest(texture, req.shader, req.color)
                {
                    colorTwo = req.colorTwo,
                    renderQueue = req.renderQueue,
                    shaderParameters = req.shaderParameters
                });
            }
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            if (rot == Rot4.North && frames != null && thing != null)
            {
                int frame = thing.TryGetComp<CompGeneVatAnimation>()?.FrameAt(Find.TickManager.TicksGame) ?? -1;
                if (frame >= 0) return frames[frame];
            }
            return base.MatAt(rot, thing);
        }

        public override Graphic GetColoredVersion(Shader shader, Color color, Color colorTwo)
            => GraphicDatabase.Get<Graphic_GeneVat>(path, shader, drawSize, color, colorTwo, data);
    }
}
