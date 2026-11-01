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
            listing.Begin(viewRect);
            Settings.Validate();
            listing.Label("All times are in-game time. Hover over sliders for explanations. Settings apply across saves.");
            listing.GapLine();
            listing.Label("Raw biomass — Gene Vat corpse extraction");
            listing.Label("All extraction uses Cooking 40%, Medicine 40%, Intellectual 20% for speed and XP. Skills give 50% speed at level 0, 100% at 5 and 150% at 10, with no further skill bonus above 10.");
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
            if (listing.ButtonText("Restore raw biomass timing defaults"))
            {
                Settings.corpseMinutesPer50Material = 24f;
                Settings.corpseSetupMinutes = 2.4f;
                Settings.corpseHandlingPercent = 30;
            }
            listing.GapLine();
            listing.Label("Refined biomass — Biomass Refinery");
            Settings.preparationMinutes = DurationSlider(listing,
                "Preparation: minimum-skilled operator", Settings.preparationMinutes, 1, 120, 1,
                "Time spent preparing a batch at Intellectual 5, Crafting 3 and Medicine 2. Range: 1–120 minutes.");
            Settings.minimumPreparationMinutes = DurationSlider(listing,
                "Preparation: fully skilled operator", Settings.minimumPreparationMinutes, 1, Settings.preparationMinutes, 1,
                "Shortest preparation time, reached at Intellectual 10, Crafting 8 and Medicine 6. Higher skills give no further benefit. Cannot exceed the minimum-skilled preparation time.");
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
            if (listing.ButtonText("Restore refined biomass timing defaults"))
            {
                Settings.preparationMinutes = 60;
                Settings.minimumPreparationMinutes = 30;
                Settings.processingMinutes = 720;
                Settings.minimumProcessingMinutes = 360;
                Settings.processorReductionPercent = 5;
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

        private static string FormatDuration(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} min";
            int remainder = minutes % 60;
            return remainder == 0 ? $"{minutes / 60} h" : $"{minutes / 60} h {remainder} min";
        }
    }
}
