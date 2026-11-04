#if RIMWORLD_TESTS
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using GeneFoundry.Components;
using RimWorld;
using Verse;
using Xunit;

namespace GeneFoundry.Tests
{
    [Collection("GeneFoundry.Tests.GameGeneResearchTests")]
    public class GameBiomassStorageTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static T Uninitialized<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

        [Fact]
        public void OnlyBiomassHopperCellPreservesRotAndRemovalRetainsPriorAge()
        {
            Game previous = Current.Game;
            try
            {
                var map = Uninitialized<Map>();
                map.info = Uninitialized<MapInfo>();
                typeof(MapInfo).GetField("sizeInt", Private).SetValue(map.info, new IntVec3(1, 1, 1));
                map.cellIndices = new CellIndices(1, 1);
                map.thingGrid = new ThingGrid(map);
                var game = Uninitialized<Game>();
                typeof(Game).GetField("maps", Private).SetValue(game, new List<Map> { map });
                Current.Game = game;

                var biomass = Uninitialized<ThingWithComps>();
                typeof(Thing).GetField("positionInt", Private).SetValue(biomass, IntVec3.Zero);
                typeof(Thing).GetField("mapIndexOrState", Private).SetValue(biomass, (sbyte)0);
                var comp = new CompRawBiomassRottable { parent = biomass };
                comp.Initialize(new CompProperties_RawBiomassRottable { daysToRotStart = 6 });
                comp.RotProgress = 1200f;
                var cell = map.thingGrid.ThingsListAt(IntVec3.Zero);
                Assert.False(comp.PreservedInHopper);
                var shelf = Uninitialized<Building_Storage>();
                shelf.def = Uninitialized<ThingDef>();
                shelf.def.defName = "Shelf";
                cell.Add(shelf);
                Assert.False(comp.PreservedInHopper);
                var hopper = Uninitialized<Building_Storage>();
                hopper.def = Uninitialized<ThingDef>();
                hopper.def.defName = "GF_BiomassHopper";
                cell.Add(hopper);
                Assert.True(comp.PreservedInHopper);
                comp.CompTickRare();
                comp.CompTickInterval(250);
                Assert.Equal(1200f, comp.RotProgress);
                Assert.Contains("spoilage paused", comp.CompInspectStringExtra());
                cell.Remove(hopper);
                Assert.False(comp.PreservedInHopper);
                Assert.Equal(1200f, comp.RotProgress);
                typeof(Thing).GetField("mapIndexOrState", Private).SetValue(biomass, (sbyte)-1);
                Assert.False(comp.PreservedInHopper);
            }
            finally { Current.Game = previous; }
        }
    }
}
#endif
