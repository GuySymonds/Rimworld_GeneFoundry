using System;
using System.Collections.Generic;
using System.Linq;
using GeneFoundry.Research;
using RimWorld;
using Verse;
using Verse.AI;

namespace GeneFoundry.Fabrication
{
    public partial class Building_GeneFabricator
    {
        public bool AtWorkSurface(UnfinishedGenePack item) => item != null && item.Spawned && item.Map == Map && item.Position == Position;
        public bool PlaceCurrentItem()
        {
            var item = Unfinished;
            if (!Spawned || item == null) return false;
            if (AtWorkSurface(item)) return true;
            if (!contents.Contains(item)) return false;
            contents.Remove(item);
            GenSpawn.Spawn(item, Position, Map);
            currentItem = item;
            item.fabricator = this;
            item.bill = activeBill;
            activeBill?.BindUnfinished(item);
            return true;
        }
        // Detach only the workstation slot. Paid state and the bill association belong
        // to the physical item and survive hauling, storage and another bill using the bench.
        internal bool DetachUnfinished(UnfinishedGenePack item)
        {
            if (item == null || Unfinished != item || contents.Contains(item)) return false;
            currentItem = null; activeBill = null; selected.Clear(); generation++;
            return true;
        }
        public Bill_GeneFabrication NextBillFor(Pawn pawn)
        {
            foreach (var bill in billStack.Bills.OfType<Bill_GeneFabrication>())
            {
                if (bill == activeBill || !bill.CanBeScheduledFor(pawn) || !bill.genes.All(GeneFabricationResearch.IsUnlocked)) continue;
                if (bill.HasStarted)
                {
                    if (CanRetrieve(bill.unfinished, pawn)) return bill;
                }
                else if (HasAvailable(bill.genes, pawn)) return bill;
            }
            return null;
        }
        public bool CanRetrieve(UnfinishedGenePack item, Pawn pawn) => item != null && !item.Destroyed && item.Valid &&
            !item.cancellationHandled && item.fabricator == this && item.CanBeWorkedBy(pawn) && item.Spawned && item.Map == Map &&
            !item.IsForbidden(pawn) && pawn.CanReserveAndReach(item, PathEndMode.ClosestTouch, Danger.Some);
        public UnfinishedGenePack ParkedManualFor(Pawn pawn) => Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("GF_UnfinishedGenePack"))
            .OfType<UnfinishedGenePack>().FirstOrDefault(item => item != Unfinished && item.bill == null && CanRetrieve(item, pawn));
        public bool TryStartBill(Bill_GeneFabrication bill)
        {
            if (bill == null || !billStack.Bills.Contains(bill) || bill.HasStarted || !bill.ShouldDoNow() || !TryStart(bill.genes)) return false;
            activeBill = bill;
            return true;
        }
        public bool RestoreUnfinished(UnfinishedGenePack item, Pawn pawn)
        {
            if (item == null || !item.Valid || item.cancellationHandled || item.fabricator != this || !item.CanBeWorkedBy(pawn) ||
                (Pending && Unfinished != item) || !item.genes.All(GeneFabricationResearch.IsUnlocked) ||
                (item.bill != null && (!billStack.Bills.Contains(item.bill) || !item.bill.ShouldDoNow() || item.bill.unfinished != item))) return false;
            if (pawn.carryTracker.CarriedThing != item) return false;
            if (pawn.carryTracker.innerContainer.TryTransferToContainer(item, contents, 1, false) != 1) return false;
            currentItem = item; activeBill = item.bill; selected = new List<GeneDef>(item.genes); generation++;
            return PlaceCurrentItem();
        }
        public bool ReleaseUncommitted()
        {
            if (Unfinished != null || !Spawned) return false;
            if (!contents.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near)) return false;
            selected.Clear(); activeBill = null; generation++;
            return true;
        }
        public void CancelItem(UnfinishedGenePack item)
        {
            if (item == null || item.cancellationHandled || !item.Valid ||
                (item.fabricator != null ? item.fabricator != this : Unfinished != item)) return;
            item.cancellationHandled = true; // Persist the receipt before creating any recovery.
            bool current = Unfinished == item;
            var associatedBill = item.bill;
            if (current) { currentItem = null; activeBill = null; selected.Clear(); generation++; }
            int biomass = item.paidBiomass >= 0 ? item.paidBiomass : FabricationCalculations.Biomass(item.genes.Count, Complexity(item.genes));
            int capsules = item.paidCapsules >= 0 ? item.paidCapsules : Archites(item.genes);
            if (FabricationCalculations.CancellationRecovery(biomass) > 0) RecoverMaterial(BiomassDef, FabricationCalculations.CancellationRecovery(biomass));
            if (FabricationCalculations.CancellationRecovery(capsules) > 0) RecoverMaterial(CapsuleDef, FabricationCalculations.CancellationRecovery(capsules));
            contents.Remove(item);
            item.Destroy();
            if (associatedBill != null && !associatedBill.deleted && billStack.Bills.Contains(associatedBill)) billStack.Delete(associatedBill);
        }
        private void RecoverMaterial(ThingDef def, int count)
        {
            while (count > 0)
            {
                int stackCount = Math.Min(count, Math.Max(1, def.stackLimit));
                Thing material = MakeRecoveredMaterial(def, stackCount);
                contents.TryAdd(material, false);
                if (Spawned) contents.TryDrop(material, InteractionCell, Map, ThingPlaceMode.Near, out Thing placed);
                count -= stackCount;
            }
        }
        protected virtual Thing MakeRecoveredMaterial(ThingDef def, int count)
        {
            Thing material = ThingMaker.MakeThing(def); material.stackCount = count; return material;
        }
    }
}
