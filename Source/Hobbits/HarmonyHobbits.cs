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
            harmony.Patch(AccessTools.Method(typeof(RCellFinder), "TryFindPartySpot"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(TryFindPartySpot_PostFix)), null);
        }

        //RCellFinder
        public static void TryFindPartySpot_PostFix(Pawn organizer, ref IntVec3 result, ref bool __result)
        {
            bool enjoyableOutside = JoyUtility.EnjoyableOutsideNow(organizer, null);
            Map map = organizer.Map;

            if (map.listerThings.ThingsOfDef(ThingDef.Named("LotRH_PlantPartyTree")).Any(x => x is Plant y && y.HarvestableNow))
            {
                Predicate<IntVec3> baseValidator = delegate(IntVec3 cell)
                {
                    if (!cell.Standable(map))
                    {
                        return false;
                    }
                    if (cell.GetDangerFor(organizer, map) != Danger.None)
                    {
                        return false;
                    }
                    if (!enjoyableOutside && !cell.Roofed(map))
                    {
                        return false;
                    }
                    if (cell.IsForbidden(organizer))
                    {
                        return false;
                    }
                    if (!organizer.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None, 1, -1, null, false))
                    {
                        return false;
                    }
                    Room room = cell.GetRoom(map, RegionType.Set_Passable);
                    bool flag = room != null && room.isPrisonCell;
                    return organizer.IsPrisoner == flag && PartyUtility.EnoughPotentialGuestsToStartParty(map, new IntVec3?(cell));
                };
                if ((from x in map.listerThings.ThingsOfDef(ThingDef.Named("LotRH_PlantPartyTree")).Where(x => x is Plant y && y.HarvestableNow)
                    where baseValidator(x.InteractionCell)
                    select x.InteractionCell).TryRandomElement(out result))
                {
                    __result = true;
                }
            }
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
