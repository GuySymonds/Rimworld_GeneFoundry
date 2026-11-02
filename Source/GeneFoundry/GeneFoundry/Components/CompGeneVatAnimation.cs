using Verse;
using Verse.AI;
using RimWorld;

namespace GeneFoundry.Components
{
    public interface IGeneVatProcessing
    {
        bool ProcessingBiomass { get; }
    }

    public class CompProperties_GeneVatAnimation : CompProperties
    {
        public CompProperties_GeneVatAnimation() { compClass = typeof(CompGeneVatAnimation); }
    }

    // Transient presentation only: rebuilt by the work toil after loading a save.
    public class CompGeneVatAnimation : ThingComp
    {
        private Pawn worker;
        private JobDriver driver;
        private int startedAt;

        public int FrameAt(int tick)
        {
            if (worker == null || worker.Destroyed || !worker.Spawned || worker.Map != parent.Map ||
                worker.Drafted || worker.jobs?.curDriver != driver ||
                !(driver is IGeneVatProcessing processing) || !processing.ProcessingBiomass ||
                !(parent is Building_WorkTable table) || !table.Spawned || !table.UsableForBillsAfterFueling())
                return -1;
            return GeneVatAnimationTiming.FrameAt(tick - startedAt);
        }

        public void NotifyProcessing(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            if (worker != pawn || driver != pawn.jobs.curDriver || FrameAt(now) < 0)
                startedAt = now;
            worker = pawn;
            driver = pawn.jobs.curDriver;
        }
    }
}
