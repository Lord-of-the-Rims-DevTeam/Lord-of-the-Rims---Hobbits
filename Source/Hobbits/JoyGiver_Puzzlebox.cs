using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using LordOfTheRims_Hobbits;
using Verse;
using Verse.AI;

namespace Hobbits
{
    public class JoyGiver_Puzzlebox : JoyGiver_Ingest
    {
        /// <summary>
        /// Similar to how a pawn can be a social drinker or solitary relaxer in their room, hobbits can try and solve a puzzle box.
        /// </summary>

        public override float GetChance(Pawn pawn)
        {
            //only for hobbits!
            if (pawn.def != DefDatabase<AlienRace.ThingDef_AlienRace>.GetNamed("LotRH_HobbitStandardRace")) return 0f;
            else return def.baseChance;
        }

        //hobbits can puzzle during a party and when they're by themselves: hence the dual-job
        public override Job TryGiveJob(Pawn pawn)
        {
            return this.TryGiveJobInternal(pawn, null);
        }

        public override Job TryGiveJobInPartyArea(Pawn pawn, IntVec3 partySpot)
        {
            return this.TryGiveJobInternal(pawn, (Thing x) => !x.Spawned || PartyUtility.InPartyArea(x.Position, partySpot, pawn.Map));
        }

        private Job TryGiveJobInternal(Pawn pawn, Predicate<Thing> extraValidator)
        {
            Thing thing = this.BestIngestItem(pawn, extraValidator);
            if (thing != null)
            {
                return this.CreateIngestJob(thing, pawn);
            }
            return null;
        }

        protected override Thing BestIngestItem(Pawn pawn, Predicate<Thing> extraValidator)
        {
            //Find the puzzle box.
            Predicate<Thing> predicate = (Thing t) => (t.def == DefDatabase<ThingDef>.GetNamed("LotRH_HobbitPuzzleBox")) && pawn.CanReserve(t) && (extraValidator == null || extraValidator(t));
            List<Thing> searchSet = new List<Thing>();
            this.GetSearchSet(pawn, searchSet);
            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);

            return GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, searchSet, PathEndMode.OnCell, traverseParams, 9999f, predicate, null);
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
