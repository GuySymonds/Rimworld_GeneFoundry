using GeneFoundry.Jobs;
using GeneFoundry.Components;
using UnityEngine;
using Verse;

namespace GeneFoundry.Fabrication
{
    public partial class Building_GeneFabricator
    {
        // Presentation state is rebuilt by the work toil after loading.
        private Pawn animationWorker;
        private JobDriver_FabricateGenePack animationDriver;
        private int animationStartedAt;

        public void NotifyFabricating(Pawn pawn, JobDriver_FabricateGenePack driver)
        {
            animationWorker = pawn;
            animationDriver = driver;
            animationStartedAt = Find.TickManager.TicksGame;
        }

        public int AnimationFrame => animationWorker != null && animationWorker.Spawned &&
            !animationWorker.Destroyed && !animationWorker.Drafted && animationWorker.Map == Map &&
            animationWorker.jobs?.curDriver == animationDriver && animationDriver.Working && CanWork
            ? (Find.TickManager.TicksGame - animationStartedAt) / 6 % 8 : -1;
    }

    public class Graphic_GeneFabricator : Graphic_StyledMachine
    {
        public override bool ShouldDrawRotated => false;
        private readonly Material[][] frames = new Material[4][];
        private readonly Material[] idleFrames = new Material[4];
        private static readonly string[] Directions = { "north", "east", "south", "west" };

        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            for (int direction = 0; direction < 4; direction++)
            {
                if (UsesSimple(new Rot4(direction))) continue;
                idleFrames[direction] = OverlayMaterial(
                    $"GeneFoundry/Buildings/GeneFabricatorFrames/GeneFabricator_Detailed_Idle_{Directions[direction]}", req.color);
                var set = new Material[8];
                for (int frame = 0; frame < set.Length; frame++)
                {
                    string framePath = $"GeneFoundry/Buildings/GeneFabricatorFrames/GeneFabricator_Detailed_Processing_{frame:00}_{Directions[direction]}";
                    var texture = ContentFinder<Texture2D>.Get(framePath, false);
                    if (texture == null)
                    {
                        Log.ErrorOnce("GeneFoundry: missing fabricator frame " + framePath, 1745040 + direction);
                        set = null;
                        break;
                    }
                    set[frame] = MaterialPool.MatFrom(new MaterialRequest(texture, ShaderDatabase.Transparent, req.color)
                    {
                        renderQueue = 3000
                    });
                }
                frames[direction] = set;
            }
        }

        private static Material OverlayMaterial(string texturePath, Color color)
        {
            var texture = ContentFinder<Texture2D>.Get(texturePath);
            return texture == null ? null : MaterialPool.MatFrom(new MaterialRequest(texture, ShaderDatabase.Transparent, color)
            {
                renderQueue = 3000
            });
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            if (UsesSimple(rot)) return;
            int frame = (thing as Building_GeneFabricator)?.AnimationFrame ?? -1;
            Material overlay = frame >= 0 && frames[rot.AsInt] != null
                ? frames[rot.AsInt][frame] : idleFrames[rot.AsInt];
            if (overlay == null) return;
            // The overlay canvases already face their direction; match the base mesh.
            Quaternion rotation = extraRotation == 0f ? Quaternion.identity : Quaternion.Euler(Vector3.up * extraRotation);
            GenDraw.DrawMeshNowOrLater(MeshAt(rot), loc + DrawOffset(rot) + Vector3.up * .002f,
                rotation, overlay, false);
        }

        public override Graphic GetColoredVersion(Shader shader, Color color, Color colorTwo)
            => GraphicDatabase.Get<Graphic_GeneFabricator>(path, shader, drawSize, color, colorTwo, data);
    }
}
