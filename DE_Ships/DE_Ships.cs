using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using System.Reflection;
using RimWorld;


namespace DE_Ships
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("DETestMod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            GenStepParams emptyParams = new GenStepParams();
            WaterGenerator.oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("GenStep_Ocean"), emptyParams));
            WaterGenerator.oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("FindPlayerStartSpot"), emptyParams));
            WaterGenerator.oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("ScenParts"), emptyParams));
            WaterGenerator.oceanGenSteps.Add(new GenStepWithParams(DefDatabase<GenStepDef>.GetNamed("Fog"), emptyParams));
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
        }
    }

    public static class WaterGenerator
    {
        public static List<GenStepWithParams> oceanGenSteps = new List<GenStepWithParams>();


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
