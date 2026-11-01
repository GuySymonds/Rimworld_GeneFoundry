using System;
using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Research;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneFoundry.Fabrication
{
    public class Dialog_GeneFabrication : Window
    {
        private readonly Building_GeneFabricator bench;
        private readonly List<GeneDef> selected;
        private readonly List<GeneDef> available;
        private Vector2 scroll;
        private string search = "";
        public override Vector2 InitialSize => new Vector2(900, 740);
        public Dialog_GeneFabrication(Building_GeneFabricator bench)
        {
            this.bench = bench;
            selected = bench.Selected;
            available = DefDatabase<GeneDef>.AllDefsListForReading.OrderBy(g => g.displayCategory?.label ?? "Uncategorized").ThenBy(g => g.displayCategory?.defName).ThenBy(g => g.label).ToList();
            forcePause = true; absorbInputAroundWindow = true; doCloseX = true; doCloseButton = true;
        }
        public override void DoWindowContents(Rect rect)
        {
            bool readOnly = bench.Pending;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0,0,rect.width,35), readOnly ? "Selected fabrication genes" : "Fabricate genepack");
            Text.Font = GameFont.Small;
            search = Widgets.TextField(new Rect(0,42,rect.width,30), search);
            var genes = available.Where(g => (!readOnly || selected.Contains(g)) &&
                (search.NullOrEmpty() || (g.label ?? g.defName).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            int groups = genes.Select(g => g.displayCategory).Distinct().Count();
            Rect view = new Rect(0,0,rect.width-20,genes.Count*52+groups*28);
            Widgets.BeginScrollView(new Rect(0,80,rect.width,rect.height-310),ref scroll,view);
            float y=0;
            GeneCategoryDef previous=null; bool first=true;
            foreach (GeneDef gene in genes)
            {
                if (first || previous != gene.displayCategory)
                {
                    Widgets.Label(new Rect(0,y,view.width,26), gene.displayCategory?.LabelCap ?? "Uncategorized");
                    y+=28; first=false; previous=gene.displayCategory;
                }
                GeneUIUtility.DrawGeneDef(gene,new Rect(0,y,48,48),GeneType.Xenogene);
                Widgets.Label(new Rect(58,y+8,view.width-190,40),$"{(selected.Contains(gene) ? "[Selected] " : "")}{gene.LabelCap} (complexity {gene.biostatCpx}, archite {gene.biostatArc})");
                bool unlocked=GeneFabricationResearch.IsUnlocked(gene);
                bool chosen=selected.Contains(gene);
                if (!readOnly && unlocked && Widgets.ButtonText(new Rect(view.width-110,y+8,105,32),chosen ? "Remove" : "Select"))
                { if(chosen) selected.Remove(gene); else selected.Add(gene); }
                else if (!readOnly && !unlocked) Widgets.Label(new Rect(view.width-110,y+8,110,40),"Research locked");
                y+=52;
            }
            Widgets.EndScrollView();
            int complexity=Building_GeneFabricator.Complexity(selected);
            float work=FabricationCalculations.Work(selected.Count,complexity);
            string summary=$"Selected: {selected.Count} genes; complexity: {complexity}\nMaterials: {FabricationCalculations.Biomass(selected.Count,complexity)} refined biomass + {Building_GeneFabricator.Archites(selected)} archite capsules\nWork: {work:0}; estimated time: {work / FabricationCalculations.Speed(10,6,4) / 2500f:0.##} game hours at minimum skills\nMinimum: Intellectual 10, Medicine 6, Crafting 4. Weights: 60% / 25% / 15%.\nFailure: 30% at minimum skills, 0% at 16 / 12 / 10; weighted by each operator's work.\nCancellation after work starts loses committed ingredients.";
            Widgets.Label(new Rect(0,rect.height-220,rect.width,160),summary);
            if (!readOnly && Widgets.ButtonText(new Rect(rect.width-240,rect.height-65,220,32),"Confirm fabrication"))
            {
                if (bench.TryStart(selected)) Close();
                else Messages.Message("Cannot start: select researched genes, provide all reachable materials, and ensure the Fabricator is empty.",MessageTypeDefOf.RejectInput,false);
            }
        }
    }
}
