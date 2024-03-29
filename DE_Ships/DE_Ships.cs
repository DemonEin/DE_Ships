﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using System.Reflection;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;


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
                //TODO: change?
                return ZoneColorUtility.NextStorageZoneColor();
            }
        }
    }

    public static class ShipMoverTest
    {
        private static void MoveAction()
        {
            Vessel sourceWorldObject = (Vessel)Find.WorldSelector.SingleSelectedObject;
            sourceWorldObject.Tile = EmbarkShipUtility.AdjacentOceanTile(sourceWorldObject.Tile);
        }
        public static Command MoveCommand()
        {
            Command_Action commandAction = new Command_Action();
            Action action = MoveAction;
            commandAction.defaultLabel = "Move Ship";
            commandAction.defaultDesc = "CommandAbandonHomeDesc".Translate();
            //commandAction.icon = SettlementAbandonUtility.AbandonCommandTex;
            commandAction.order = 30f;
            commandAction.action = action;
            return (Command)commandAction;
        }
    }

    //TODO: add expansion designator?
    //TODO: handle creation of multiple shipyards
    public class Designator_ZoneAdd_Shipyard : Designator_ZoneAdd
    {
        public Designator_ZoneAdd_Shipyard()
        {
            this.zoneTypeToPlace = typeof(Zone_Shipyard);
            this.defaultLabel = "Shipyard".Translate();
            this.defaultDesc = "ShipyardDesignatorDesc".Translate();
            //TODO: change
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
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(this.Map))
                return (AcceptanceReport)false;
            if (c.Fogged(this.Map))
                return (AcceptanceReport)false;
            //if (c.InNoZoneEdgeArea(this.Map))
            //return (AcceptanceReport)"TooCloseToMapEdge".Translate();
            Zone zone = this.Map.zoneManager.ZoneAt(c);
            if (zone != null && zone.GetType() != this.zoneTypeToPlace)
                return (AcceptanceReport)false;
            foreach (Thing thing in this.Map.thingGrid.ThingsAt(c))
            {
                if (!thing.def.CanOverlapZones)
                    return (AcceptanceReport)false;
            }

            return (AcceptanceReport)true;
        }
    }
    class GenStep_Ocean : GenStep
    {
        private Vessel_Structure structure;
        public override int SeedPart
        {
            get
            {
                return 262606459;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            structure = WaterGenerator.cachedStructure;
            foreach (IntVec3 allCell in map.AllCells)
            {
                map.terrainGrid.SetTerrain(allCell, TerrainDefOf.WaterOceanDeep);
                //map.terrainGrid.SetTerrain(allCell, TerrainDefOf.Soil);
            }
            IntVec3 newOffset = map.Center - structure.Center;
            foreach (IntVec3 cell in structure.cells)
            {
                map.terrainGrid.SetTerrain(cell + newOffset, structure.TerrainAt(cell));
                map.terrainGrid.SetUnderTerrain(cell + newOffset, structure.UnderTerrainAt(cell));
                foreach (Thing thing in structure.ThingsListAtFast(cell))
                {
                    thing.SpawnSetup(map, false);
                    thing.Position += newOffset;
                    map.thingGrid.Register(thing);
                }
            }
            //only included to avoid an error, this is not actually used (I think)
            MapGenerator.PlayerStartSpot = map.Center;
        }
    }
    [StaticConstructorOnStartup]
    public static class VesselManager
    {
        //ISSUE: does not save, so if a vessel is being sent when the game is reloaded, a regular caravan will form instead
        private static List<LordJob_FormAndSendCaravan> SendVesselJobs = new List<LordJob_FormAndSendCaravan>();
        private static List<Zone_Shipyard> ShipyardsToSend = new List<Zone_Shipyard>();
        private static LordJob_FormAndSendCaravan ActiveJob;
        private static Zone_Shipyard activeshipyard;
        public static bool FormVessel
        {
            get
            {
                return ActiveJob != null;
            }
        }
        public static Zone_Shipyard ActiveShipyard
        {
            get
            {
                return activeshipyard;
            }
        }
        public static void AddVesselEmbark (LordJob_FormAndSendCaravan job, Zone_Shipyard shipyard)
        {
            SendVesselJobs.Add(job);
            ShipyardsToSend.Add(shipyard);
        }
        private static void RemoveVesselEmbark (LordJob_FormAndSendCaravan job)
        {
            for (int i = 0; i < SendVesselJobs.Count; i++)
            {
                if (SendVesselJobs[i] == job)
                {
                    SendVesselJobs.RemoveAt(i);
                    ShipyardsToSend.RemoveAt(i);
                    return;
                }
            }
            Log.Error("Failed to find VesselEmbark job to remove");
        }
        public static void ActivateVesselForming(LordJob_FormAndSendCaravan job)
        {
            ActiveJob = job;
            activeshipyard = GetShipyardToEmbark(job);
        }
        public static void DeactivateVesselForming(LordJob_FormAndSendCaravan job)
        {
            RemoveVesselEmbark(job);
            ActiveJob = null;
            activeshipyard = null;
        }
        public static Zone_Shipyard GetShipyardToEmbark(LordJob_FormAndSendCaravan job)
        {
            for (int i = 0; i < SendVesselJobs.Count; i++)
            {
                if (SendVesselJobs[i] == job)
                {
                    return ShipyardsToSend[i];
                }
            }
            return null;
        }
        public static bool CaravanJobIsForVessel(LordJob_FormAndSendCaravan testJob)
        {
            for (int i = 0; i < SendVesselJobs.Count; i++)
            {
                if (SendVesselJobs[i] == testJob)
                {
                    return true;
                }
            }
            return false;
        }
        /*
        public static List<Caravan> caravans = new List<Caravan>();
        public static bool WorldObjectIsNavigator (WorldObject cara)
        {
            if (cara == null || cara.GetType() != typeof(Caravan))
            {
                return false;
            }
            foreach (Caravan caravan in caravans)
            {
                if (caravan == cara)
                {
                    return true;
                }
            }
            return false;
        }
        */
        public static bool WorldObjectIsNavigator (WorldObject cara)
        {
            if (cara == null || cara.GetType() != typeof(Caravan))
            {
                return false;
            }
            if (Find.WorldGrid[cara.Tile].biome.Equals(DefDatabase<BiomeDef>.GetNamed("Ocean"))) {
                return true;
            }
            return false;
        }
    }
    public class Vessel : MapParent
    {
        public Vessel_Structure structure;
        //the caravan that represents the vessel on the world map
        public Caravan caravan;

        public new void SetFaction(Faction faction)
        {
            base.SetFaction(faction);
            caravan.SetFaction(faction);
        }
        public new int Tile
        {
            set
            {
                base.Tile = value;
                caravan.Tile = value;
            }
            get
            {
                return base.Tile;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Caravan>(ref this.caravan, "caravan");
        }
        /*
        public override IEnumerable<GenStepWithParams> ExtraGenStepDefs
        {
            get
            {
                return WaterGenerator.oceanGenSteps;
            }
        }
        */
    }
    //based on Verse.TerrainGrid and Verse.ThingGrid
    public class Vessel_Structure
    {
        //Verse.TerrainGrid variables
        private Map map;
        public TerrainDef[] topGrid;
        public TerrainDef[] underGrid;

        //Verse.ThingGrid variables
        private static readonly List<Thing> EmptyThingList = new List<Thing>();
        //private Map map;
        public List<Thing>[] thingGrid;

        //Custom variables
        public List<IntVec3> cells = new List<IntVec3>();
        //(uses int division, may need to be adjusted)
        public IntVec3 Center;

        /*
        private static int nextloadID = 0;
        private int loadID;
        */

        //constructs a Vessel_Structure
        public Vessel_Structure(Zone_Shipyard shipyard)
        {
            /*
            loadID = nextloadID;
            nextloadID++;
            */
            map = shipyard.Map;
            ResetGrids();
            int avgDenom = 0;
            IntVec3 avgNumerator = IntVec3.Zero;
            foreach (IntVec3 cell in shipyard.cells)
            {
                if (map.terrainGrid.TerrainAt(cell).defName.StartsWith("Boat_"))
                {
                    if (map.terrainGrid.TerrainAt(cell) != null)
                    {
                        SetTerrain(cell, map.terrainGrid.TerrainAt(cell));
                        map.terrainGrid.RemoveTopLayer(cell, false);
                        FilthMaker.RemoveAllFilth(cell, map);
                        avgNumerator += cell;
                        avgDenom++;
                    }
                    /*
                    if (map.terrainGrid.UnderTerrainAt(cell) != null)
                    {
                        SetUnderTerrain(cell, map.terrainGrid.UnderTerrainAt(cell));
                        avgNumerator += cell;
                        avgDenom++;
                    }
                    */
                    List<Thing> thingList = map.thingGrid.ThingsListAtFast(cell);
                    for (int i = thingList.Count - 1; i >= 0; i--)
                    {
                        RegisterInCell(thingList[i], cell);
                        thingList[i].DeSpawn();

                        avgNumerator += cell;
                        avgDenom++;
                    }
                    cells.Add(cell);
                }
            }
            Center = new IntVec3(avgNumerator.x / avgDenom, avgNumerator.y / avgDenom, avgNumerator.z / avgDenom);
        }
        /*
        public string GetUniqueLoadID()
        {
            return "Vessel_Structrue_" + loadID;
        }
        */
        //Verse.TerrainGrid methods (some removed)
        public void ResetGrids()
        {
            CellIndices cellIndices = map.cellIndices;
            this.topGrid = new TerrainDef[cellIndices.NumGridCells];
            this.underGrid = new TerrainDef[cellIndices.NumGridCells];


            this.thingGrid = new List<Thing>[cellIndices.NumGridCells];
            for (int index = 0; index < cellIndices.NumGridCells; ++index)
                this.thingGrid[index] = new List<Thing>(4);
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
                /*
                if (newTerr.layerable)
                {
                    if (this.underGrid[index] == null)
                    {
                        //causes NullReferenceException
                        this.underGrid[index] = this.topGrid[index].passability == Traversability.Impassable ? TerrainDefOf.Sand : this.topGrid[index];
                    }
                        
                }
                else
                */
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

        //Verse.ThingGrid methods (some removed)
        public void Register(Thing t)
        {
            if (t.def.size.x == 1 && t.def.size.z == 1)
            {
                this.RegisterInCell(t, t.Position);
            }
            else
            {
                CellRect cellRect = t.OccupiedRect();
                for (int minZ = cellRect.minZ; minZ <= cellRect.maxZ; ++minZ)
                {
                    for (int minX = cellRect.minX; minX <= cellRect.maxX; ++minX)
                        this.RegisterInCell(t, new IntVec3(minX, 0, minZ));
                }
            }
        }

        private void RegisterInCell(Thing t, IntVec3 c)
        {
            if (!c.InBounds(this.map))
            {
                Log.Warning(t.ToString() + " tried to register out of bounds at " + (object)c + ". Destroying.", false);
                t.Destroy(DestroyMode.Vanish);
            }
            else
                this.thingGrid[this.map.cellIndices.CellToIndex(c)].Add(t);
        }

        public void Deregister(Thing t, bool doEvenIfDespawned = false)
        {
            if (!t.Spawned && !doEvenIfDespawned)
                return;
            if (t.def.size.x == 1 && t.def.size.z == 1)
            {
                this.DeregisterInCell(t, t.Position);
            }
            else
            {
                CellRect cellRect = t.OccupiedRect();
                for (int minZ = cellRect.minZ; minZ <= cellRect.maxZ; ++minZ)
                {
                    for (int minX = cellRect.minX; minX <= cellRect.maxX; ++minX)
                        this.DeregisterInCell(t, new IntVec3(minX, 0, minZ));
                }
            }
        }

        private void DeregisterInCell(Thing t, IntVec3 c)
        {
            if (!c.InBounds(this.map))
            {
                Log.Error(t.ToString() + " tried to de-register out of bounds at " + (object)c, false);
            }
            else
            {
                int index = this.map.cellIndices.CellToIndex(c);
                if (!this.thingGrid[index].Contains(t))
                    return;
                this.thingGrid[index].Remove(t);
            }
        }

        public List<Thing> ThingsListAt(IntVec3 c)
        {
            if (c.InBounds(this.map))
                return this.thingGrid[this.map.cellIndices.CellToIndex(c)];
            Log.ErrorOnce("Got ThingsListAt out of bounds: " + (object)c, 495287, false);
            return EmptyThingList;
        }

        public List<Thing> ThingsListAtFast(IntVec3 c)
        {
            return this.thingGrid[this.map.cellIndices.CellToIndex(c)];
        }

        public List<Thing> ThingsListAtFast(int index)
        {
            return this.thingGrid[index];
        }

        public bool CellContains(IntVec3 c, ThingCategory cat)
        {
            return this.ThingAt(c, cat) != null;
        }

        public Thing ThingAt(IntVec3 c, ThingCategory cat)
        {
            if (!c.InBounds(this.map))
                return (Thing)null;
            List<Thing> thingList = this.thingGrid[this.map.cellIndices.CellToIndex(c)];
            for (int index = 0; index < thingList.Count; ++index)
            {
                if (thingList[index].def.category == cat)
                    return thingList[index];
            }
            return (Thing)null;
        }

        public bool CellContains(IntVec3 c, ThingDef def)
        {
            return this.ThingAt(c, def) != null;
        }

        public Thing ThingAt(IntVec3 c, ThingDef def)
        {
            if (!c.InBounds(this.map))
                return (Thing)null;
            List<Thing> thingList = this.thingGrid[this.map.cellIndices.CellToIndex(c)];
            for (int index = 0; index < thingList.Count; ++index)
            {
                if (thingList[index].def == def)
                    return thingList[index];
            }
            return (Thing)null;
        }

        public T ThingAt<T>(IntVec3 c) where T : Thing
        {
            if (!c.InBounds(this.map))
                return (T)null;
            List<Thing> thingList = this.thingGrid[this.map.cellIndices.CellToIndex(c)];
            for (int index = 0; index < thingList.Count; ++index)
            {
                T obj = thingList[index] as T;
                if ((object)obj != null)
                    return obj;
            }
            return (T)null;
        }
    }

    [StaticConstructorOnStartup]
    public static class WaterGenerator
    {
        public static List<GenStepWithParams> oceanGenSteps = new List<GenStepWithParams>();
        public static Vessel_Structure cachedStructure;

        static WaterGenerator()
        {
            GenStepParams emptyParams = new GenStepParams();
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("GenStep_Ocean"), emptyParams));
            //oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("FindPlayerStartSpot"), emptyParams));
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("ScenParts"), emptyParams));
            oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("Fog"), emptyParams));
        }
    }
    //inpspired by SettlementAbandonUtility
    public static class EmbarkShipUtility
    {
        public static bool EmbarkUIActive = false;
        private static MapParent sourceWorldObject;
        private static Dialog_FormCaravan EmbarkUI;

        //ISSUE: arbitrary choice of tile if multiple tiles
        public static int AdjacentOceanTile(int tileID)
        {
            List<int> tileNeighbors = new List<int>();
            List<int> oceanNeighbors = new List<int>();
            Find.WorldGrid.GetTileNeighbors(tileID, tileNeighbors);
            foreach (int tile in tileNeighbors)
            {
                if (Find.WorldGrid.tiles[tile].biome.defName.Equals("Ocean"))
                {
                    oceanNeighbors.Add(tile);
                }
            }
            if (oceanNeighbors.Count < 1)
            {
                return -1;
            }
            return oceanNeighbors[0];
        }

        public static Zone_Shipyard FindShipyardInMap(Map map)
        {
            foreach (Zone zone in sourceWorldObject.Map.zoneManager.AllZones)
            {

                if (zone is Zone_Shipyard)
                {
                    return ((Zone_Shipyard)zone);
                }
            }
            Log.Warning("No shipyards found in map");
            return null;
        }

        private static void EmbarkActionBeforeLaunch()
        {

            sourceWorldObject = (MapParent)Find.WorldSelector.SingleSelectedObject;
            EmbarkUIActive = true;
            EmbarkUI = new Dialog_FormCaravan(sourceWorldObject.Map, false, EmbarkActionAfterLaunch);
            Find.WindowStack.Add(EmbarkUI);
        }
        private static void EmbarkActionAfterLaunch()
        {
            EmbarkUIActive = false;
        }

        public static Command EmbarkCommand()
        {
            Command_Action commandAction = new Command_Action();
            Action action = EmbarkActionBeforeLaunch;
            commandAction.defaultLabel = "ayylmao";
            commandAction.defaultDesc = "CommandAbandonHomeDesc".Translate();
            //commandAction.icon = SettlementAbandonUtility.AbandonCommandTex;
            commandAction.order = 30f;
            commandAction.action = action;
            return (Command)commandAction;
        }
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
    [HarmonyPatch(typeof(GenConstruct))]
    [HarmonyPatch("CanPlaceBlueprintAt")]
    class ShipConstructionPatch
    {
        static void Postfix(BuildableDef entDef, IntVec3 center, Map map, ref AcceptanceReport __result)
        {
            if (__result.Accepted && entDef.designationCategory.defName.Equals("ShipyardDesignator"))
            {
                if (!(map.zoneManager.ZoneAt(center) is DE_Ships.Zone_Shipyard))
                {
                    __result = new AcceptanceReport("CannotBuildOutOfShipyard".Translate());
                }
            }
        }
    }
    [HarmonyPatch(typeof(MapParent))]
    [HarmonyPatch("GetGizmos")]
    class SettlementGizmoPatch
    {
        static void Postfix(ref IEnumerable<Gizmo> __result, SettlementBase __instance)
        {
            if (EmbarkShipUtility.AdjacentOceanTile(__instance.Tile) == -1)
            {
                return;
            }
            List<Gizmo> newResult = new List<Gizmo>();
            foreach (Gizmo gizmo in __result)
            {
                newResult.Add(gizmo);
            }
            //FEATURE NOT IMPLEMENTED
            /*
            //adds an embark ship button for every shipyard in the map
            foreach (Zone zone in __instance.Map.zoneManager.AllZones)
            {
                if (zone is Zone_Shipyard)
                {
                    newResult.Add(EmbarkShipUtility.EmbarkCommand());
                    (Command)newResult.Last()).def
                }
            }
            */
            newResult.Add(EmbarkShipUtility.EmbarkCommand());
            newResult.Add(ShipMoverTest.MoveCommand());
            __result = newResult;
        }
    }
    [HarmonyPatch(typeof(CaravanFormingUtility))]
    [HarmonyPatch("FormAndCreateCaravan")]
    class CreateCaravanPatch
    {
        static bool Prefix()
        {

            return !EmbarkShipUtility.EmbarkUIActive;
        }
    }
    [HarmonyPatch(typeof(Reachability))]
    [HarmonyPatch("CanReachMapEdge")]
    class CanReachMapEdgePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (EmbarkShipUtility.EmbarkUIActive)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
    //custom CaravanEnterMapUtility.Enter for embarking vessels; original goal: if embarkUI is active, caravan entering a map will not remove the caravan
    [HarmonyPatch(typeof(CaravanEnterMapUtility))]
    [HarmonyPatch("Enter")]
    [HarmonyPatch(new Type[] { typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool) })]
    class CaravanEnterPatch
    {
        private static List<Pawn> tmpPawns = new List<Pawn>();
        static bool Prefix(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter)
        {
            if (!EmbarkShipUtility.EmbarkUIActive)
            {
                return true;
            }
            tmpPawns.Clear();
            tmpPawns.AddRange((IEnumerable<Pawn>)caravan.PawnsListForReading);
            for (int index = 0; index < tmpPawns.Count; ++index)
            {
                IntVec3 loc = spawnCellGetter(tmpPawns[index]);
                GenSpawn.Spawn((Thing)tmpPawns[index], loc, map, Rot4.Random, WipeMode.Vanish, false);
            }
            /*
            switch (dropInventoryMode)
            {
                case CaravanDropInventoryMode.DropInstantly:
                    CaravanEnterMapUtility.DropAllInventory(tmpPawns);
                    break;
                case CaravanDropInventoryMode.UnloadIndividually:
                    for (int index = 0; index < tmpPawns.Count; ++index)
                        tmpPawns[index].inventory.UnloadEverything = true;
                    break;
            }
            */
            CaravanEnterMapUtility.DropAllInventory(tmpPawns);
            /*
            if (draftColonists)
                CaravanEnterMapUtility.DraftColonists(tmpPawns);
            if (map.IsPlayerHome)
            {
                for (int index = 0; index < tmpPawns.Count; ++index)
                {
                    if (tmpPawns[index].IsPrisoner)
                        tmpPawns[index].guest.WaitInsteadOfEscapingForDefaultTicks();
                }
            }
            */
            caravan.RemoveAllPawns();
            /*
            if (caravan.Spawned)
                Find.WorldObjects.Remove((WorldObject)caravan);
            */
            tmpPawns.Clear();
            return false;
        }
    }
    [HarmonyPatch(typeof(WorldSelector))]
    [HarmonyPatch("Select")]
    class SelectorPatch
    {
        static bool Prefix(WorldObject obj)
        {
            return !(obj.GetType() == typeof(Vessel));
        }
    }
    //changes passability so that vessels can work as intended; sets passability based on the selected object, if nothing is selected, all things are passable
    [HarmonyPatch(typeof(WorldPathGrid))]
    [HarmonyPatch("Passable")]
    class PassablityPatch
    {
        static bool Prefix (ref bool __result, int tile)
        {
            WorldObject selected = Find.WorldSelector.SingleSelectedObject;
            if (selected == null || EmbarkShipUtility.EmbarkUIActive)
            {
                __result = true;
                return false;
            }
            if (!VesselManager.WorldObjectIsNavigator(selected))
            {
                return true;
            }
            if (!Find.WorldGrid.InBounds(tile))
            {
                __result = false;
                return false;
            }
            __result = Find.WorldGrid[tile].biome.defName == "Ocean";
            return false;
        }
    }
    [HarmonyPatch(typeof(Caravan))]
    [HarmonyPatch("TicksPerMove", MethodType.Getter)]
    class TicksPerMovePatch
    {
        static void Postfix (ref int __result, Caravan __instance)
        {
            if (!VesselManager.WorldObjectIsNavigator(__instance))
            {
                return;
            }
            __result = 100000;
        }
    }
    [HarmonyPatch(typeof(Caravan))]
    [HarmonyPatch("CantMove", MethodType.Getter)]
    class CantMovePatch
    {
        static void Postfix(ref bool __result, Caravan __instance)
        {
            if (!VesselManager.WorldObjectIsNavigator(__instance))
            {
                return;
            }
            __result = false;
        }
    }
    [HarmonyPatch(typeof(MapParent))]
    [HarmonyPatch("Tick")]
    class CaravanTickPatch
    {
        static void Postfix(MapParent __instance)
        {
            if (!(__instance is Vessel))
            {
                return;
            }
            __instance.Tile = ((Vessel)__instance).caravan.Tile;
        }
    }
    [HarmonyPatch(typeof(WorldObject))]
    [HarmonyPatch("DrawPos", MethodType.Getter)]
    class VesselDrawPosPatch
    {
        static void Postfix(ref Vector3 __result, WorldObject __instance)
        {
            if (!(__instance is Vessel))
            {
                return;
            }
            __result = ((Vessel)__instance).caravan.DrawPos;
        }
    }
    [HarmonyPatch(typeof(CaravanTweenerUtility))]
    [HarmonyPatch("PatherTweenedPosRoot")]
    class TweenerPatch
    {
        static bool Prefix(Caravan caravan, ref Vector3 __result)
        {
            if (!VesselManager.WorldObjectIsNavigator(caravan))
            {
                return true;
            }
            WorldGrid worldGrid = Find.WorldGrid;
            if (!caravan.Spawned)
            {
                __result = worldGrid.GetTileCenter(caravan.Tile);
                return false;
            }
            if (!caravan.pather.Moving)
            {
                __result = worldGrid.GetTileCenter(caravan.Tile);
                return false;
            }
            float num = worldGrid[caravan.pather.nextTile].biome.defName.Equals("Ocean") ? (float)(1.0 - (double)caravan.pather.nextTileCostLeft / (double)caravan.pather.nextTileCostTotal) : 0.0f;
            int tileID = caravan.pather.nextTile != caravan.Tile || caravan.pather.previousTileForDrawingIfInDoubt == -1 ? caravan.Tile : caravan.pather.previousTileForDrawingIfInDoubt;
            __result = worldGrid.GetTileCenter(caravan.pather.nextTile) * num + worldGrid.GetTileCenter(tileID) * (1f - num);
            return false;
        }
    }
    [HarmonyPatch(typeof(Caravan_PathFollower))]
    [HarmonyPatch("CostToMove")]
    [HarmonyPatch(new Type[] { typeof(Caravan), typeof(int), typeof(int), typeof(int?) })]
    class CostToMovePatch
    {
        static bool Prefix (ref int __result, Caravan caravan, int start, int end, int? ticksAbs = null)
        {
            if (!VesselManager.WorldObjectIsNavigator(caravan))
            {
                return true;
            }

            int caravanTicksPerMove = caravan.TicksPerMove;
            bool perceivedStatic = false;
            StringBuilder explanation = null;
            string caravanTicksPerMoveExplanation = null;

            if (start == end)
                __result = 0;
            if (explanation != null)
            {
                explanation.Append(caravanTicksPerMoveExplanation);
                explanation.AppendLine();
            }
            StringBuilder explanation1 = explanation == null ? (StringBuilder)null : new StringBuilder();
            float num1 = !perceivedStatic || explanation != null ? WorldPathGrid.CalculatedMovementDifficultyAt(end, perceivedStatic, ticksAbs, explanation1) : Find.WorldPathGrid.PerceivedMovementDifficultyAt(end);
            //float difficultyMultiplier = Find.WorldGrid.GetRoadMovementDifficultyMultiplier(start, end, explanation1);
            float difficultyMultiplier = 0;
            if (explanation != null)
            {
                explanation.AppendLine();
                explanation.Append("TileMovementDifficulty".Translate() + ":");
                explanation.AppendLine();
                explanation.Append(explanation1.ToString().Indented("  "));
                explanation.AppendLine();
                explanation.Append("  = " + (num1 * difficultyMultiplier).ToString("0.#"));
            }
            int num2 = Mathf.Clamp((int)((double)caravanTicksPerMove * (double)num1 * (double)difficultyMultiplier), 1, 30000);
            if (explanation != null)
            {
                explanation.AppendLine();
                explanation.AppendLine();
                explanation.Append("FinalCaravanMovementSpeed".Translate() + ":");
                int num3 = Mathf.CeilToInt((float)num2 / 1f);
                explanation.AppendLine();
                explanation.Append("  " + (60000f / (float)caravanTicksPerMove).ToString("0.#") + " / " + (num1 * difficultyMultiplier).ToString("0.#") + " = " + (60000f / (float)num3).ToString("0.#") + " " + "TilesPerDay".Translate());
            }
            __result = num2;

            return false;
        }
    }
    [HarmonyPatch(typeof(LordJob_FormAndSendCaravan), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(List<TransferableOneWay>), typeof(List<Pawn>), typeof(IntVec3), typeof(IntVec3), typeof(int), typeof(int) })]
    class VesselFormJobCtorPatch
    {
        static void Postfix(LordJob_FormAndSendCaravan __instance)
        {
            if (EmbarkShipUtility.EmbarkUIActive)
            {
                VesselManager.AddVesselEmbark(__instance, EmbarkShipUtility.FindShipyardInMap(((MapParent)Find.WorldSelector.SingleSelectedObject).Map));
            }
        }
    }
    [HarmonyPatch(typeof(CaravanMaker))]
    [HarmonyPatch("MakeCaravan")]
    class VesselSpawnPatch
    {
        static void Prefix(ref int startingTile)
        {
            if (VesselManager.FormVessel)
            {
                startingTile = EmbarkShipUtility.AdjacentOceanTile(startingTile);
            }
        }
    }
    [HarmonyPatch(typeof(LordJob_FormAndSendCaravan))]
    [HarmonyPatch("SendCaravan")]
    class VesselFormJobPatch
    {
        static void Prefix(LordJob_FormAndSendCaravan __instance)
        {
            if (VesselManager.CaravanJobIsForVessel(__instance))
            {
                VesselManager.ActivateVesselForming(__instance);
            }
        }
        static void Postfix(LordJob_FormAndSendCaravan __instance)
        {
            if (VesselManager.FormVessel)
            {
                VesselManager.DeactivateVesselForming(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(Caravan_PathFollower))]
    [HarmonyPatch("TryRecoverFromUnwalkablePosition")]
    class PreventTeleportPatch
    {
        static bool Prefix(ref bool __result, Caravan ___caravan)
        {
            if (Find.WorldGrid[___caravan.Tile].biome.defName.Equals("Ocean"))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(CaravanExitMapUtility))]
    [HarmonyPatch("ExitMapAndCreateCaravan")]
    [HarmonyPatch(new Type[] { typeof(IEnumerable<Pawn>), typeof(Faction), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    class PostCaravanCreationPatch
    {
        static void Postfix(Caravan __result)
        {
            if (VesselManager.FormVessel)
            {
                __result.def.selectable = false;
                Vessel factionBase = (Vessel)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("Vessel"));
                factionBase.caravan = __result;
                factionBase.Tile = __result.Tile;
                factionBase.SetFaction(Faction.OfPlayer);
                Find.WorldObjects.Add((WorldObject)factionBase);
                factionBase.structure = new Vessel_Structure(VesselManager.ActiveShipyard);
                WaterGenerator.cachedStructure = factionBase.structure;
                Map newMap;
                newMap = GetOrGenerateMapUtility.GetOrGenerateMap(__result.Tile, Find.World.info.initialMapSize, null);
                //ISSUE: pawns are placed at the center of the map, which may or may not be a valid location
                //ISSUE: weather warning message
                CaravanEnterMapUtility.Enter(__result, newMap, (Func<Pawn, IntVec3>)(p => factionBase.Map.Center));
            }
        }
    }
    [HarmonyPatch(typeof(WorldObjectsHolder))]
    [HarmonyPatch("Remove")]
    class CaravanAntiDeletionPatch
    {
        static bool Prefix(WorldObject o)
        {
            if (VesselManager.WorldObjectIsNavigator(o) && VesselManager.FormVessel)
            {
                return false;
            }
            return true;
        }
    }
    //ISSUE: check if this solves a bug, or is not neccessary
    [HarmonyPatch(typeof(ColonistBar))]
    [HarmonyPatch("CheckRecacheEntries")]
    class HideEmptyCaravansPatch
    {
        static bool Prefix(ColonistBar __instance, ref bool ___entriesDirty, ref List<Map> ___tmpMaps, ref List<Pawn> ___tmpPawns, ref List<Caravan> ___tmpCaravans, ref List<ColonistBar.Entry> ___cachedEntries, ref ColonistBarDrawLocsFinder ___drawLocsFinder, ref List<Vector2> ___cachedDrawLocs, ref float ___cachedScale)
        {
            if (!___entriesDirty)
                return false;
            ___entriesDirty = false;
            ___cachedEntries.Clear();
            if (Find.PlaySettings.showColonistBar)
            {
                ___tmpMaps.Clear();
                ___tmpMaps.AddRange((IEnumerable<Map>)Find.Maps);
                ___tmpMaps.SortBy<Map, bool, int>((Func<Map, bool>)(x => !x.IsPlayerHome), (Func<Map, int>)(x => x.uniqueID));
                int group = 0;
                for (int index1 = 0; index1 < ___tmpMaps.Count; ++index1)
                {
                    ___tmpPawns.Clear();
                    ___tmpPawns.AddRange(___tmpMaps[index1].mapPawns.FreeColonists);
                    List<Thing> thingList = ___tmpMaps[index1].listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                    for (int index2 = 0; index2 < thingList.Count; ++index2)
                    {
                        if (!thingList[index2].IsDessicated())
                        {
                            Pawn innerPawn = ((Corpse)thingList[index2]).InnerPawn;
                            if (innerPawn != null && innerPawn.IsColonist)
                                ___tmpPawns.Add(innerPawn);
                        }
                    }
                    List<Pawn> allPawnsSpawned = ___tmpMaps[index1].mapPawns.AllPawnsSpawned;
                    for (int index2 = 0; index2 < allPawnsSpawned.Count; ++index2)
                    {
                        Corpse carriedThing = allPawnsSpawned[index2].carryTracker.CarriedThing as Corpse;
                        if (carriedThing != null && !carriedThing.IsDessicated() && carriedThing.InnerPawn.IsColonist)
                            ___tmpPawns.Add(carriedThing.InnerPawn);
                    }
                    PlayerPawnsDisplayOrderUtility.Sort(___tmpPawns);
                    for (int index2 = 0; index2 < ___tmpPawns.Count; ++index2)
                        ___cachedEntries.Add(new ColonistBar.Entry(___tmpPawns[index2], ___tmpMaps[index1], group));
                    if (!___tmpPawns.Any<Pawn>())
                        ___cachedEntries.Add(new ColonistBar.Entry((Pawn)null, ___tmpMaps[index1], group));
                    ++group;
                }
                ___tmpCaravans.Clear();
                ___tmpCaravans.AddRange((IEnumerable<Caravan>)Find.WorldObjects.Caravans);
                ___tmpCaravans.SortBy<Caravan, int>((Func<Caravan, int>)(x => x.ID));
                for (int index1 = 0; index1 < ___tmpCaravans.Count; ++index1)
                {
                    //change from original
                    if (___tmpCaravans[index1].IsPlayerControlled && ___tmpCaravans[index1].pawns.Count != 0)
                    {
                        ___tmpPawns.Clear();
                        ___tmpPawns.AddRange((IEnumerable<Pawn>)___tmpCaravans[index1].PawnsListForReading);
                        PlayerPawnsDisplayOrderUtility.Sort(___tmpPawns);
                        for (int index2 = 0; index2 < ___tmpPawns.Count; ++index2)
                        {
                            if (___tmpPawns[index2].IsColonist)
                                ___cachedEntries.Add(new ColonistBar.Entry(___tmpPawns[index2], (Map)null, group));
                        }
                        ++group;
                    }
                }
            }
            __instance.drawer.Notify_RecachedEntries();
            ___tmpPawns.Clear();
            ___tmpMaps.Clear();
            ___tmpCaravans.Clear();
            ___drawLocsFinder.CalculateDrawLocs(___cachedDrawLocs, out ___cachedScale);

            return false;
        }
    }
}