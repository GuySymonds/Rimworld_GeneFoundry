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
        public static List<GeneDef> ByCost(IEnumerable<GeneDef> genes) => genes
            .OrderByDescending(gene => FabricationCalculations.GeneBiomass(gene.biostatCpx))
            .ThenBy(gene => gene.label ?? gene.defName, System.StringComparer.OrdinalIgnoreCase)
            .ThenBy(gene => gene.defName, System.StringComparer.Ordinal).ToList();

        // Include all loaded definitions, including generated, non-random and zero-complexity
        // cosmetic genes. Extraction eligibility is unrelated to manufacturing permission.
        public static List<GeneDef> Available(IEnumerable<GeneDef> genes) => genes
            .Where(GeneFabricationResearch.IsUnlocked).Distinct()
            .OrderByDescending(g => g.displayCategory?.displayPriorityInXenotype ?? 0)
            .ThenBy(g => g.displayCategory?.defName)
            .ThenByDescending(g => FabricationCalculations.GeneBiomass(g.biostatCpx))
            .ThenBy(g => g.label ?? g.defName, System.StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.defName, System.StringComparer.Ordinal).ToList();
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
        private static readonly Vector2 CardSize = new Vector2(110f, 90f);
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
            selected = FabricationGeneCatalog.ByCost((editingBill?.genes ?? (addBill ? new List<GeneDef>() : bench.Selected)).Where(g => g != null));
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
                $"Materials: {Building_GeneFabricator.Biomass(selected)} refined biomass + {Building_GeneFabricator.Archites(selected)} archite capsules\n" +
                $"Estimated time: {work / FabricationCalculations.Speed(10) / 2500f:0.##} game hours at minimum skills");
            Rect details = new Rect(rect.width - 180, y + 76, 180, 24);
            Widgets.Label(details, "Skills, risk and recovery");
            TooltipHandler.TipRegion(details,
                "Biomass by gene, most expensive first: 100%, 90%, 80%, 70%, 60%, then 50% each. Total rounded up.\n" +
                "Minimum: Crafting 10. Crafting controls speed and operator failure.\n" +
                "Base failure: 30% at minimum; 0% at Crafting 20. Dirt, outdoors and dim light add risk.\n" +
                "Cancellation recovers 75% of committed ingredients, rounded down.");
            Rect buttons = new Rect(0, rect.height - ButSize.y, rect.width, ButSize.y);
            if (!ReadOnly) DoBottomButtons(buttons);
            else if (Widgets.ButtonText(new Rect(0, buttons.y, ButSize.x, ButSize.y), "Close".Translate())) Close();
        }

        protected override void DrawGenes(Rect rect)
        {
            int selectedRows = Mathf.CeilToInt(selected.Count / (float)Columns(rect.width));
            float neededHeight = selected.Count == 0 ? 32f : 8f + selectedRows * (CardSize.y + 12f);
            float selectedHeight = Mathf.Min(neededHeight,
                ReadOnly ? rect.height - 26f : rect.height * .38f);
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

        private static int Columns(float width) =>
            Mathf.Max(1, Mathf.FloorToInt((width - 24f) / (CardSize.x + 12f)));

        private void DrawSection(Rect rect, List<GeneDef> genes, ref Vector2 scroll, bool grouped)
        {
            int columns = Columns(rect.width);
            var groups = grouped ? genes.GroupBy(g => g.displayCategory).Select(g => g.ToList()).ToList()
                : new List<List<GeneDef>> { genes };
            float height = 8;
            foreach (var group in groups)
            {
                if (grouped) height += 30;
                if (!grouped || !collapsed.Contains(group[0].displayCategory) || quickSearchWidget.filter.Active)
                    height += Mathf.CeilToInt(group.Count / (float)columns) * (CardSize.y + 12);
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
                    Rect card = new Rect(4 + i % columns * (CardSize.x + 12), y + i / columns * (CardSize.y + 12), CardSize.x, CardSize.y);
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
                y += Mathf.CeilToInt(group.Count / (float)columns) * (CardSize.y + 12);
            }
            if (genes.Count == 0) Widgets.Label(new Rect(8, 4, view.width - 16, 30), "NoneLower".Translate());
            Widgets.EndScrollView();
        }

        protected override void OnGenesChanged()
        {
            var ordered = FabricationGeneCatalog.ByCost(selected);
            selected.Clear();
            selected.AddRange(ordered);
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
