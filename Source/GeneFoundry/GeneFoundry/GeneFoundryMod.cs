using System;
using UnityEngine;
using Verse;

namespace GeneFoundry
{
    public class GeneFoundrySettings : ModSettings
    {
        public int preparationMinutes = 60;
        public int minimumPreparationMinutes = 30;
        public int processingMinutes = 720;
        public int minimumProcessingMinutes = 360;
        public int processorReductionPercent = 5;
        public float corpseMinutesPer50Material = 24f;
        public float corpseSetupMinutes = 2.4f;
        public int corpseHandlingPercent = 30;
        public int expertSpeedPercent = 200;
        public int expertYieldPercent = 20;
        public int refinedExpertSpeedPercent = 200;
        public int refinedExpertYieldPercent = 20;
        public bool refinementCleanlinessEnabled = true;
        public int refinementDirtyPenalty = 15;
        public int refinementSterileReduction = 5;
        public int refinementExposurePenalty = 15;
        public int geneFabricationResearchCost = 1500;
        public int geneCategoryBaseCost = 500;
        public int geneResearchPerGene = 100;
        public int geneResearchPerComplexity = 200;
        public int geneResearchPerArchite = 500;
        public int geneCategoryCostPercent = 25;
        public int geneCategoryMaximumCost = 4000;

