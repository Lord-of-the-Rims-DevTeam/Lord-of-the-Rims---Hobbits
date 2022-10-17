using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hobbits;

public class PlaceWorker_Unique : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        _ = Find.CurrentMap;
        GenDraw.DrawFieldEdges(new List<IntVec3> { center + def.interactionCellOffset });
    }

    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        if (base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing).Accepted)
            return map.listerThings.ThingsOfDef(checkingDef as ThingDef).Any()
                ? AcceptanceReport.WasRejected
                : AcceptanceReport.WasAccepted;

        Messages.Message("LotRH_PartyTreePlacement".Translate(checkingDef.LabelCap), MessageTypeDefOf.RejectInput);
        return AcceptanceReport.WasRejected;
    }
}