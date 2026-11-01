using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneFoundry.Fabrication
{
    public class Bill_GeneFabrication : Bill
    {
        public List<GeneDef> genes = new List<GeneDef>();
        private bool completed;
        private bool started;
        public UnfinishedGenePack unfinished;
        public bool HasStarted => started || unfinished != null;
        public void BindUnfinished(UnfinishedGenePack item) { unfinished = item; started = true; item.bill = this; }
        public bool CanBeScheduledFor(Pawn pawn) => ShouldDoNow() && (!HasStarted || unfinished.CanBeWorkedBy(pawn));
        public string AssignmentLabel => unfinished?.OwnerLabel ?? (started ? "Unfinished pack unavailable" : "Unassigned");
        protected override string StatusString => "Assigned to: " + AssignmentLabel;
        public Bill_GeneFabrication() { }
        public Bill_GeneFabrication(IEnumerable<GeneDef> genes)
            : base(DefDatabase<RecipeDef>.GetNamed("GF_FabricateSelectedGenes"))
        {
            this.genes = genes.ToList();
        }
        public override string Label => "Fabricate " + genes.Count + "-gene pack: " +
            string.Join(", ", genes.Take(2).Select(g => g?.label ?? "missing gene")) + (genes.Count > 2 ? "…" : "");
        public override string LabelCap => Label;
        public override bool ShouldDoNow() => !completed && !suspended && genes != null &&
            genes.Count > 0 && genes.All(g => g != null) && (!HasStarted || (unfinished != null && !unfinished.Destroyed && unfinished.Valid && !unfinished.cancellationHandled));
        // Vanilla DoBill must never run this recipe: it cannot handle selected genes,
        // custom ownership, material commitment or failure. Only our workgiver executes it.
        public override bool PawnAllowedToStartAnew(Pawn pawn) => false;
        protected override bool CanCopy => false;
        protected override void DoConfigInterface(Rect rect, Color color)
        {
            Widgets.Label(new Rect(28, 29, rect.width - 140, 24), "One genepack");
            if (Widgets.ButtonText(new Rect(rect.xMax - 100, 27, 100, 24), "Details".Translate() + "…"))
                Find.WindowStack.Add(new Dialog_GeneFabrication((Building_GeneFabricator)billStack.billGiver, this));
        }
        public override void Notify_IterationCompleted(Pawn pawn, List<Thing> ingredients)
        {
            if (completed) return;
            completed = true;
            billStack?.Delete(this);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            ExposeGeneData();
        }
        internal void ExposeGeneData()
        {
            Scribe_Collections.Look(ref genes, "gfBillGenes", LookMode.Def);
            Scribe_Values.Look(ref completed, "gfBillCompleted");
            Scribe_Values.Look(ref started, "gfBillStarted");
            Scribe_References.Look(ref unfinished, "gfBillUnfinished");
        }
        public override Bill Clone()
        {
            var clone = (Bill_GeneFabrication)base.Clone();
            clone.genes = new List<GeneDef>(genes);
            return clone;
        }
    }

    // Native BillStack listing supplies ordering, suspension and deletion. Each entry
    // is one pack: there are no quantity or repeat controls.
    public class ITab_GeneFabricationBills : ITab_Bills
    {
        private Vector2 scroll;
        private float viewHeight = 1000;
        protected override void FillTab()
        {
            var bench = (Building_GeneFabricator)SelTable;
            bench.billStack.DoListing(new Rect(0, 0, size.x, size.y).ContractedBy(10),
                () => new List<FloatMenuOption> { new FloatMenuOption("Fabricate genepack…",
                    () => Find.WindowStack.Add(new Dialog_GeneFabrication(bench, addBill: true))) },
                ref scroll, ref viewHeight);
        }
    }
}
