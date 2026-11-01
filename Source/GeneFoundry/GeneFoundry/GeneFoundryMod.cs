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

        public void Validate()
        {
            preparationMinutes = Math.Max(1, Math.Min(120, preparationMinutes));
            minimumPreparationMinutes = Math.Max(1, Math.Min(preparationMinutes, minimumPreparationMinutes));
            processingMinutes = Math.Max(5, Math.Min(1440, processingMinutes));
            minimumProcessingMinutes = Math.Max(5, Math.Min(processingMinutes, minimumProcessingMinutes));
            processorReductionPercent = Math.Max(0, Math.Min(25, processorReductionPercent));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref preparationMinutes, "preparationMinutes", 60);
            Scribe_Values.Look(ref minimumPreparationMinutes, "minimumPreparationMinutes", 30);
            Scribe_Values.Look(ref processingMinutes, "processingMinutes", 720);
            Scribe_Values.Look(ref minimumProcessingMinutes, "minimumProcessingMinutes", 360);
            Scribe_Values.Look(ref processorReductionPercent, "processorReductionPercent", 5);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Validate();
        }
    }

    public class GeneFoundryMod : Mod
    {
        public static GeneFoundrySettings Settings { get; private set; } = new GeneFoundrySettings();

        public GeneFoundryMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GeneFoundrySettings>();
        }

        public override string SettingsCategory() => "GeneFoundry";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            Settings.Validate();
            listing.Label("Refinery timings use in-game time. Hover over a slider for details.");
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
            if (listing.ButtonText("Restore defaults"))
            {
                Settings.preparationMinutes = 60;
                Settings.minimumPreparationMinutes = 30;
                Settings.processingMinutes = 720;
                Settings.minimumProcessingMinutes = 360;
                Settings.processorReductionPercent = 5;
            }
            listing.End();
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
