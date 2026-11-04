using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Research;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneFoundry.Fabrication
{
    public static class FabricationGeneCatalog
    {
        // Include all loaded definitions, including generated, non-random and zero-complexity
        // cosmetic genes. Extraction eligibility is unrelated to manufacturing permission.
        public static List<GeneDef> Available(IEnumerable<GeneDef> genes) => genes
            .Where(GeneFabricationResearch.IsUnlocked).Distinct()
            .OrderByDescending(g => g.displayCategory?.displayPriorityInXenotype ?? 0)
            .ThenBy(g => g.displayCategory?.defName).ThenBy(g => g.label).ToList();
    }

    // The same base, search widget, gene cards and bottom controls used by the vanilla
    // assembler. Fabrication has its own footer instead of xenotype naming/assembly limits.
    public class Dialog_GeneFabrication : GeneCreationDialogBase
    {
        private readonly Building_GeneFabricator bench;
        private readonly Bill_GeneFabrication editingBill;
        private readonly bool addBill;
        private bool ReadOnly => editingBill != null ? editingBill.HasStarted : !addBill && bench.Pending;
        private readonly List<GeneDef> selected;
        private readonly List<GeneDef> available;
        private readonly HashSet<GeneCategoryDef> collapsed = new HashSet<GeneCategoryDef>();
        private Vector2 selectedScroll;
        protected override List<GeneDef> SelectedGenes => selected;
        protected override string Header => ReadOnly ? "Selected fabrication genes" : editingBill != null ? "Edit fabrication bill" : addBill ? "Add fabrication bill" : "Fabricate genepack";
        protected override string AcceptButtonLabel => editingBill != null ? "Save bill" : addBill ? "Add bill" : "Fabricate genepack";
        public override Vector2 InitialSize => new Vector2(Mathf.Min(UI.screenWidth, 1016f), UI.screenHeight - 4f);

        public Dialog_GeneFabrication(Building_GeneFabricator bench, Bill_GeneFabrication editingBill = null, bool addBill = false)
        {
            this.bench = bench;
            this.editingBill = editingBill;
            this.addBill = addBill;
            selected = (editingBill?.genes ?? (addBill ? new List<GeneDef>() : bench.Selected)).Where(g => g != null).ToList();
            available = FabricationGeneCatalog.Available(DefDatabase<GeneDef>.AllDefsListForReading);
            collapsed.UnionWith(available.Select(gene => gene.displayCategory));
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            doCloseX = true;
            xenotypeName = "";
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, rect.width - 320, 35), Header);
            Text.Font = GameFont.Small;
            DrawSearchRect(rect);
            // Reserve a fixed footer; the two gene sections scroll independently.
            DrawGenes(new Rect(0, 42, rect.width, Mathf.Max(100, rect.height - 200)));
            float y = rect.height - 150;
            int complexity = Building_GeneFabricator.Complexity(selected);
            float work = FabricationCalculations.Work(selected.Count, complexity);
            Widgets.Label(new Rect(0, y, rect.width, 76),
                $"Selected: {selected.Count} genes; complexity: {complexity}\n" +
                $"Materials: {FabricationCalculations.Biomass(selected.Count, complexity)} refined biomass ({FabricationCalculations.BiomassDiscountPercent(selected.Count)}% discount) + {Building_GeneFabricator.Archites(selected)} archite capsules\n" +
                $"Estimated time: {work / FabricationCalculations.Speed(10, 6, 4) / 2500f:0.##} game hours at minimum skills");
            Rect details = new Rect(rect.width - 180, y + 76, 180, 24);
            Widgets.Label(details, "Skills, risk and recovery");
            TooltipHandler.TipRegion(details,
                "Minimum: Intellectual 10, Medicine 6, Crafting 4. Weights: 60% / 25% / 15%.\n" +
                "Base failure: 30% at minimum; 0% at 16 / 12 / 10. Dirt, outdoors and dim light add risk.\n" +
                "Cancellation recovers 75% of committed ingredients, rounded down.");
            Rect buttons = new Rect(0, rect.height - ButSize.y, rect.width, ButSize.y);
            if (!ReadOnly) DoBottomButtons(buttons);
            else if (Widgets.ButtonText(new Rect(0, buttons.y, ButSize.x, ButSize.y), "Close".Translate())) Close();
        }

        protected override void DrawGenes(Rect rect)
        {
            float selectedHeight = Mathf.Min(190f, rect.height * .38f);
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 25), "SelectedGenes".Translate() + " — " + "ClickToAddOrRemove".Translate());
            DrawSection(new Rect(rect.x, rect.y + 26, rect.width, selectedHeight), selected.ToList(), ref selectedScroll, false);
            if (ReadOnly) return;
            float libraryY = rect.y + selectedHeight + 36;
            Widgets.Label(new Rect(rect.x, libraryY, rect.width - 120, 25), "Genes".Translate());
            bool allShown = collapsed.Count == 0;
            if (Widgets.ButtonText(new Rect(rect.xMax - 110, libraryY, 110, 24), allShown ? "Hide all" : "Show all"))
            {
                if (allShown) collapsed.UnionWith(available.Select(gene => gene.displayCategory));
                else collapsed.Clear();
                scrollPosition = Vector2.zero;
            }
            DrawSection(new Rect(rect.x, libraryY + 26, rect.width, rect.yMax - libraryY - 26),
                available.Where(g => !selected.Contains(g) && (!quickSearchWidget.filter.Active || matchingGenes.Contains(g))).ToList(),
                ref scrollPosition, true);
        }

        private void DrawSection(Rect rect, List<GeneDef> genes, ref Vector2 scroll, bool grouped)
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width - 24) / (GeneSize.x + 12)));
            var groups = grouped ? genes.GroupBy(g => g.displayCategory).Select(g => g.ToList()).ToList()
                : new List<List<GeneDef>> { genes };
            float height = 8;
            foreach (var group in groups)
            {
                if (grouped) height += 30;
                if (!grouped || !collapsed.Contains(group[0].displayCategory) || quickSearchWidget.filter.Active)
                    height += Mathf.CeilToInt(group.Count / (float)columns) * (GeneSize.y + 12);
            }
            Rect view = new Rect(0, 0, rect.width - 16, Mathf.Max(rect.height, height));
            Widgets.DrawRectFast(rect, Widgets.MenuSectionBGFillColor);
            Widgets.BeginScrollView(rect, ref scroll, view);
            float y = 4;
            foreach (var group in groups)
            {
                if (grouped)
                {
                    GeneCategoryDef category = group[0].displayCategory;
                    bool hidden = collapsed.Contains(category);
                    Rect heading = new Rect(4, y, view.width - 8, 26);
                    Widgets.Label(heading, (hidden ? "+ " : "− ") + (category?.LabelCap ?? "Uncategorized"));
                    if (Widgets.ButtonInvisible(heading))
                    { if (hidden) collapsed.Remove(category); else collapsed.Add(category); }
                    y += 30;
                    if (collapsed.Contains(category) && !quickSearchWidget.filter.Active) continue;
                }
                for (int i = 0; i < group.Count; i++)
                {
                    GeneDef gene = group[i];
                    Rect card = new Rect(4 + i % columns * (GeneSize.x + 12), y + i / columns * (GeneSize.y + 12), GeneSize.x, GeneSize.y);
                    GeneUIUtility.DrawGeneDef(gene, card, GeneType.Xenogene, clickable: false);
                    Widgets.InfoCardButton(card.xMax - 20, card.y, gene);
                    Rect info = new Rect(card.xMax - 20, card.y, 24, 24);
                    if (!ReadOnly && !Mouse.IsOver(info) && Widgets.ButtonInvisible(card))
                    {
                        if (grouped) selected.Add(gene); else selected.Remove(gene);
                        OnGenesChanged();
                        UpdateSearchResults();
                    }
                    if (Mouse.IsOver(card)) Widgets.DrawHighlight(card);
                }
                y += Mathf.CeilToInt(group.Count / (float)columns) * (GeneSize.y + 12);
            }
            if (genes.Count == 0) Widgets.Label(new Rect(8, 4, view.width - 16, 30), "NoneLower".Translate());
            Widgets.EndScrollView();
        }

        protected override void OnGenesChanged()
        {
            gcx = Building_GeneFabricator.Complexity(selected);
            arc = Building_GeneFabricator.Archites(selected);
        }
        protected override void UpdateSearchResults()
        {
            matchingGenes.Clear();
            foreach (GeneDef gene in available)
                if (quickSearchWidget.filter.Matches(gene.label) || quickSearchWidget.filter.Matches(gene.displayCategory?.label ?? "Uncategorized"))
                    matchingGenes.Add(gene);
            quickSearchWidget.noResultsMatched = quickSearchWidget.filter.Active && matchingGenes.Count == 0;
        }
        protected override bool CanAccept() => !ReadOnly && selected.Count > 0 && selected.All(GeneFabricationResearch.IsUnlocked);
        protected override void Accept()
        {
            bool accepted = editingBill != null ? bench.TryConfigureBill(editingBill, selected) : addBill ? bench.TryAddBill(selected) : bench.TryStart(selected);
            if (accepted) Close();
            else Messages.Message("Cannot confirm: select researched genes. Bills need a free bill slot and cannot be edited during their active job. Immediate fabrication also needs reachable materials and an empty Fabricator.", MessageTypeDefOf.RejectInput, false);
        }
    }
}
