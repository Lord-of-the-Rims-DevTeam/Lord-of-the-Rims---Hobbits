using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using LordOfTheRims_Hobbits;
using UnityEngine;
using Verse.AI.Group;

namespace JecsHuntingWithTraps
{

    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.lotr.hobbits");
            harmony.Patch(AccessTools.Method(typeof(SnowGrid), "CanHaveSnow"), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(CanHaveSnow_PostFix)), null);
        }

        public static void CanHaveSnow_PostFix(SnowGrid __instance, int ind, ref bool __result)
        {
            var map = Traverse.Create(__instance).Field("map").GetValue<Map>();
            if (map == null) return;
            var building = map.edificeGrid[ind];
            if (building is EarthenWall w)
            {
                __result = true;
                if (Find.TickManager.TicksGame % 250 == 0)
                    w.DirtyMapMesh(w.MapHeld);
            }
        }

    }
}
