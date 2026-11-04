using RimWorld;
using Verse;

namespace GeneFoundry.Recipes
{
    internal static class ProcessingEnvironment
    {
        internal static float Adjustment(Thing bench, bool includeCleanliness) =>
            Adjustment(bench, includeCleanliness, GeneFoundryMod.Settings.refinementCleanlinessEnabled,
                out _, out _, out _);

        internal static float Adjustment(Thing bench, bool includeCleanliness, bool cleanlinessEnabled,
            out float cleanliness, out bool exposed, out float light)
        {
            cleanliness = 0f; exposed = false; light = 1f;
            if (bench == null || !bench.Spawned) return 0f;
            Room room = bench.GetRoom();
            exposed = room == null || room.OutdoorsForWork || room.TouchesMapEdge || room.OpenRoofCount > 0;
            if (!exposed && includeCleanliness && cleanlinessEnabled)
                cleanliness = room.GetStat(RoomStatDefOf.Cleanliness);
            IntVec3 cell = bench.def.hasInteractionCell ? bench.InteractionCell : bench.Position;
            light = bench.Map.glowGrid.GroundGlowAt(cell);
            GeneFoundrySettings settings = GeneFoundryMod.Settings;
            return BiomassCalculations.ProcessingEnvironmentAdjustment(cleanliness, exposed, light,
                includeCleanliness, cleanlinessEnabled, settings.refinementDirtyPenalty,
                settings.refinementExposurePenalty, settings.dimLightPenalty);
        }

        internal static string Description(Thing bench, bool includeCleanliness)
        {
            float adjustment = Adjustment(bench, includeCleanliness, GeneFoundryMod.Settings.refinementCleanlinessEnabled,
                out float cleanliness, out bool exposed, out float light);
            string room = exposed ? "Outdoor/unroofed" : includeCleanliness && GeneFoundryMod.Settings.refinementCleanlinessEnabled
                ? $"Room cleanliness: {cleanliness:0.##}" : "Indoors";
            return $"{room}; light: {light:P0}; environment penalty: +{adjustment * 100f:0.#} points";
        }
    }
}
