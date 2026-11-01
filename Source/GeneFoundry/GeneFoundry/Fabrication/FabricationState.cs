using System;
using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Research;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GeneFoundry.Fabrication
{
    // Vanilla UnfinishedThing requires a fixed recipe and Bill_ProductionWithUft. This
    // selected-gene item lives in its workstation's ThingOwner with its paid job state.
    public class UnfinishedGenePack : ThingWithComps
    {
        public List<GeneDef> genes = new List<GeneDef>();
        public float totalWork, workDone, riskWork;
        public bool committed;
        public bool outcomeRolled, failed;
        public Pawn creator;
        private bool creatorAssigned;
        public bool CanBeWorkedBy(Pawn pawn) => pawn != null && (!creatorAssigned || creator == pawn);
        public bool TryClaim(Pawn pawn)
        {
            if (!Valid || !CanBeWorkedBy(pawn)) return false;
            creator = pawn; creatorAssigned = true; return true;
        }
        public bool Valid => committed && genes != null && genes.Count > 0 &&
            genes.All(g => g != null) && genes.Distinct().Count() == genes.Count &&
            totalWork > 0 && !float.IsInfinity(totalWork) && workDone >= 0 && workDone <= totalWork &&
            riskWork >= 0 && !float.IsInfinity(riskWork) && riskWork <= workDone * .30001f;
        public float Failure => FabricationCalculations.WeightedFailure(riskWork, workDone);
        public float AddWork(float amount, float risk)
        {
            if (!Valid || float.IsNaN(amount) || float.IsInfinity(amount) || float.IsNaN(risk) || amount <= 0) return 0;
            float actual = Math.Min(amount, totalWork - workDone);
            workDone += actual;
            riskWork += actual * Mathf.Clamp(risk, 0, .30f);
            return actual;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            ExposeJobData();
        }
        internal void ExposeJobData()
        {
            Scribe_Collections.Look(ref genes, "gfGenes", LookMode.Def);
            Scribe_Values.Look(ref totalWork, "gfTotalWork");
            Scribe_Values.Look(ref workDone, "gfWorkDone");
            Scribe_Values.Look(ref riskWork, "gfRiskWork");
            Scribe_Values.Look(ref committed, "gfCommitted");
            Scribe_Values.Look(ref outcomeRolled, "gfOutcomeRolled");
            Scribe_Values.Look(ref failed, "gfFailed");
            Scribe_References.Look(ref creator, "gfCreator");
            Scribe_Values.Look(ref creatorAssigned, "gfCreatorAssigned");
        }
        public override string GetInspectString() => Valid
            ? $"Genes: {genes.Count}\nProgress: {workDone / totalWork:P0}\nFailure estimate: {Failure:P1}"
            : "No valid paid fabrication job. Cannot be fabricated.";
    }

    public static class FabricationSkills
    {
        public static bool Qualified(Pawn p) => p?.skills != null &&
            !p.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled &&
            !p.skills.GetSkill(SkillDefOf.Medicine).TotallyDisabled &&
            !p.skills.GetSkill(SkillDefOf.Crafting).TotallyDisabled &&
            FabricationCalculations.Qualified(Level(p, SkillDefOf.Intellectual), Level(p, SkillDefOf.Medicine), Level(p, SkillDefOf.Crafting));
        private static int Level(Pawn p, SkillDef s) => p.skills.GetSkill(s).Level;
        public static float Speed(Pawn p) => FabricationCalculations.Speed(Level(p, SkillDefOf.Intellectual), Level(p, SkillDefOf.Medicine), Level(p, SkillDefOf.Crafting)) *
            p.GetStatValue(StatDefOf.WorkSpeedGlobal) * p.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) * p.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
        public static float Failure(Pawn p) => FabricationCalculations.Failure(Level(p, SkillDefOf.Intellectual), Level(p, SkillDefOf.Medicine), Level(p, SkillDefOf.Crafting));
        public static void Learn(Pawn p, int ticks)
        {
            float xp = FabricationCalculations.XpPerTick * ticks;
            p.skills.Learn(SkillDefOf.Intellectual, xp * FabricationCalculations.IntellectualWeight);
            p.skills.Learn(SkillDefOf.Medicine, xp * FabricationCalculations.MedicineWeight);
            p.skills.Learn(SkillDefOf.Crafting, xp * FabricationCalculations.CraftingWeight);
        }
    }

    public class Building_GeneFabricator : Building_WorkTable, IThingHolder
    {
        private ThingOwner<Thing> contents;
        private List<GeneDef> selected = new List<GeneDef>();
        private int generation;
        private Bill_GeneFabrication activeBill;
        public Bill_GeneFabrication ActiveBill => activeBill;
        public bool CanBeWorkedBy(Pawn pawn) => Unfinished == null || Unfinished.CanBeWorkedBy(pawn);
        public int Generation => generation;
        public Building_GeneFabricator() { contents = new ThingOwner<Thing>(this); }
        public List<GeneDef> Selected => new List<GeneDef>(selected ?? new List<GeneDef>());
        public UnfinishedGenePack Unfinished => contents.OfType<UnfinishedGenePack>().FirstOrDefault();
        public bool Pending => selected != null && selected.Count > 0;
        public bool Powered => this.TryGetComp<CompPowerTrader>()?.PowerOn == true &&
            this.TryGetComp<CompBreakdownable>()?.BrokenDown != true;
        public bool CanWork => Spawned && Pending && Powered && (activeBill == null || (!activeBill.DeletedOrDereferenced && activeBill.ShouldDoNow())) && selected.All(GeneFabricationResearch.IsUnlocked) &&
            (Unfinished == null || (Unfinished.Valid && selected.SequenceEqual(Unfinished.genes)));
        public static int Complexity(IEnumerable<GeneDef> genes) => genes.Sum(g => Math.Max(0, g.biostatCpx));
        public static int Archites(IEnumerable<GeneDef> genes) => genes.Sum(g => Math.Max(0, g.biostatArc));
        public static ThingDef BiomassDef => DefDatabase<ThingDef>.GetNamed("GF_RefinedBiomass");
        public static ThingDef CapsuleDef => DefDatabase<ThingDef>.GetNamed("ArchiteCapsule");
        public int Required(ThingDef def) => def == BiomassDef ? FabricationCalculations.Biomass(selected.Count, Complexity(selected)) : def == CapsuleDef ? Archites(selected) : 0;
        public int Missing(ThingDef def) => Unfinished != null ? 0 : Math.Max(0, Required(def) - contents.Where(t => t.def == def).Sum(t => t.stackCount));
        public bool HasAvailable(List<GeneDef> genes, Pawn pawn = null)
        {
            int biomass = FabricationCalculations.Biomass(genes.Count, Complexity(genes));
            int capsules = Archites(genes);
            return Available(BiomassDef, pawn) >= biomass && Available(CapsuleDef, pawn) >= capsules;
        }
        private int Available(ThingDef def, Pawn pawn) => Map.listerThings.ThingsOfDef(def)
            .Where(t => !t.IsForbidden(Faction) && (pawn == null
                ? Map.reachability.CanReach(InteractionCell, t, PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.PassDoors))
                : pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Some)))
            .Sum(t => t.stackCount);
        public bool TryStart(IEnumerable<GeneDef> genes)
        {
            var list = genes?.Where(g => g != null).Distinct().ToList();
            if (!Spawned || Pending || contents.Count != 0 || list == null || list.Count == 0 ||
                !list.All(GeneFabricationResearch.IsUnlocked) || !HasAvailable(list)) return false;
            selected = list;
            generation++;
            return true;
        }
        // A delivery job carries a single exact stack count through vanilla carry toils.
        // Partial deliveries remain physical owned Things, so an interruption cannot charge twice.
        public bool AcceptDelivery(Pawn pawn)
        {
            Thing carried = pawn.carryTracker.CarriedThing;
            if (!CanWork || carried == null || Unfinished != null) return false;
            int count = Math.Min(Missing(carried.def), carried.stackCount);
            return count > 0 && pawn.carryTracker.innerContainer.TryTransferToContainer(carried, contents, count) == count;
        }
        public bool TryCommit(Pawn pawn)
        {
            if (pawn == null || !CanWork || !FabricationSkills.Qualified(pawn) || !CanBeWorkedBy(pawn)) return false;
            return CommitDeliveredMaterials() && Unfinished.TryClaim(pawn);
        }
        internal bool CommitDeliveredMaterials()
        {
            if (!Pending || selected.Any(g => g == null)) return false;
            if (Unfinished != null) return Unfinished.Valid;
            if (Missing(BiomassDef) > 0 || Missing(CapsuleDef) > 0) return false;
            var item = MakeUnfinished();
            item.genes = Selected;
            item.totalWork = FabricationCalculations.Work(selected.Count, Complexity(selected));
            item.committed = true;
            if (!contents.TryAdd(item, false)) { item.Destroy(); return false; }
            ConsumeDelivered(BiomassDef, Required(BiomassDef));
            ConsumeDelivered(CapsuleDef, Required(CapsuleDef));
            return true;
        }
        private void ConsumeDelivered(ThingDef def, int count)
        {
            foreach (Thing stack in contents.Where(t => t.def == def).ToList())
            {
                if (count <= 0) break;
                int take = Math.Min(count, stack.stackCount);
                Thing consumed = stack.SplitOff(take);
                consumed.Destroy();
                count -= take;
            }
        }
        public void Work(Pawn pawn, int ticks)
        {
            if (!CanWork || !FabricationSkills.Qualified(pawn) || !TryCommit(pawn)) return;
            var item = Unfinished;
            float actual = item.AddWork(FabricationSkills.Speed(pawn) * ticks, FabricationSkills.Failure(pawn));
            if (actual > 0) FabricationSkills.Learn(pawn, ticks);
            if (item.workDone >= item.totalWork) Complete(item.outcomeRolled ? 0 : Rand.Value);
        }
        public bool Complete(float roll)
        {
            var item = Unfinished;
            if (item == null || !item.Valid || item.workDone < item.totalWork) return false;
            if (!item.outcomeRolled)
            {
                item.failed = roll < item.Failure;
                item.outcomeRolled = true;
            }
            if (item.failed)
            {
                contents.Remove(item);
                item.Destroy();
                FinishBillIteration(item.creator);
                selected.Clear();
                NotifyFailure();
                return true;
            }
            var output = MakeOutput();
            output.Initialize(new List<GeneDef>(item.genes));
            // Keep the actual output in the saved owner until placement succeeds. No second roll
            // or second MakeThing on retry, including after save/load or a blocked output tile.
            if (!contents.TryAdd(output, false))
            {
                output.Destroy();
                return false; // Keep the paid job and its already-rolled success for retry.
            }
            contents.Remove(item);
            item.Destroy();
            FinishBillIteration(item.creator);
            selected.Clear();
            TryPlaceOutput();
            return true;
        }
        protected virtual UnfinishedGenePack MakeUnfinished() => (UnfinishedGenePack)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("GF_UnfinishedGenePack"));
        protected virtual Genepack MakeOutput() => (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
        protected virtual void NotifyFailure() => Messages.Message("Gene fabrication failed. The committed ingredients and unfinished genepack were lost.", this, MessageTypeDefOf.NegativeEvent);
        private void TryPlaceOutput()
        {
            if (!Spawned) return;
            foreach (Thing output in contents.OfType<Genepack>().ToList())
                contents.TryDrop(output, InteractionCell, Map, ThingPlaceMode.Near, out Thing placed);
        }
        public void Cancel()
        {
            generation++;
            var bill = activeBill;
            activeBill = null;
            if (bill != null && !bill.deleted && billStack.Bills.Contains(bill)) billStack.Delete(bill);
            if (Unfinished != null) { var item = Unfinished; contents.Remove(item); item.Destroy(); }
            selected.Clear();
            if (Spawned) contents.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }
        public bool TryAddBill(IEnumerable<GeneDef> genes)
        {
            var list = genes?.Distinct().ToList();
            if (billStack.Count >= BillStack.MaxCount || list == null || list.Count == 0 ||
                !list.All(GeneFabricationResearch.IsUnlocked)) return false;
            billStack.AddBill(new Bill_GeneFabrication(list));
            return true;
        }
        public bool TryConfigureBill(Bill_GeneFabrication bill, IEnumerable<GeneDef> genes)
        {
            var list = genes?.Distinct().ToList();
            if (!billStack.Bills.Contains(bill) || (Pending && activeBill == bill) ||
                list == null || list.Count == 0 || !list.All(GeneFabricationResearch.IsUnlocked)) return false;
            bill.genes = list;
            return true;
        }
        public void TryStartNextBill()
        {
            if (!Spawned || !Powered || Pending || contents.Count != 0) return;
            foreach (var bill in billStack.Bills.OfType<Bill_GeneFabrication>())
                if (bill.ShouldDoNow() && TryStart(bill.genes))
                {
                    activeBill = bill;
                    return;
                }
        }
        private void FinishBillIteration(Pawn pawn)
        {
            var bill = activeBill;
            activeBill = null; // Clear before callbacks: completion must never count twice.
            if (bill != null && !bill.DeletedOrDereferenced)
                bill.Notify_IterationCompleted(pawn, new List<Thing>());
        }
        public override void Notify_BillDeleted(Bill bill)
        {
            base.Notify_BillDeleted(bill);
            if (activeBill == bill) Cancel();
        }
        public override void TickRare()
        {
            base.TickRare();
            if (!Pending && Spawned)
            {
                contents.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
                TryStartNextBill();
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned) contents.TryDropAll(Position, Map, ThingPlaceMode.Near);
            base.Destroy(mode);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            // Colonies saved before the Bills tab have no billStack node.
            if (billStack == null) billStack = new BillStack(this);
            Scribe_Deep.Look(ref contents, "gfContents", this);
            Scribe_Collections.Look(ref selected, "gfSelectedGenes", LookMode.Def);
            Scribe_Values.Look(ref generation, "gfGeneration");
            Scribe_References.Look(ref activeBill, "gfActiveBill");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (contents == null) contents = new ThingOwner<Thing>(this);
                if (billStack == null) billStack = new BillStack(this);
                if (selected == null) selected = new List<GeneDef>();
            }
        }
        public ThingOwner GetDirectlyHeldThings() => contents;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, contents);
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (Unfinished != null)
            {
                drawLoc.y = Altitudes.AltitudeFor(AltitudeLayer.BuildingOnTop);
                Unfinished.Graphic.Draw(drawLoc, Rot4.North, Unfinished);
            }
        }
        public override string GetInspectString()
        {
            string state = !Pending ? (contents.OfType<Genepack>().Any() ? "Output waiting for space" : "Ready") : !Powered ? "No power / breakdown" : Unfinished == null ? "Awaiting materials" : Unfinished.Valid ? "Fabricating / awaiting qualified operator" : "Invalid job: cancel to clear";
            string text = base.GetInspectString() + "\n" + state;
            if (Pending) text += $"\nGenes: {selected.Count}";
            if (Unfinished != null) text += $"\nProgress: {Unfinished.workDone / Math.Max(1, Unfinished.totalWork):P0}; remaining work: {Math.Max(0, Unfinished.totalWork - Unfinished.workDone):0}\nFailure estimate: {Unfinished.Failure:P1}";
            else if (Pending) text += $"\nMissing: {Missing(BiomassDef)} refined biomass, {Missing(CapsuleDef)} archite capsules";
            if (Unfinished != null) text += "\nOwner: " + (Unfinished.creator?.LabelShortCap ?? "Unassigned / unavailable");
            if (activeBill != null && activeBill.suspended) text += "\nBill suspended";
            return text.Trim();
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            if (Faction != Faction.OfPlayer) yield break;
            yield return new Command_Action { defaultLabel = Pending ? "Inspect selected genes" : "Fabricate genepack", defaultDesc = "Select researched genes for one manual fabrication job.", icon = ContentFinder<Texture2D>.Get("UI/Gizmos/RecombineGenes"), action = () => Find.WindowStack.Add(new Dialog_GeneFabrication(this)) };
            yield return new Command_Action { defaultLabel = "Add fabrication bill", defaultDesc = "Queue one genepack with the selected genes. No quantity or repeat controls.", icon = ContentFinder<Texture2D>.Get("UI/Gizmos/RecombineGenes"), action = () => Find.WindowStack.Add(new Dialog_GeneFabrication(this, addBill: true)) };
            if (Pending) yield return new Command_Action { defaultLabel = "Cancel fabrication", defaultDesc = "Return delivered ingredients before work starts. Committed ingredients are lost.", icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"), action = Cancel };
        }
    }
}
