using RimWorld;
using UnityEngine;
using Verse;

namespace GeneFoundry.Components
{
    // GameComponent discovery also adds this to colonies created before the mod was installed.
    public class GameComponent_GeneFoundryIntroduction : GameComponent
    {
        private bool introductionShown;
        private float nextCheckTime;

        public GameComponent_GeneFoundryIntroduction(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref introductionShown, "geneFoundryIntroductionShown", false);
        }

        public override void GameComponentUpdate()
        {
            // Updates run while paused, unlike ticks, so an existing colony sees this on load.
            if (introductionShown || Current.ProgramState != ProgramState.Playing ||
                Find.CurrentMap == null || Time.realtimeSinceStartup < nextCheckTime)
                return;

            nextCheckTime = Time.realtimeSinceStartup + 1f;
            ResearchProjectDef xenogenetics = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Xenogermination");
            if (xenogenetics == null || !xenogenetics.IsFinished ||
                Find.WindowStack.IsOpen<Dialog_MessageBox>())
                return;

            Find.WindowStack.Add(new Dialog_MessageBox("GF_IntroductionText".Translate(),
                buttonAText: "OK".Translate(), title: "GF_IntroductionTitle".Translate())
            {
                forcePause = true
            });
            introductionShown = true;
        }
    }
}
