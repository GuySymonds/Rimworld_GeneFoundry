using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeneFoundry.Components
{
    public class CompBiomassRefiner : ThingComp
    {
        private const int MaxPowerOutageTicks = 5000;

        private int progressTicks;
        private int powerOutageTicks;

        private bool processing;

        private CompProperties_BiomassRefiner Props =>
            (CompProperties_BiomassRefiner)props;

        private CompPowerTrader Power =>
            parent.TryGetComp<CompPowerTrader>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            UpdatePowerConsumption();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref progressTicks, "gfProgressTicks", 0);
            Scribe_Values.Look(ref powerOutageTicks, "gfPowerOutageTicks", 0);
            Scribe_Values.Look(ref processing, "gfProcessing", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!parent.Spawned || Power == null)
                return;

            // Handle an existing batch.
            if (processing)
            {
                // Processing cannot continue without electricity.
                if (!Power.PowerOn)
                {
                    powerOutageTicks++;

                    if (powerOutageTicks >= MaxPowerOutageTicks)
                    {
                        FailBatchDueToPowerLoss();
                    }

                    return;
                }

                // Electricity has been restored.
                // Previous interruptions do not accumulate.
                powerOutageTicks = 0;

                // Continue refining.
                progressTicks++;

                if (progressTicks >= Props.processingTicks)
                {
                    CompleteBatch();
                }

                return;
            }

            // No batch is running.
            if (!Power.PowerOn)
                return;

            // Temporary automatic initiation.
            // Stage 2 will replace this with colonist-operated bills.
            if (TryConsumeInputs())
            {
                processing = true;

                progressTicks = 0;
                powerOutageTicks = 0;

                UpdatePowerConsumption();
            }
        }

        private void FailBatchDueToPowerLoss()
        {
            // Discard the current batch.
            processing = false;

            progressTicks = 0;
            powerOutageTicks = 0;

            // Return to idle electricity consumption.
            UpdatePowerConsumption();

            // Notify the player.
            Messages.Message(
                "GeneFoundry: Biomass refinery batch lost after two hours without power.",
                parent,
                MessageTypeDefOf.NegativeEvent,
                true
            );
        }

        private bool TryConsumeInputs()
        {
            var stacks = new List<Thing>();
            int available = 0;

            CellRect footprint = parent.OccupiedRect();

            foreach (IntVec3 cell in footprint.ExpandedBy(1))
            {
                if (!cell.InBounds(parent.Map) || footprint.Contains(cell))
                    continue;

                // Only accept hoppers touching a side of the refinery.
                bool adjacent =
                    footprint.Contains(cell + IntVec3.North) ||
                    footprint.Contains(cell + IntVec3.South) ||
                    footprint.Contains(cell + IntVec3.East) ||
                    footprint.Contains(cell + IntVec3.West);

                if (!adjacent)
                    continue;

                List<Thing> contents = parent.Map.thingGrid.ThingsListAt(cell);

                // Check whether this cell contains our biomass hopper.
                bool hasHopper = false;

                foreach (Thing thing in contents)
                {
                    if (thing is Building_Storage &&
                        thing.def.defName == "GF_BiomassHopper")
                    {
                        hasHopper = true;
                        break;
                    }
                }

                if (!hasHopper)
                    continue;

                // Collect available biomass from this hopper.
                foreach (Thing thing in contents)
                {
                    if (thing.def != Props.inputDef)
                        continue;

                    stacks.Add(thing);
                    available += thing.stackCount;
                }
            }

            // Never consume ingredients for an incomplete batch.
            if (available < Props.inputCount)
                return false;

            int remaining = Props.inputCount;

            foreach (Thing stack in stacks)
            {
                int amount = Math.Min(remaining, stack.stackCount);

                Thing consumed = stack.SplitOff(amount);
                consumed.Destroy(DestroyMode.Vanish);

                remaining -= amount;

                if (remaining == 0)
                    break;
            }

            return true;
        }

        private void CompleteBatch()
        {
            Thing output = ThingMaker.MakeThing(Props.outputDef);
            output.stackCount = Props.outputCount;

            bool placed = GenPlace.TryPlaceThing(
                output,
                parent.Position,
                parent.Map,
                ThingPlaceMode.Near
            );

            // Keep the completed batch pending if output cannot be placed.
            if (!placed)
                return;

            processing = false;

            progressTicks = 0;
            powerOutageTicks = 0;

            UpdatePowerConsumption();
        }

        private void UpdatePowerConsumption()
        {
            if (Power == null)
                return;

            float watts = processing
                ? Props.activePower
                : Props.idlePower;

            // Consumers use negative power output.
            Power.PowerOutput = -watts;
        }

        public override string CompInspectStringExtra()
        {
            if (!processing)
                return "Status: Waiting for raw biomass";

            int percentage = Math.Min(
                100,
                progressTicks * 100 / Math.Max(1, Props.processingTicks)
            );

            if (Power == null || !Power.PowerOn)
            {
                return
                    $"Status: Paused (no power)\n" +
                    $"Progress: {percentage}%\n" +
                    $"Power outage: {powerOutageTicks}/{MaxPowerOutageTicks} ticks";
            }

            return $"Status: Refining biomass\nProgress: {percentage}%";
        }
    }
}