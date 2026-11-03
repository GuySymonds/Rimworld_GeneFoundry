using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using GeneFoundry.Recipes;

namespace GeneFoundry.Components
{
    public class CompBiomassRefiner : ThingComp
    {
        private const int MaxPowerOutageTicks = 5000;

        private double batchProgress;
        private int powerOutageTicks;

        private float batchFailureChance;
        private float batchOperatorRisk;
        private float batchRoomCleanliness;
        private float batchCleanlinessAdjustment;
        private bool batchExposed;
        private bool batchCleanlinessEnabled;
        private bool completionRollMade;

        private bool processing;
        private bool vatHasProcessed;
        private int vatStartedAt = -1;

        // Output placement can remain pending after the reaction has finished.
        public int VatFrameAt(int tick) => BiomassRefineryAnimation.FrameAt(
            processing && batchProgress < 1d, vatHasProcessed, tick - vatStartedAt);

        internal void TickVatAnimation(bool powered)
        {
            // Move the origin with the game clock during outages, preserving even
            // partial-frame progress. The existing saved timestamp is sufficient.
            if (!powered) vatStartedAt++;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            BiomassRefineryOverlay.Draw(parent, VatFrameAt(Find.TickManager.TicksGame));
        }

        private CompProperties_BiomassRefiner Props =>
            (CompProperties_BiomassRefiner)props;

        private CompPowerTrader Power =>
            parent.TryGetComp<CompPowerTrader>();

        public bool AcceptsProcessorAssistance =>
            processing && parent.Spawned && Power != null && Power.PowerOn;

        private int AssistingProcessors => parent.Spawned
            ? parent.Map.GetComponent<GeneProcessorAssistance>().ProcessorCount(parent)
            : 0;

        private float ProcessingDurationTicks(int processors)
        {
            GeneFoundrySettings settings = GeneFoundryMod.Settings;
            float baseMinutes = Math.Max(1, settings.processingMinutes);
            float minimumMinutes = Math.Max(1, Math.Min(baseMinutes, settings.minimumProcessingMinutes));
            float reduction = Math.Max(0, Math.Min(100, settings.processorReductionPercent)) / 100f;
            return Math.Max(minimumMinutes, baseMinutes * (1f - processors * reduction)) * 2500f / 60f;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // An older active save has no visual timestamp: resume mixing, not filling.
            if (processing && vatStartedAt < 0)
            {
                vatHasProcessed = true;
                vatStartedAt = Find.TickManager.TicksGame - BiomassRefineryAnimation.FillingDuration;
            }

            UpdatePowerConsumption();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref batchProgress, "gfBatchProgress", -1d);
            if (Scribe.mode == LoadSaveMode.LoadingVars && batchProgress < 0d)
            {
                // Older saves stored elapsed ticks against the old XML duration.
                int oldProgress = 0;
                Scribe_Values.Look(ref oldProgress, "gfProgressTicks", 0);
                batchProgress = Math.Min(1d, oldProgress / (double)Math.Max(1, Props.processingTicks));
            }
            Scribe_Values.Look(ref powerOutageTicks, "gfPowerOutageTicks", 0);
            Scribe_Values.Look(ref processing, "gfProcessing", false);
            Scribe_Values.Look(ref vatHasProcessed, "gfVatHasProcessed", false);
            Scribe_Values.Look(ref vatStartedAt, "gfVatStartedAt", -1);

            Scribe_Values.Look(ref batchFailureChance, "gfBatchFailureChance", 0f);
            Scribe_Values.Look(ref batchOperatorRisk, "gfBatchOperatorRisk", 0f);
            Scribe_Values.Look(ref batchRoomCleanliness, "gfBatchRoomCleanliness", 0f);
            Scribe_Values.Look(ref batchCleanlinessAdjustment, "gfBatchCleanlinessAdjustment", 0f);
            Scribe_Values.Look(ref batchExposed, "gfBatchExposed", false);
            Scribe_Values.Look(ref batchCleanlinessEnabled, "gfBatchCleanlinessEnabled", true);
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
                TickVatAnimation(Power.PowerOn);
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
                batchProgress = Math.Min(1d, batchProgress + 1d / ProcessingDurationTicks(AssistingProcessors));

                if (batchProgress >= 1f)
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

