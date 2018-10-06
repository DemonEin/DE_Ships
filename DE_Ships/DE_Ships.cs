using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using System.Reflection;
using RimWorld;
using UnityEngine;


namespace DE_Ships
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("DE_Ships");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class Zone_Shipyard : Zone
    {
        public Zone_Shipyard()
        {

        }
        public Zone_Shipyard(ZoneManager zoneManager)
      : base("Shipyard".Translate(), zoneManager)
        {
        }
        protected override Color NextZoneColor
        {
            get
            {
                return ZoneColorUtility.NextStorageZoneColor();
            }
        }
    }
    public class Designator_ZoneAdd_Shipyard : Designator_ZoneAdd
    {
        public Designator_ZoneAdd_Shipyard()
        {
            this.zoneTypeToPlace = typeof(Zone_Shipyard);
            this.defaultLabel = "Shipyard".Translate();
            this.defaultDesc = "ShipyardDesignatorDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing", true);
            //TODO: not sure what this does, take a look
            this.tutorTag = "ZoneAdd_Shipyard";
        }
        protected override string NewZoneLabel
        {
            get
            {
                return "Shipyard".Translate();
            }
        }
        protected override Zone MakeNewZone()
        {
            return (Zone)new Zone_Shipyard(Find.CurrentMap.zoneManager);
        }
    }
    class GenStep_Ocean : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 262606459;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            foreach (IntVec3 allCell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(allCell, TerrainDefOf.WaterOceanDeep);
                //map.terrainGrid.SetTerrain(allCell, TerrainDefOf.Soil);
            }
            map.terrainGrid.SetTerrain(new IntVec3(5, 0, 10), TerrainDefOf.Gravel);
        }
    }

    //based on Verse.TerrainGrid
    public class Ship_Structure
    {
        private Map map;
        public TerrainDef[] topGrid;
        private TerrainDef[] underGrid;

        //constructs a Ship_Structure from the corners of a rectangle of a TerrainGrid from which to form a ship
        public Ship_Structure(TerrainGrid baseTerrain, IntVec3 botLeftCorner, IntVec3 topRightCorner)
        {
            //this.map = map;
            this.ResetGrids();
            IntVec3 currentTile = new IntVec3();
            for (int x = botLeftCorner.x; x <= topRightCorner.x; x++)
            {
                for (int z = botLeftCorner.z; z <= botLeftCorner.z; z++)
                {
                    currentTile.x = x;
                    currentTile.z = z;
                    if (baseTerrain.TerrainAt(currentTile).defName.Contains("Boat_")) {
                        this.SetTerrain(currentTile, baseTerrain.TerrainAt(currentTile));
                    }
                }
            }
        }

        public void ResetGrids()
        {
            this.topGrid = new TerrainDef[this.map.cellIndices.NumGridCells];
            this.underGrid = new TerrainDef[this.map.cellIndices.NumGridCells];
        }

        public TerrainDef TerrainAt(int ind)
        {
            return this.topGrid[ind];
        }

        public TerrainDef TerrainAt(IntVec3 c)
        {
            return this.topGrid[this.map.cellIndices.CellToIndex(c)];
        }

        public TerrainDef UnderTerrainAt(int ind)
        {
            return this.underGrid[ind];
        }

        public TerrainDef UnderTerrainAt(IntVec3 c)
        {
            return this.underGrid[this.map.cellIndices.CellToIndex(c)];
        }

        public void SetTerrain(IntVec3 c, TerrainDef newTerr)
        {
            if (newTerr == null)
            {
                Log.Error("Tried to set terrain at " + (object)c + " to null.", false);
            }
            else
            {
                if (Current.ProgramState == ProgramState.Playing)
                    this.map.designationManager.DesignationAt(c, DesignationDefOf.SmoothFloor)?.Delete();
                int index = this.map.cellIndices.CellToIndex(c);
                if (newTerr.layerable)
                {
                    if (this.underGrid[index] == null)
                        this.underGrid[index] = this.topGrid[index].passability == Traversability.Impassable ? TerrainDefOf.Sand : this.topGrid[index];
                }
                else
                    this.underGrid[index] = (TerrainDef)null;
                this.topGrid[index] = newTerr;
                this.DoTerrainChangedEffects(c);
            }
        }

        public void SetUnderTerrain(IntVec3 c, TerrainDef newTerr)
        {
            if (!c.InBounds(this.map))
                Log.Error("Tried to set terrain out of bounds at " + (object)c, false);
            else
                this.underGrid[this.map.cellIndices.CellToIndex(c)] = newTerr;
        }

        public void RemoveTopLayer(IntVec3 c, bool doLeavings = true)
        {
            int index = this.map.cellIndices.CellToIndex(c);
            if (doLeavings)
                GenLeaving.DoLeavingsFor(this.topGrid[index], c, this.map);
            if (this.underGrid[index] == null)
                return;
            this.topGrid[index] = this.underGrid[index];
            this.underGrid[index] = (TerrainDef)null;
            this.DoTerrainChangedEffects(c);
        }

        public bool CanRemoveTopLayerAt(IntVec3 c)
        {
            int index = this.map.cellIndices.CellToIndex(c);
            if (this.topGrid[index].Removable)
                return this.underGrid[index] != null;
            return false;
        }

        private void DoTerrainChangedEffects(IntVec3 c)
        {
            this.map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Terrain, true, false);
            List<Thing> thingList = c.GetThingList(this.map);
            for (int index = thingList.Count - 1; index >= 0; --index)
            {
                if (thingList[index].def.category == ThingCategory.Plant && (double)this.map.fertilityGrid.FertilityAt(c) < (double)thingList[index].def.plant.fertilityMin)
                    thingList[index].Destroy(DestroyMode.Vanish);
                else if (thingList[index].def.category == ThingCategory.Filth && !this.TerrainAt(c).acceptFilth)
                    thingList[index].Destroy(DestroyMode.Vanish);
                else if ((thingList[index].def.IsBlueprint || thingList[index].def.IsFrame) && !GenConstruct.CanBuildOnTerrain(thingList[index].def.entityDefToBuild, thingList[index].Position, this.map, thingList[index].Rotation, (Thing)null))
                    thingList[index].Destroy(DestroyMode.Cancel);
            }
            this.map.pathGrid.RecalculatePerceivedPathCostAt(c);
            Region rebuildInvalidAllowed = this.map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c);
            if (rebuildInvalidAllowed == null || rebuildInvalidAllowed.Room == null)
                return;
            rebuildInvalidAllowed.Room.Notify_TerrainChanged();
        }

        public void ExposeData()
        {
            this.ExposeTerrainGrid(this.topGrid, "topGrid");
            this.ExposeTerrainGrid(this.underGrid, "underGrid");
        }

        public void Notify_TerrainBurned(IntVec3 c)
        {
            TerrainDef terrain = c.GetTerrain(this.map);
            this.Notify_TerrainDestroyed(c);
            if (terrain.burnedDef == null)
                return;
            this.SetTerrain(c, terrain.burnedDef);
        }

        public void Notify_TerrainDestroyed(IntVec3 c)
        {
            if (!this.CanRemoveTopLayerAt(c))
                return;
            TerrainDef terrainDef = this.TerrainAt(c);
            this.RemoveTopLayer(c, false);
            if (terrainDef.destroyBuildingsOnDestroyed)
                c.GetFirstBuilding(this.map)?.Kill(new DamageInfo?(), (Hediff)null);
            if (terrainDef.destroyEffectWater != null && this.TerrainAt(c) != null && this.TerrainAt(c).IsWater)
            {
                Effecter effecter = terrainDef.destroyEffectWater.Spawn();
                effecter.Trigger(new TargetInfo(c, this.map, false), new TargetInfo(c, this.map, false));
                effecter.Cleanup();
            }
            else
            {
                if (terrainDef.destroyEffect == null)
                    return;
                Effecter effecter = terrainDef.destroyEffect.Spawn();
                effecter.Trigger(new TargetInfo(c, this.map, false), new TargetInfo(c, this.map, false));
                effecter.Cleanup();
            }
        }

        private void ExposeTerrainGrid(TerrainDef[] grid, string label)
        {
            Dictionary<ushort, TerrainDef> terrainDefsByShortHash = new Dictionary<ushort, TerrainDef>();
            foreach (TerrainDef allDef in DefDatabase<TerrainDef>.AllDefs)
                terrainDefsByShortHash.Add(allDef.shortHash, allDef);
            MapExposeUtility.ExposeUshort(this.map, (Func<IntVec3, ushort>)(c =>
            {
                TerrainDef terrainDef = grid[this.map.cellIndices.CellToIndex(c)];
                if (terrainDef != null)
                    return terrainDef.shortHash;
                return 0;
            }), (Action<IntVec3, ushort>)((c, val) =>
            {
                TerrainDef terrainDef1 = terrainDefsByShortHash.TryGetValue<ushort, TerrainDef>(val, (TerrainDef)null);
                if (terrainDef1 == null && val != (ushort)0)
                {
                    TerrainDef terrainDef2 = BackCompatibility.BackCompatibleTerrainWithShortHash(val);
                    if (terrainDef2 == null)
                    {
                        Log.Error("Did not find terrain def with short hash " + (object)val + " for cell " + (object)c + ".", false);
                        terrainDef2 = TerrainDefOf.Soil;
                    }
                    terrainDef1 = terrainDef2;
                    terrainDefsByShortHash.Add(val, terrainDef2);
                }
                grid[this.map.cellIndices.CellToIndex(c)] = terrainDef1;
            }), label);
        }

        public string DebugStringAt(IntVec3 c)
        {
            if (!c.InBounds(this.map))
                return "out of bounds";
            TerrainDef terrain = c.GetTerrain(this.map);
            TerrainDef terrainDef = this.underGrid[this.map.cellIndices.CellToIndex(c)];
            return "top: " + (terrain == null ? "null" : terrain.defName) + ", under=" + (terrainDef == null ? "null" : terrainDef.defName);
        }
    }
}

    [StaticConstructorOnStartup]
    public static class WaterGenerator
    {
        public static List<GenStepWithParams> oceanGenSteps = new List<GenStepWithParams>();

        static WaterGenerator()
        {
            GenStepParams emptyParams = new GenStepParams();
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("GenStep_Ocean"), emptyParams));
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("FindPlayerStartSpot"), emptyParams));
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("ScenParts"), emptyParams));
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("Fog"), emptyParams));
        }

    [HarmonyPatch(typeof(MapGenerator))]
    [HarmonyPatch("GenerateContentsIntoMap")]
    class MapGeneratorPatch
    {

        //static void Prefix(IEnumerable<GenStepWithParams> genStepDefs, Map map, int seed)
        static bool Prefix(Map map, int seed)
        {
            if (!map.Biome.defName.Equals("Ocean"))
            {
                return true;
            }

            //based on MapGenerator.GenerateMap
            //MapGenerator.data.Clear();
            Rand.PushState();
            try
            {
                Rand.Seed = seed;
                RockNoises.Init(map);
                //OceanGenSteps.Clear();
                //OceanGenSteps.AddRange((IEnumerable<GenStepWithParams>)genStepDefs.OrderBy<GenStepWithParams, float>((Func<GenStepWithParams, float>)(x => x.def.order)).ThenBy<GenStepWithParams, ushort>((Func<GenStepWithParams, ushort>)(x => x.def.index)));
                for (int index = 0; index < WaterGenerator.oceanGenSteps.Count; ++index)
                {
                    DeepProfiler.Start("GenStep - " + (object)WaterGenerator.oceanGenSteps[index].def);
                    try
                    {
                        //Rand.Seed = Gen.HashCombineInt(seed, MapGenerator.GetSeedPart(OceanGenSteps, index));
                        WaterGenerator.oceanGenSteps[index].def.genStep.Generate(map, WaterGenerator.oceanGenSteps[index].parms);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error in GenStep: " + (object)ex, false);
                    }
                    finally
                    {
                        DeepProfiler.End();
                    }
                }
            }
            finally
            {
                Rand.PopState();
                RockNoises.Reset();
                //MapGenerator.data.Clear();
            }

            return false;
        }
    }
}