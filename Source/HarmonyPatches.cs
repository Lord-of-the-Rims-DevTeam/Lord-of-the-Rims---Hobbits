using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hobbits;

[StaticConstructorOnStartup]
internal static class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("rimworld.lotr.hobbits");
        harmony.Patch(AccessTools.Method(typeof(SnowGrid), "CanHaveSnow"), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(CanHaveSnow_PostFix)));
        harmony.Patch(AccessTools.Method(typeof(RCellFinder), "TryFindGatheringSpot"), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(TryFindGatheringSpot_PostFix)));
        harmony.Patch(AccessTools.Method(typeof(Plant), "get_Graphic"), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(GetPartyTreeGraphic)));
        harmony.Patch(AccessTools.Method(typeof(CompGlower), "get_ShouldBeLitNow"), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(Get_PartyTree_ShouldBeLitNow)));
        harmony.Patch(AccessTools.Method(typeof(WorkGiver_GrowerHarvest), "HasJobOnCell"), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(HasJobOnCell_PostFix)));
    }

    //CompGlower
    public static void Get_PartyTree_ShouldBeLitNow(CompGlower __instance, ref bool __result)
    {
        if (__instance?.parent is not Plant_PartyTree p) return;

        if (p.Spawned && p.LifeStage == PlantLifeStage.Mature)
            __result = true;
        else
            __result = false;
    }

    // Do not automatically harvest Party Trees. Player can still designate it explicitly if they wish.
    public static void HasJobOnCell_PostFix(WorkGiver_GrowerHarvest __instance, Pawn pawn, IntVec3 c, bool forced,
        ref bool __result)
    {
        if (__result == false) return;

        if (c.GetPlant(pawn.Map) is Plant_PartyTree) __result = false;
    }

    //Plant
    public static void GetPartyTreeGraphic(Plant __instance, ref Graphic __result)
    {
        if (!(__instance is Plant_PartyTree)) return;

        if (__instance.LifeStage == PlantLifeStage.Sowing) return;

        if (__instance.def.plant.leaflessGraphic != null && __instance.LeaflessNow &&
            (!__instance.sown || !__instance.HarvestableNow))
            return;

        if (__instance.LifeStage == PlantLifeStage.Mature) return;

        // return the immature graphic even if it is harvestable, but not yet fully mature.
        // only return the mature graphic if it is actually in lifestage "Mature", i.e. lit with lanterns and providing the mood bonus.
        if (__instance.def.plant.immatureGraphic != null) __result = __instance.def.plant.immatureGraphic;
    }

    //RCellFinder
    public static void TryFindGatheringSpot_PostFix(Pawn organizer, ref IntVec3 result, ref bool __result)
    {
        var enjoyableOutside = JoyUtility.EnjoyableOutsideNow(organizer);
        var map = organizer.Map;

        if (!map.listerThings.ThingsOfDef(ThingDef.Named("LotRH_PlantPartyTree"))
                .Any(x => x is Plant { LifeStage: PlantLifeStage.Mature }))
            return;

        {
            bool baseValidator(IntVec3 cell)
            {
                if (cell.GetDangerFor(organizer, map) != Danger.None) return false;

                if (!enjoyableOutside && !cell.Roofed(map)) return false;

                if (cell.IsForbidden(organizer)) return false;

                if (!organizer.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None)) return false;

                var room = cell.GetRoom(map);
                return organizer.IsPrisoner == room is { IsPrisonCell: true } &&
                       GatheringsUtility.EnoughPotentialGuestsToStartGathering(map, GatheringDefOf.Party, cell);
            }

            if ((from x in map.listerThings.ThingsOfDef(ThingDef.Named("LotRH_PlantPartyTree"))
                        .Where(x => x is Plant { LifeStage: PlantLifeStage.Mature })
                    where baseValidator(x.Position)
                    select x.Position).TryRandomElement(out result))
                __result = true;
        }
    }

    public static void CanHaveSnow_PostFix(SnowGrid __instance, int ind, ref bool __result)
    {
        var map = Traverse.Create(__instance).Field("map").GetValue<Map>();
        if (map == null) return;

        var building = map.edificeGrid[ind];
        if (building is not EarthenWall w) return;

        __result = true;
        if (Find.TickManager.TicksGame % 250 == 0) w.DirtyMapMesh(w.MapHeld);
    }
}