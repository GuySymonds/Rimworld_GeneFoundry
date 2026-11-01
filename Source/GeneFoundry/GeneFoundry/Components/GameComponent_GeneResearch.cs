using System.Linq;
using GeneFoundry.Research;
using UnityEngine;
using Verse;

namespace GeneFoundry.Components
{
    public class GameComponent_GeneResearch : GameComponent
    {
        private float nextCheckTime;

        public GameComponent_GeneResearch(Game game) { }

        public override void GameComponentUpdate()
        {
            // Updates also run while paused, including after loading an existing colony.
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null ||
                Time.realtimeSinceStartup < nextCheckTime)
                return;
            nextCheckTime = Time.realtimeSinceStartup + 1f;

            ResearchProjectDef fabrication = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("GF_GeneFabrication");
            var projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .OfType<GeneCategoryResearchProjectDef>();
            foreach (var project in GeneFabricationResearch.AutomaticProjectsToComplete(fabrication, projects))
                Find.ResearchManager.FinishProject(project, false, null, false);
        }
    }
}
