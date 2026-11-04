using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Research;
using UnityEngine;
using Verse;

namespace GeneFoundry.Components
{
    public class GameComponent_GeneResearch : GameComponent
    {
        private float nextCheckTime;
        private List<string> completedProjects = new List<string>();

        public GameComponent_GeneResearch(Game game) { }

        private static IEnumerable<ResearchProjectDef> Projects =>
            DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(project =>
                project is GeneCategoryResearchProjectDef || project.defName == "GF_GeneFabrication");

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Record completion against this session's costs, before definitions can change.
                // Keep IDs for temporarily absent mods; use strings to avoid missing-def references.
                var projects = Projects.ToList();
                var present = new HashSet<string>(projects.Select(project => project.defName));
                completedProjects.RemoveAll(present.Contains);
                completedProjects.AddRange(projects.Where(project => project.IsFinished)
                    .Select(project => project.defName));
            }
            Scribe_Collections.Look(ref completedProjects, "completedGeneResearch", LookMode.Value);
            if (completedProjects == null)
                completedProjects = new List<string>();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            var completed = new HashSet<string>(completedProjects);
            foreach (var project in Projects)
                if (completed.Contains(project.defName) && !project.IsFinished)
                    RestoreCompletion(project);
        }

        protected virtual void RestoreCompletion(ResearchProjectDef project) =>
            Find.ResearchManager.FinishProject(project, false, null, false);

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