        public void Validate()
        {
            preparationMinutes = Math.Max(1, Math.Min(120, preparationMinutes));
            minimumPreparationMinutes = Math.Max(1, Math.Min(preparationMinutes, minimumPreparationMinutes));
            processingMinutes = Math.Max(5, Math.Min(1440, processingMinutes));
            minimumProcessingMinutes = Math.Max(5, Math.Min(processingMinutes, minimumProcessingMinutes));
            processorReductionPercent = Math.Max(0, Math.Min(25, processorReductionPercent));
            corpseMinutesPer50Material = Math.Max(1f, Math.Min(120f, corpseMinutesPer50Material));
            corpseSetupMinutes = Math.Max(0f, Math.Min(15f, corpseSetupMinutes));
            corpseHandlingPercent = Math.Max(0, Math.Min(100, corpseHandlingPercent));
            expertSpeedPercent = Math.Max(150, Math.Min(300, expertSpeedPercent));
            expertYieldPercent = Math.Max(0, Math.Min(50, expertYieldPercent));
            refinedExpertSpeedPercent = Math.Max(150, Math.Min(300, refinedExpertSpeedPercent));
            refinedExpertYieldPercent = Math.Max(0, Math.Min(50, refinedExpertYieldPercent));
            refinementDirtyPenalty = Math.Max(0, Math.Min(50, refinementDirtyPenalty));
            refinementSterileReduction = Math.Max(0, Math.Min(30, refinementSterileReduction));
            refinementExposurePenalty = Math.Max(0, Math.Min(50, refinementExposurePenalty));
            geneFabricationResearchCost = Math.Max(100, Math.Min(10000, geneFabricationResearchCost));
            geneCategoryBaseCost = Math.Max(0, Math.Min(5000, geneCategoryBaseCost));
            geneResearchPerGene = Math.Max(0, Math.Min(1000, geneResearchPerGene));
            geneResearchPerComplexity = Math.Max(0, Math.Min(2000, geneResearchPerComplexity));
            geneResearchPerArchite = Math.Max(0, Math.Min(5000, geneResearchPerArchite));
            geneCategoryCostPercent = Math.Max(1, Math.Min(200, geneCategoryCostPercent));
            geneCategoryMaximumCost = Math.Max(100, Math.Min(10000, geneCategoryMaximumCost));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref preparationMinutes, "preparationMinutes", 60);
            Scribe_Values.Look(ref minimumPreparationMinutes, "minimumPreparationMinutes", 30);
            Scribe_Values.Look(ref processingMinutes, "processingMinutes", 720);
            Scribe_Values.Look(ref minimumProcessingMinutes, "minimumProcessingMinutes", 360);
            Scribe_Values.Look(ref processorReductionPercent, "processorReductionPercent", 5);
            Scribe_Values.Look(ref corpseMinutesPer50Material, "corpseMinutesPer50Material", 24f);
            Scribe_Values.Look(ref corpseSetupMinutes, "corpseSetupMinutes", 2.4f);
            Scribe_Values.Look(ref corpseHandlingPercent, "corpseHandlingPercent", 30);
            Scribe_Values.Look(ref expertSpeedPercent, "expertSpeedPercent", 200);
            Scribe_Values.Look(ref expertYieldPercent, "expertYieldPercent", 20);
            Scribe_Values.Look(ref refinedExpertSpeedPercent, "refinedExpertSpeedPercent", 200);
            Scribe_Values.Look(ref refinedExpertYieldPercent, "refinedExpertYieldPercent", 20);
            Scribe_Values.Look(ref refinementCleanlinessEnabled, "refinementCleanlinessEnabled", true);
            Scribe_Values.Look(ref refinementDirtyPenalty, "refinementDirtyPenalty", 15);
            Scribe_Values.Look(ref refinementSterileReduction, "refinementSterileReduction", 5);
            Scribe_Values.Look(ref refinementExposurePenalty, "refinementExposurePenalty", 15);
            Scribe_Values.Look(ref geneFabricationResearchCost, "geneFabricationResearchCost", 1500);
            Scribe_Values.Look(ref geneCategoryBaseCost, "geneCategoryBaseCost", 500);
            Scribe_Values.Look(ref geneResearchPerGene, "geneResearchPerGene", 100);
            Scribe_Values.Look(ref geneResearchPerComplexity, "geneResearchPerComplexity", 200);
            Scribe_Values.Look(ref geneResearchPerArchite, "geneResearchPerArchite", 500);
            Scribe_Values.Look(ref geneCategoryCostPercent, "geneCategoryCostPercent", 25);
            Scribe_Values.Look(ref geneCategoryMaximumCost, "geneCategoryMaximumCost", 4000);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Validate();
        }
    }

    public class GeneFoundryMod : Mod
    {
        public static GeneFoundrySettings Settings { get; private set; } = new GeneFoundrySettings();
        private Vector2 scrollPosition;
        private float contentHeight = 1000f;

        public GeneFoundryMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GeneFoundrySettings>();
        }

        public override string SettingsCategory() => "GeneFoundry";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            var viewRect = new Rect(0f, 0f, inRect.width - 20f, contentHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            // Allow the listing to measure its full height rather than starting a new column.
            listing.Begin(new Rect(0f, 0f, viewRect.width, 10000f));
            Settings.Validate();
            listing.Label("All times are in-game time. Hover over sliders for explanations. Settings apply across saves.");
            listing.GapLine();
            listing.Label("Raw biomass — Gene Vat");
            listing.Label("All extraction uses Cooking 40%, Medicine 40%, Intellectual 20%. Speed is 100% at level 5 and 150% at 10. Skills 11–20 add tapered speed and better biomass recovery.");
            Settings.expertSpeedPercent = (int)Math.Round(listing.SliderLabeled(
                $"Maximum extraction speed at level 20: {Settings.expertSpeedPercent}%",
                Settings.expertSpeedPercent, 150, 300, 0.75f,
                "Skill multiplier when all three skills reach 20. Default: 200%. Levels 11–20 scale linearly from the fixed 150% at level 10. Each skill contributes independently. Set 150% to disable expert speed gains. General work speed and capacities apply separately. Range: 150–300%."));
            Settings.expertYieldPercent = (int)Math.Round(listing.SliderLabeled(
                $"Maximum extra biomass at level 20: +{Settings.expertYieldPercent}%",
                Settings.expertYieldPercent, 0, 50, 0.75f,
                "Extra biomass earned only through levels 11–20, weighted Cooking 40%, Medicine 40%, Intellectual 20%. Default: +20%. Applies to meat, enzymatic, crop and corpse extraction, after material and decay calculations. Does not multiply horns, tusks or recovered implants. Set 0% to disable. Range: 0–50%."));
            Settings.corpseMinutesPer50Material = (float)Math.Round(listing.SliderLabeled(
                $"Fresh extraction per 50 material: {Settings.corpseMinutesPer50Material:0} min",
                Settings.corpseMinutesPer50Material, 1f, 120f, 0.75f,
                "Base extraction time for 50 units of biological material at 100% work speed, before setup and large-body efficiency. Default: 24 minutes. Lower values speed up all corpse extraction. Range: 1–120 minutes."));
            Settings.corpseSetupMinutes = (float)Math.Round(listing.SliderLabeled(
                $"Setup time per corpse: {Settings.corpseSetupMinutes:0.#} min",
                Settings.corpseSetupMinutes, 0f, 15f, 0.75f,
                "Added to fresh extraction time before large-body efficiency, decay and operator speed are applied. Default: 2.4 minutes. Keeps many small bodies less efficient than one large body. Range: 0–15 minutes, in 0.1-minute steps.") * 10f) / 10f;
            Settings.corpseHandlingPercent = (int)Math.Round(listing.SliderLabeled(
                $"Work unaffected by decay: {Settings.corpseHandlingPercent}%",
                Settings.corpseHandlingPercent, 0, 100, 0.75f,
                "Share of total fresh-corpse work retained regardless of decay, representing handling and structural breakdown. The remaining work scales with recoverable yield. Default: 30%. At 0%, time follows yield; at 100%, decay never reduces time."));
            float handling = Settings.corpseHandlingPercent / 100f;
            listing.Label($"Current decay timings: fresh 100%, rotting {(handling + (1f - handling) * 0.5f) * 100f:0.#}%, desiccated {(handling + (1f - handling) * 0.1f) * 100f:0.#}% of fresh time.");
            listing.Label("Yields remain 100% / 50% / 10%, with at least 1 biomass. Large bodies receive up to 25% less work. Operator speed also affects duration.");
            listing.Label("Corpse timing changes apply when extraction begins. Existing jobs keep their saved duration. Meat and crop bills are unaffected.");
            listing.Label("Expert speed changes affect ongoing meat/crop work; corpse duration stays fixed once extraction starts. Expert yield is evaluated at completion for all recipes.");
            if (listing.ButtonText("Restore raw biomass defaults"))
            {
                Settings.corpseMinutesPer50Material = 24f;
                Settings.corpseSetupMinutes = 2.4f;
                Settings.corpseHandlingPercent = 30;
                Settings.expertSpeedPercent = 200;
                Settings.expertYieldPercent = 20;
            }
            listing.GapLine();
            listing.Label("Refined biomass — Biomass Refinery");
            Settings.preparationMinutes = DurationSlider(listing,
                "Preparation: minimum-skilled operator", Settings.preparationMinutes, 1, 120, 1,
                "Time spent preparing a batch at Intellectual 5, Crafting 3 and Medicine 2. Range: 1–120 minutes.");
            Settings.minimumPreparationMinutes = DurationSlider(listing,
                "Preparation at Intellectual 10 / Crafting 8 / Medicine 6", Settings.minimumPreparationMinutes, 1, Settings.preparationMinutes, 1,
                "Preparation time at the zero-failure skill thresholds, before expert speed gains. Higher skills can shorten preparation further. Cannot exceed the minimum-skilled preparation time.");
            Settings.refinedExpertSpeedPercent = (int)Math.Round(listing.SliderLabeled(
                $"Extra preparation speed at level 20: +{(Settings.refinedExpertSpeedPercent / 150f - 1f) * 100f:0.#}%",
                Settings.refinedExpertSpeedPercent, 150, 300, 0.75f,
                "Additional preparation speed above Intellectual 10, Crafting 8 and Medicine 6, reaching its maximum when all three skills reach 20. Weights: 60%, 25%, 15%. Default: +33.3% speed, making a 30-minute preparation take 22.5 minutes. Range: +0–100% speed; +0 disables this bonus. Automated processing remains controlled by gene processors."));
            Settings.refinedExpertYieldPercent = (int)Math.Round(listing.SliderLabeled(
                $"Maximum extra refined biomass at level 20: +{Settings.refinedExpertYieldPercent}%",
                Settings.refinedExpertYieldPercent, 0, 50, 0.75f,
                "Yield bonus earned above Intellectual 10, Crafting 8 and Medicine 6, independently weighted 60%, 25%, 15%, capped at skill 20. Default: +20%. With a base output of 2 this gives 2 guaranteed items plus a 40% chance of a third on success. Captured when processing starts. Range: 0–50%; 0 disables expert yield."));
            listing.Label($"At level 20, preparation takes {Settings.minimumPreparationMinutes * 150d / Settings.refinedExpertSpeedPercent:0.#} minutes with these settings. Fractional refined biomass becomes a chance of one extra item.");
            Settings.processingMinutes = DurationSlider(listing,
                "Processing: no gene processors", Settings.processingMinutes, 5, 1440, 5,
                "Automated processing time without processor assistance. Range: 5 minutes–24 hours, in 5-minute steps.");
            Settings.minimumProcessingMinutes = DurationSlider(listing,
                "Processing: shortest allowed time", Settings.minimumProcessingMinutes, 5, Settings.processingMinutes, 5,
                "Gene processors cannot reduce processing below this duration. Cannot exceed the unassisted processing time.");
            Settings.processorReductionPercent = (int)Math.Round(listing.SliderLabeled(
                $"Time reduction per gene processor: {Settings.processorReductionPercent}%",
                Settings.processorReductionPercent, 0, 25, 0.75f,
                "Each available processor removes this percentage of the unassisted duration. Reductions add together until the shortest allowed time is reached. 0% disables assistance. Range: 0–25% in 1% steps."));
            listing.Label("Processing settings affect running batches. Preparation settings apply to new preparation jobs.");
            listing.Label("Active gene assemblers have priority. Each powered processor assists at most one refinery.");
            listing.GapLine();
            listing.Label("Refinement cleanliness and exposure");
            listing.CheckboxLabeled("Room cleanliness affects batch failure risk", ref Settings.refinementCleanlinessEnabled,
                "Add room cleanliness or exposure risk to the operator's failure chance. Applies only to refinement; artificial-part recovery still ignores cleanliness. Changes affect new batches only.");
            if (Settings.refinementCleanlinessEnabled)
            {
                Settings.refinementDirtyPenalty = (int)Math.Round(listing.SliderLabeled(
                    $"Very dirty room (cleanliness -2): +{Settings.refinementDirtyPenalty} risk points",
                    Settings.refinementDirtyPenalty, 0, 50, 0.75f,
                    "Maximum added failure probability, in percentage points, at cleanliness -2 or worse. Scales linearly to zero at cleanliness 0. Default: +15 points; -1 gives +7.5. Range: 0–50 points."));
                Settings.refinementSterileReduction = (int)Math.Round(listing.SliderLabeled(
                    $"Sterile room (cleanliness +0.6): -{Settings.refinementSterileReduction} risk points",
                    Settings.refinementSterileReduction, 0, 30, 0.75f,
                    "Maximum reduction in failure probability at cleanliness +0.6 or better. Scales linearly from zero at cleanliness 0. Default: -5 percentage points. Final failure chance cannot fall below zero. Range: 0–30 points."));
                Settings.refinementExposurePenalty = (int)Math.Round(listing.SliderLabeled(
                    $"Outdoor or unroofed exposure: +{Settings.refinementExposurePenalty} risk points",
                    Settings.refinementExposurePenalty, 0, 50, 0.75f,
                    "Fixed added failure probability when the refinery is outside an enclosed, fully roofed room. Replaces the room cleanliness adjustment; the two penalties do not stack. Default: +15 percentage points. Range: 0–50 points."));
            }
            listing.Label("Risk = operator risk + room adjustment, clamped to 0–100%. Captured when processing starts; later dirt, cleaning or settings changes do not alter that batch. Speed and yield are unaffected.");
            if (listing.ButtonText("Restore refined biomass defaults"))
            {
                Settings.preparationMinutes = 60;
                Settings.minimumPreparationMinutes = 30;
                Settings.processingMinutes = 720;
                Settings.minimumProcessingMinutes = 360;
                Settings.processorReductionPercent = 5;
                Settings.refinedExpertSpeedPercent = 200;
                Settings.refinedExpertYieldPercent = 20;
                Settings.refinementCleanlinessEnabled = true;
                Settings.refinementDirtyPenalty = 15;
                Settings.refinementSterileReduction = 5;
                Settings.refinementExposurePenalty = 15;
            }
            listing.GapLine();
            listing.Label("Gene research — Fabrication and categories");
            listing.Label("Category costs combine the base, gene count, complexity and archite terms below, then apply the category cost percentage and maximum. Round up to whole points, with at least 1 point. All loaded genes count; negative complexity or archite values contribute zero.");
            Settings.geneCategoryCostPercent = (int)Math.Round(listing.SliderLabeled(
                $"Category research cost percentage: {Settings.geneCategoryCostPercent}%",
                Settings.geneCategoryCostPercent, 1, 200, 0.75f,
                "Scales the entire category calculation before the maximum is applied. Default: 25%, one quarter of the original costs. 100% restores the original scale. Range: 1–200%. Does not change the fabrication foundation or remove Archogenetics requirements."));
            Settings.geneCategoryMaximumCost = ResearchCostSlider(listing,
                "Maximum research cost per category", Settings.geneCategoryMaximumCost, 100, 10000, 100,
                "Hard limit applied after the category percentage and rounding. Default: 4,000 points. Range: 100–10,000 in 100-point steps. Large categories can reach the same maximum; gene count and complexity still distinguish categories below it. Does not cap the fabrication foundation.");
            listing.Label("Restart RimWorld after changing these costs. Research points already earned remain unchanged; raising costs can require additional research, including for previously completed projects. Earlier biomass research costs are unaffected.");
            Settings.geneFabricationResearchCost = ResearchCostSlider(listing,
                "Gene fabrication foundation", Settings.geneFabricationResearchCost, 100, 10000, 100,
                "Cost of the foundation project after Biomass refinement. Default: 1,500 points. Range: 100–10,000, in 100-point steps.");
            Settings.geneCategoryBaseCost = ResearchCostSlider(listing,
                "Base cost per gene category", Settings.geneCategoryBaseCost, 0, 5000, 50,
                "Fixed cost added once per category. Default: 500 points. Range: 0–5,000, in 50-point steps. Final category cost is always at least 1 point.");
            Settings.geneResearchPerGene = ResearchCostSlider(listing,
                "Additional cost per gene", Settings.geneResearchPerGene, 0, 1000, 25,
                "Added for every gene in the category, including zero-complexity cosmetic genes. Default: 100 points per gene. Range: 0–1,000, in 25-point steps.");
            Settings.geneResearchPerComplexity = ResearchCostSlider(listing,
                "Additional cost per complexity point", Settings.geneResearchPerComplexity, 0, 2000, 25,
                "Multiplies the sum of non-negative gene complexity in the category, not the average. Default: 200 points per complexity. Range: 0–2,000, in 25-point steps.");
            Settings.geneResearchPerArchite = ResearchCostSlider(listing,
                "Additional cost per archite value", Settings.geneResearchPerArchite, 0, 5000, 50,
                "Multiplies the sum of non-negative archite values in the category. Default: 500 points per archite value. Range: 0–5,000, in 50-point steps. Setting zero removes the surcharge but never removes the vanilla Archogenetics requirement.");
            if (listing.ButtonText("Restore gene research defaults"))
            {
                Settings.geneFabricationResearchCost = 1500;
                Settings.geneCategoryBaseCost = 500;
                Settings.geneResearchPerGene = 100;
                Settings.geneResearchPerComplexity = 200;
                Settings.geneResearchPerArchite = 500;
                Settings.geneCategoryCostPercent = 25;
                Settings.geneCategoryMaximumCost = 4000;
            }
            contentHeight = Math.Max(inRect.height, listing.CurHeight + 20f);
            listing.End();
            Widgets.EndScrollView();
        }

        public override void WriteSettings()
        {
            Settings.Validate();
            base.WriteSettings();
        }

        private static int DurationSlider(Listing_Standard listing, string label,
            int value, int minimum, int maximum, int step, string tooltip)
        {
            value = Math.Max(minimum, Math.Min(maximum, value));
            float selected = listing.SliderLabeled(
                $"{label}: {FormatDuration(value)}", value, minimum, maximum, 0.75f, tooltip);
            return Math.Max(minimum, Math.Min(maximum, (int)Math.Round(selected / step) * step));
        }

        private static int ResearchCostSlider(Listing_Standard listing, string label,
            int value, int minimum, int maximum, int step, string tooltip)
        {
            float selected = listing.SliderLabeled($"{label}: {value:N0} points",
                value, minimum, maximum, 0.75f, tooltip);
            return Math.Max(minimum, Math.Min(maximum, (int)Math.Round(selected / step) * step));
        }

        private static string FormatDuration(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} min";
            int remainder = minutes % 60;
            return remainder == 0 ? $"{minutes / 60} h" : $"{minutes / 60} h {remainder} min";
        }
    }
}
