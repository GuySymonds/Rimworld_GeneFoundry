using System;
using UnityEngine;
using Verse;

namespace GeneFoundry
{
    public class GeneFoundrySettings : ModSettings
    {
        public int processingMinutes = 2160;
        public int minimumProcessingMinutes = 1080;
        public int processorReductionPercent = 5;
        public float corpseMinutesPer50Material = 24f;
        public float corpseSetupMinutes = 2.4f;
        public int corpseHandlingPercent = 30;
        public int expertSpeedPercent = 200;
        public bool refinementCleanlinessEnabled = true;
        public int refinementDirtyPenalty = 15;
        public int refinementExposurePenalty = 10;
        public int dimLightPenalty = 10;
        public int geneFabricationResearchCost = 1500;
        public int geneCategoryBaseCost = 500;
        public int geneResearchPerGene = 100;
        public int geneResearchPerComplexity = 200;
        public int geneResearchPerArchite = 500;
        public int geneCategoryCostPercent = 25;
        public int geneCategoryMaximumCost = 4000;
        public int geneCategoryMinimumCost = 1000;

        public void Validate()
        {
            processingMinutes = Math.Max(5, Math.Min(4320, processingMinutes));
            minimumProcessingMinutes = Math.Max(5, Math.Min(processingMinutes, minimumProcessingMinutes));
            processorReductionPercent = Math.Max(0, Math.Min(25, processorReductionPercent));
            corpseMinutesPer50Material = Math.Max(1f, Math.Min(120f, corpseMinutesPer50Material));
            corpseSetupMinutes = Math.Max(0f, Math.Min(15f, corpseSetupMinutes));
            corpseHandlingPercent = Math.Max(0, Math.Min(100, corpseHandlingPercent));
            expertSpeedPercent = Math.Max(150, Math.Min(300, expertSpeedPercent));
            refinementDirtyPenalty = Math.Max(0, Math.Min(50, refinementDirtyPenalty));
            refinementExposurePenalty = Math.Max(0, Math.Min(50, refinementExposurePenalty));
            dimLightPenalty = Math.Max(0, Math.Min(50, dimLightPenalty));
            geneFabricationResearchCost = Math.Max(100, Math.Min(10000, geneFabricationResearchCost));
            geneCategoryBaseCost = Math.Max(0, Math.Min(5000, geneCategoryBaseCost));
            geneResearchPerGene = Math.Max(0, Math.Min(1000, geneResearchPerGene));
            geneResearchPerComplexity = Math.Max(0, Math.Min(2000, geneResearchPerComplexity));
            geneResearchPerArchite = Math.Max(0, Math.Min(5000, geneResearchPerArchite));
            geneCategoryCostPercent = Math.Max(1, Math.Min(200, geneCategoryCostPercent));
            geneCategoryMinimumCost = Math.Max(100, Math.Min(10000, geneCategoryMinimumCost));
            geneCategoryMaximumCost = Math.Max(geneCategoryMinimumCost, Math.Min(10000, geneCategoryMaximumCost));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref processingMinutes, "processingMinutes", 2160);
            Scribe_Values.Look(ref minimumProcessingMinutes, "minimumProcessingMinutes", 1080);
            Scribe_Values.Look(ref processorReductionPercent, "processorReductionPercent", 5);
            Scribe_Values.Look(ref corpseMinutesPer50Material, "corpseMinutesPer50Material", 24f);
            Scribe_Values.Look(ref corpseSetupMinutes, "corpseSetupMinutes", 2.4f);
            Scribe_Values.Look(ref corpseHandlingPercent, "corpseHandlingPercent", 30);
            Scribe_Values.Look(ref expertSpeedPercent, "expertSpeedPercent", 200);
            Scribe_Values.Look(ref refinementCleanlinessEnabled, "refinementCleanlinessEnabled", true);
            Scribe_Values.Look(ref refinementDirtyPenalty, "refinementDirtyPenalty", 15);
            Scribe_Values.Look(ref refinementExposurePenalty, "refinementExposurePenalty", 10);
            Scribe_Values.Look(ref dimLightPenalty, "dimLightPenalty", 10);
            Scribe_Values.Look(ref geneFabricationResearchCost, "geneFabricationResearchCost", 1500);
            Scribe_Values.Look(ref geneCategoryBaseCost, "geneCategoryBaseCost", 500);
            Scribe_Values.Look(ref geneResearchPerGene, "geneResearchPerGene", 100);
            Scribe_Values.Look(ref geneResearchPerComplexity, "geneResearchPerComplexity", 200);
            Scribe_Values.Look(ref geneResearchPerArchite, "geneResearchPerArchite", 500);
            Scribe_Values.Look(ref geneCategoryCostPercent, "geneCategoryCostPercent", 25);
            Scribe_Values.Look(ref geneCategoryMaximumCost, "geneCategoryMaximumCost", 4000);
            Scribe_Values.Look(ref geneCategoryMinimumCost, "geneCategoryMinimumCost", 1000);
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
            listing.Label("Extraction weights: Cooking 40%, Medicine 40%, Intellectual 20%. References 10/6/4 give full yield and normal speed. Below-reference skills cause random wastage; above-reference skills improve speed only.");
            Settings.expertSpeedPercent = (int)Math.Round(listing.SliderLabeled(
                $"Maximum extraction speed at level 20: {Settings.expertSpeedPercent}%",
                Settings.expertSpeedPercent, 150, 300, 0.75f,
                "Skill multiplier when all three skills reach 20. Default: 200%. Each skill scales independently from its reference level to 20. Each skill contributes independently. Below reference, skill speed remains 100%. General work speed and capacities apply separately. Range: 150–300%."));
            Settings.corpseMinutesPer50Material = (float)Math.Round(listing.SliderLabeled(
                $"Fresh extraction per 50 material: {Settings.corpseMinutesPer50Material:0} min",
                Settings.corpseMinutesPer50Material, 1f, 120f, 0.75f,
                "Timing parameter (halved during calculation) for 50 units of biological material at 100% work speed, before setup and large-body efficiency. Default parameter: 24 minutes, giving 12 minutes before setup and other factors. Lower values speed up all corpse extraction. Range: 1–120 minutes."));
            Settings.corpseSetupMinutes = (float)Math.Round(listing.SliderLabeled(
                $"Setup time per corpse: {Settings.corpseSetupMinutes:0.#} min",
                Settings.corpseSetupMinutes, 0f, 15f, 0.75f,
                "Halved, then added to fresh extraction time before large-body efficiency, decay and operator speed are applied. Default: 2.4 minutes. Keeps many small bodies less efficient than one large body. Range: 0–15 minutes, in 0.1-minute steps.") * 10f) / 10f;
            Settings.corpseHandlingPercent = (int)Math.Round(listing.SliderLabeled(
                $"Work unaffected by decay: {Settings.corpseHandlingPercent}%",
                Settings.corpseHandlingPercent, 0, 100, 0.75f,
                "Share of total fresh-corpse work retained regardless of decay, representing handling and structural breakdown. The remaining work scales with recoverable yield. Default: 30%. At 0%, time follows yield; at 100%, decay never reduces time."));
            float handling = Settings.corpseHandlingPercent / 100f;
            listing.Label($"Current decay timings: fresh 100%, rotting {(handling + (1f - handling) * 0.5f) * 100f:0.#}%, desiccated {(handling + (1f - handling) * 0.1f) * 100f:0.#}% of fresh time.");
            listing.Label("Yields remain 100% / 50% / 10%, with at least 1 biomass. Large bodies receive up to 25% less work. Operator speed also affects duration.");
            listing.Label("Corpse timing changes apply when extraction begins. Existing jobs keep their saved duration. Meat and crop bills are unaffected.");
            listing.Label("Wastage is rolled once at completion for every input. All successful extractions yield at least one raw biomass. Rot-stink risk applies to all inputs during active extraction.");
            if (listing.ButtonText("Restore raw biomass defaults"))
            {
                Settings.corpseMinutesPer50Material = 24f;
                Settings.corpseSetupMinutes = 2.4f;
                Settings.corpseHandlingPercent = 30;
                Settings.expertSpeedPercent = 200;
            }
            listing.GapLine();
            listing.Label("Refined biomass — Biomass Refinery");
            Settings.processingMinutes = DurationSlider(listing,
                "Processing: no gene processors", Settings.processingMinutes, 5, 4320, 5,
                "Automated processing time without processor assistance. Range: 5 minutes to 72 hours, in 5-minute steps.");
            Settings.minimumProcessingMinutes = DurationSlider(listing,
                "Processing: shortest allowed time", Settings.minimumProcessingMinutes, 5, Settings.processingMinutes, 5,
                "Gene processors cannot reduce processing below this duration. Cannot exceed the unassisted processing time.");
            Settings.processorReductionPercent = (int)Math.Round(listing.SliderLabeled(
                $"Time reduction per gene processor: {Settings.processorReductionPercent}%",
                Settings.processorReductionPercent, 0, 25, 0.75f,
                "Each available processor removes this percentage of the unassisted duration. Reductions add together until the shortest allowed time is reached. 0% disables assistance. Range: 0–25% in 1% steps."));
            listing.Label("Preparation: 90 minutes at weighted skill 5, falling linearly to 30 at 20 (Intellectual 60%, Crafting 25%, Medicine 15%). Operator failure interpolates from 30% at skill 5 to 5% at 10, then 0% at 20. Success converts 40 raw biomass and 1 neutroamine into 10 refined biomass.");
            listing.Label("Active gene assemblers have priority. Each powered processor assists at most one refinery.");
            listing.GapLine();
            listing.Label("Processing environment — all three machines");
            listing.CheckboxLabeled("Room cleanliness affects refinement and fabrication failure risk", ref Settings.refinementCleanlinessEnabled,
                "Dirt adds failure risk to refinement and fabrication. Extraction and artificial-part recovery ignore room cleanliness. Outdoor and dim-light penalties apply independently.");
            if (Settings.refinementCleanlinessEnabled)
            {
                Settings.refinementDirtyPenalty = (int)Math.Round(listing.SliderLabeled(
                    $"Very dirty room (cleanliness -2): +{Settings.refinementDirtyPenalty} risk points",
                    Settings.refinementDirtyPenalty, 0, 50, 0.75f,
                    "Maximum added failure probability, in percentage points, at cleanliness -2 or worse. Scales linearly to zero at cleanliness 0. Default: +15 points; -1 gives +7.5. Range: 0–50 points."));
            }
            Settings.refinementExposurePenalty = (int)Math.Round(listing.SliderLabeled(
                $"Outdoor or unroofed exposure: +{Settings.refinementExposurePenalty} penalty points",
                Settings.refinementExposurePenalty, 0, 50, 0.75f,
                "All three processes: +10 points by default outside an enclosed, fully roofed room. Replaces room dirt; stacks with dim light. Adds failure risk for refinement/fabrication, wastage for extraction. Range: 0–50 points."));
            Settings.dimLightPenalty = (int)Math.Round(listing.SliderLabeled(
                $"Darkness: up to +{Settings.dimLightPenalty} penalty points", Settings.dimLightPenalty, 0, 50, 0.75f,
                "All three processes: maximum penalty at 0% light, tapering linearly to zero at 30% light. Uses light at the operator interaction cell. Default: +10 points. Range: 0–50."));
            listing.Label("Refinement captures conditions at batch start. Fabrication weights failure risk by work done under each condition. Extraction adds environmental wastage at completion and still yields at least one biomass. Existing work speed is unchanged.");
            if (listing.ButtonText("Restore refinement and environment defaults"))
            {
                Settings.processingMinutes = 2160;
                Settings.minimumProcessingMinutes = 1080;
                Settings.processorReductionPercent = 5;
                Settings.refinementCleanlinessEnabled = true;
                Settings.refinementDirtyPenalty = 15;
                Settings.refinementExposurePenalty = 10;
                Settings.dimLightPenalty = 10;
            }
            listing.GapLine();
            listing.Label("Gene research — Fabrication and categories");
            listing.Label("Category costs combine the base, gene count, complexity and archite terms below, apply the category percentage, round up, then stay between the minimum and maximum. All loaded genes count; negative complexity or archite values contribute zero.");
            Settings.geneCategoryCostPercent = (int)Math.Round(listing.SliderLabeled(
                $"Category research cost percentage: {Settings.geneCategoryCostPercent}%",
                Settings.geneCategoryCostPercent, 1, 200, 0.75f,
                "Scales the entire category calculation before the maximum is applied. Default: 25%, one quarter of the original costs. 100% restores the original scale. Range: 1–200%. Does not change the fabrication foundation or remove Archogenetics requirements."));
            Settings.geneCategoryMinimumCost = ResearchCostSlider(listing,
                "Minimum research cost per category", Settings.geneCategoryMinimumCost, 100, 10000, 100,
                "Minimum final cost after applying the percentage and rounding. Default: 1,000 points. Range: 100–10,000 in 100-point steps. Raises the maximum if necessary. Does not change the fabrication foundation.");
            Settings.geneCategoryMaximumCost = ResearchCostSlider(listing,
                "Maximum research cost per category", Settings.geneCategoryMaximumCost, Settings.geneCategoryMinimumCost, 10000, 100,
                "Maximum final cost after the category percentage and rounding. Default: 4,000 points. Cannot be below the category minimum. Categories within the limits still scale with gene count and complexity. Does not cap the fabrication foundation.");
            listing.Label("Restart RimWorld after changing these costs. Research points already earned remain unchanged; raising costs can require additional research, including for previously completed projects. Earlier biomass research costs are unaffected.");
            Settings.geneFabricationResearchCost = ResearchCostSlider(listing,
                "Gene fabrication foundation", Settings.geneFabricationResearchCost, 100, 10000, 100,
                "Cost of the foundation project after Biomass refinement. Default: 1,500 points. Range: 100–10,000, in 100-point steps.");
            Settings.geneCategoryBaseCost = ResearchCostSlider(listing,
                "Base cost per gene category", Settings.geneCategoryBaseCost, 0, 5000, 50,
                "Fixed cost added once per category before scaling and applying the minimum/maximum. Default: 500 points. Range: 0–5,000, in 50-point steps.");
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
                Settings.geneCategoryMinimumCost = 1000;
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
