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

        private float batchFailureChance;
        private bool completionRollMade;

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

            Scribe_Values.Look(ref batchFailureChance, "gfBatchFailureChance", 0f);
            Scribe_Values.Look(ref completionRollMade, "gfCompletionRollMade", false);
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

        public bool CanStartBatch()
        {
            // Another batch must not be running.
            if (!parent.Spawned || processing)
                return false;

            // Refinement requires electricity.
            if (Power == null || !Power.PowerOn)
                return false;

            // Check biomass availability without consuming it.
            return TryConsumeInputs(false);
        }

        public bool TryStartBatch(Pawn operatorPawn = null)
        {
            if (!CanStartBatch())
                return false;

            // Consume the required biomass from adjacent hoppers.
            if (!TryConsumeInputs())
                return false;

            // Capture the operator's skills once for this batch.
            // The developer command starts a zero-risk test batch.
            batchFailureChance = operatorPawn == null
                ? 0f
                : CalculateFailureChance(operatorPawn);
            completionRollMade = false;

            // Begin automated processing.
            processing = true;
            progressTicks = 0;
            powerOutageTicks = 0;

            UpdatePowerConsumption();

            return true;
        }

        private bool TryConsumeInputs(bool consume = true)
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
                        thing.def.defName == "GF_BiomassHopper" &&
                        footprint.Contains(thing.Position + thing.Rotation.FacingCell))
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

            // Allow callers to check availability without consuming anything.
            if (!consume)
                return true;

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
            // Roll only once, even if successful output placement must be retried.
            if (!completionRollMade)
            {
                completionRollMade = true;

                if (Rand.Chance(batchFailureChance))
                {
                    processing = false;
                    progressTicks = 0;
                    powerOutageTicks = 0;
                    UpdatePowerConsumption();

                    Messages.Message(
                        "GeneFoundry: Biomass refinement failed. The batch was lost.",
                        parent,
                        MessageTypeDefOf.NegativeEvent,
                        true);

                    return;
                }
            }

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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            // Allow players to build hoppers directly from the refinery.
            if (parent.Spawned && parent.Faction == Faction.OfPlayer)
            {
                ThingDef hopperDef =
                    DefDatabase<ThingDef>.GetNamed("GF_BiomassHopper");

                yield return new Command_Action
                {
                    defaultLabel = "Build biomass hopper",

                    defaultDesc =
                        "Construct a raw biomass hopper to supply this refinery.",

                    icon = hopperDef.uiIcon,

                    action = () =>
                    {
                        Find.DesignatorManager.Select(
                            new Designator_Build(hopperDef)
                        );
                    }
                };
            }

            if (!Prefs.DevMode)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "DEV: Start refinement",
                defaultDesc = "Manually initiates a biomass refinement batch for testing.",

                action = () =>
                {
                    if (!TryStartBatch())
                    {
                        Messages.Message(
                            "GeneFoundry: Cannot start refinement. Check power, biomass and refinery status.",
                            parent,
                            MessageTypeDefOf.NegativeEvent,
                            false
                        );
                    }
                }
            };
        }

        public override string CompInspectStringExtra()
        {
            string failureInfo = $"Batch failure chance: {batchFailureChance * 100f:0.#}%";

            if (!processing)
            {
                if (Power == null || !Power.PowerOn)
                    return "Status: Idle (no power)";

                if (!CanStartBatch())
                    return "Status: Idle (insufficient raw biomass)";

                return "Status: Ready for preparation";
            }

            int percentage = Math.Min(
                100,
                progressTicks * 100 / Math.Max(1, Props.processingTicks)
            );

            if (Power == null || !Power.PowerOn)
            {
                return
                    $"Status: Paused (no power)\n" +
                    $"Progress: {percentage}%\n" +
                    $"Power outage: {powerOutageTicks}/{MaxPowerOutageTicks} ticks\n" +
                    failureInfo;
            }

            return
                $"Status: Refining biomass\n" +
                $"Progress: {percentage}%\n" +
                failureInfo;
        }

        private static float SkillProgress(
            Pawn pawn,
            SkillDef skill,
            int minimum,
            int zeroFailureLevel)
        {
            int level = pawn.skills.GetSkill(skill).Level;

            return Math.Max(
                0f,
                Math.Min(1f, (level - minimum) / (float)(zeroFailureLevel - minimum))
            );
        }

        public static float CalculateFailureChance(Pawn pawn)
        {
            float weightedProgress =
                0.60f * SkillProgress(pawn, SkillDefOf.Intellectual, 5, 10) +
                0.25f * SkillProgress(pawn, SkillDefOf.Crafting, 3, 8) +
                0.15f * SkillProgress(pawn, SkillDefOf.Medicine, 2, 6);

            return 0.30f * (1f - weightedProgress);
        }
    }
}
