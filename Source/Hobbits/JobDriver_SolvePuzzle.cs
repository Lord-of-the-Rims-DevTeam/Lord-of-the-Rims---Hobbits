using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Hobbits
{
    public class JobDriver_SolvePuzzle : JobDriver
    {
        private const TargetIndex PuzzleBoxInd = TargetIndex.A;
        private const TargetIndex joySpot = TargetIndex.B;

        private Thing PuzzleBox
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool yeaaa)
        {
            return this.pawn.Reserve(PuzzleBox, this.job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(PuzzleBoxInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(PuzzleBoxInd);
            yield return Toils_Ingest.PickupIngestible(PuzzleBoxInd, this.pawn);
            yield return CarryPuzzleToSpot(pawn, PuzzleBoxInd);
            yield return Toils_Ingest.FindAdjacentEatSurface(joySpot, PuzzleBoxInd);
            Toil puzzle;
            puzzle = new Toil();

            puzzle.tickAction = this.WaitTickAction();
            puzzle.AddFinishAction(() =>
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
                this.RollForLuck();
            });
            puzzle.defaultCompleteMode = ToilCompleteMode.Delay;
            puzzle.defaultDuration = this.job.def.joyDuration;
            puzzle.handlingFacing = true;
            yield return puzzle;
        }

        protected void RollForLuck()
        {
            float extraLuckFromQuality = base.TargetThingA.GetStatValue(StatDefOf.JoyGainFactor, true);
            float extraLuckFromHobbitSmarts = pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt;

            float yourLuckyNumber = ((1f + extraLuckFromHobbitSmarts) * extraLuckFromQuality) / 100;

            Log.Message("lucky number is: " + yourLuckyNumber.ToString());

            if (Rand.Chance(yourLuckyNumber) || DebugSettings.godMode)
            {
                Thing reward = ThingMaker.MakeThing(ThingDefOf.Gold);
                reward.stackCount = Rand.RangeInclusive(10, 50);
                GenSpawn.Spawn(reward, pawn.Position, pawn.Map);
                PuzzleBox.Destroy();
                Letter letter = LetterMaker.MakeLetter("LotRH_PuzzleSolvedLabel".Translate(), "LotRH_PuzzleSolved".Translate(new object[] {
                    pawn.Label,
                    reward.Label,
                }), LetterDefOf.PositiveEvent);
                Find.LetterStack.ReceiveLetter(letter);
            }
        }

        protected Action WaitTickAction()
        {
            return delegate
            {
                this.pawn.rotationTracker.FaceCell(base.TargetB.Cell);
                this.pawn.GainComfortFromCellIfPossible();
                float extraJoyGainFactor = base.TargetThingA.GetStatValue(StatDefOf.JoyGainFactor, true);
                JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor);
            };
        }

        //slightly modified version of Toils_Ingest.CarryIngestibleToChewSpot
        public static Toil CarryPuzzleToSpot(Pawn pawn, TargetIndex puzzleInd)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                IntVec3 intVec = IntVec3.Invalid;
                Thing thing = null;
                Thing thing2 = actor.CurJob.GetTarget(puzzleInd).Thing;
                Predicate<Thing> baseChairValidator = delegate (Thing t)
                {
                    if (t.def.building == null || !t.def.building.isSittable)
                    {
                        return false;
                    }
                    if (t.IsForbidden(pawn))
                    {
                        return false;
                    }
                    if (!actor.CanReserve(t, 1, -1, null, false))
                    {
                        return false;
                    }
                    if (!t.IsSociallyProper(actor))
                    {
                        return false;
                    }
                    if (t.IsBurning())
                    {
                        return false;
                    }
                    if (t.HostileTo(pawn))
                    {
                        return false;
                    }
                    bool result = false;
                    for (int i = 0; i < 4; i++)
                    {
                        IntVec3 c = t.Position + GenAdj.CardinalDirections[i];
                        Building edifice = c.GetEdifice(t.Map);
                        if (edifice != null && edifice.def.surfaceType == SurfaceType.Eat)
                        {
                            result = true;
                            break;
                        }
                    }
                    return result;
                };

                //if you can find a table with chair, great. If not, go to your room.

                thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, 
                    ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, 
                    TraverseParms.For(actor), 
                    30f, //"chair search radius"
                    (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);
                
                if (thing == null)
                {
                    if (pawn.ownership?.OwnedRoom != null)
                    {
                        (from c in pawn.ownership.OwnedRoom.Cells
                         where c.Standable(pawn.Map) && !c.IsForbidden(pawn) && pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None)
                         select c).TryRandomElement(out intVec);
                    }
                }
                if (thing != null)
                {
                    intVec = thing.Position;
                    actor.Reserve(thing, actor.CurJob, 1, -1, null);
                }
                if (intVec == IntVec3.Invalid)
                {
                    intVec = RCellFinder.SpotToChewStandingNear(pawn, thing2);
                }
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, intVec);
                actor.pather.StartPath(intVec, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        //public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        //{
        //    return base.ModifyCarriedThingDrawPos(ref drawPos, ref behind, ref flip);
        //}
    }
}