            batchProgress = 0f;
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
            batchOperatorRisk = operatorPawn == null
                ? 0f
                : CalculateFailureChance(operatorPawn);
            batchCleanlinessEnabled = operatorPawn != null && GeneFoundryMod.Settings.refinementCleanlinessEnabled;
            batchCleanlinessAdjustment = EnvironmentAdjustment(batchCleanlinessEnabled,
                out batchRoomCleanliness, out batchExposed);
            batchFailureChance = BiomassCalculations.CombinedRefinementFailure(batchOperatorRisk, batchCleanlinessAdjustment);
            completionRollMade = false;

            // Begin automated processing.
            processing = true;
            vatHasProcessed = true;
            vatStartedAt = Find.TickManager.TicksGame;
            batchProgress = 0f;
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
                    batchProgress = 0f;
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

            // Fixed recovery requires no yield roll, including output-placement retries.
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
            {
                output.Destroy(DestroyMode.Vanish);
                return;
            }

            processing = false;

            batchProgress = 0f;
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
            string environmentInfo = !batchCleanlinessEnabled ? "Cleanliness influence: disabled for this batch" :
                batchExposed ? $"Outdoor/unroofed exposure: +{batchCleanlinessAdjustment * 100f:0.#} percentage points" :
                $"Captured room cleanliness: {batchRoomCleanliness:0.##} ({batchCleanlinessAdjustment * 100f:+0.#;-0.#;0} percentage points)";
            string failureInfo = $"Operator risk: {batchOperatorRisk * 100f:0.#}%\n" + environmentInfo + "\n" +
                $"Batch failure chance: {batchFailureChance * 100f:0.#}%\n" +
                $"Expected output on success: {Props.outputCount} refined biomass";

            if (!processing)
            {
                if (Power == null || !Power.PowerOn)
                    return "Status: Idle (no power)";

                if (!CanStartBatch())
                    return "Status: Idle (insufficient raw biomass)";

                float adjustment = EnvironmentAdjustment(GeneFoundryMod.Settings.refinementCleanlinessEnabled,
                    out float cleanliness, out bool exposed);
                string roomInfo = !GeneFoundryMod.Settings.refinementCleanlinessEnabled ? "Cleanliness influence disabled" :
                    exposed ? "Outdoor/unroofed exposure" : $"Room cleanliness: {cleanliness:0.##}";
                return $"Status: Ready for preparation\n{roomInfo}: {adjustment * 100f:+0.#;-0.#;0} risk percentage points";
            }

            int percentage = Math.Min(100, (int)(batchProgress * 100f));
            int processors = AssistingProcessors;
            float duration = ProcessingDurationTicks(processors);
            string timingInfo =
                $"Assisting gene processors: {processors}\n" +
                $"Processing duration at current speed: {duration / 2500f:0.##} hours\n" +
                $"Time remaining at current speed: {(1f - batchProgress) * duration / 2500f:0.##} hours";

            if (Power == null || !Power.PowerOn)
            {
                return
                    $"Status: Paused (no power)\n" +
                    $"Progress: {percentage}%\n" +
                    $"Power outage: {powerOutageTicks}/{MaxPowerOutageTicks} ticks\n" +
                    failureInfo + "\n" + timingInfo;
            }

            return
                $"Status: Refining biomass\n" +
                $"Progress: {percentage}%\n" +
                failureInfo + "\n" + timingInfo;
        }

        public static float CalculateFailureChance(Pawn pawn)
        {
            return BiomassCalculations.RefinementFailureChance(
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Level,
                pawn.skills.GetSkill(SkillDefOf.Crafting).Level,
                pawn.skills.GetSkill(SkillDefOf.Medicine).Level);
        }

        private float EnvironmentAdjustment(bool enabled, out float cleanliness, out bool exposed)
        {
            Room room = parent.GetRoom();
            exposed = room == null || room.OutdoorsForWork || room.TouchesMapEdge || room.OpenRoofCount > 0;
            cleanliness = exposed ? 0f : room.GetStat(RoomStatDefOf.Cleanliness);
            GeneFoundrySettings settings = GeneFoundryMod.Settings;
            return BiomassCalculations.RefinementCleanlinessAdjustment(cleanliness, exposed, enabled,
                settings.refinementDirtyPenalty, settings.refinementExposurePenalty);
        }

        public static int CalculatePreparationTicks(Pawn pawn)
        {
            return BiomassCalculations.RefinementPreparationTicks(
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Level,
                pawn.skills.GetSkill(SkillDefOf.Crafting).Level,
                pawn.skills.GetSkill(SkillDefOf.Medicine).Level);
        }
    }
}
