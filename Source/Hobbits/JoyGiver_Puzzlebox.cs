using System;
using System.Collections.Generic;
using AlienRace;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hobbits
{
    public class JoyGiver_Puzzlebox : JoyGiver_Ingest
    {
        /// <summary>
        ///     Similar to how a pawn can be a social drinker or solitary relaxer in their room, hobbits can try and solve a puzzle
        ///     box.
        /// </summary>
        public override float GetChance(Pawn pawn)
        {
            //only for hobbits!
            if (pawn.def != DefDatabase<ThingDef_AlienRace>.GetNamed("LotRH_HobbitStandardRace"))
            {
                return 0f;
            }

            return def.baseChance;
        }

        //hobbits can puzzle during a party and when they're by themselves: hence the dual-job
        public override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJobInternal(pawn, null);
        }

        public override Job TryGiveJobInGatheringArea(Pawn pawn, IntVec3 partySpot, float maxRadius = -1f)
        {
            return TryGiveJobInternal(pawn,
                x => !x.Spawned || GatheringsUtility.InGatheringArea(x.Position, partySpot, pawn.Map));
        }

        private Job TryGiveJobInternal(Pawn pawn, Predicate<Thing> extraValidator)
        {
            var thing = BestIngestItem(pawn, extraValidator);
            if (thing != null)
            {
                return CreateIngestJob(thing, pawn);
            }

            return null;
        }

        protected override Thing BestIngestItem(Pawn pawn, Predicate<Thing> extraValidator)
        {
            //Find the puzzle box.
            bool predicate(Thing t)
            {
                return t.def == DefDatabase<ThingDef>.GetNamed("LotRH_HobbitPuzzleBox") && pawn.CanReserve(t) &&
                       (extraValidator == null || extraValidator(t));
            }

            var searchSet = new List<Thing>();
            GetSearchSet(pawn, searchSet);
            var traverseParams = TraverseParms.For(pawn);

            return GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, searchSet, PathEndMode.OnCell,
                traverseParams, 9999f, predicate);
        }

        protected override Job CreateIngestJob(Thing thing, Pawn pawn)
        {
            return new Job(DefDatabase<JobDef>.GetNamed("LotRH_SolvePuzzleBox"), thing)
            {
                count = 1
            };
        }
    }
}