using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hobbits;

public class JobDriver_SolvePuzzle : JobDriver
{
    private const TargetIndex PuzzleBoxInd = TargetIndex.A;
    private const TargetIndex joySpot = TargetIndex.B;

    private Thing PuzzleBox => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool yeaaa)
    {
        return pawn.Reserve(PuzzleBox, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(PuzzleBoxInd, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(PuzzleBoxInd);
        yield return Toils_Ingest.PickupIngestible(PuzzleBoxInd, pawn);
        yield return CarryPuzzleToSpot(pawn, PuzzleBoxInd);
        yield return Toils_Ingest.FindAdjacentEatSurface(joySpot, PuzzleBoxInd);
        var puzzle = new Toil
        {
            tickAction = WaitTickAction()
        };
        puzzle.AddFinishAction(() =>
        {
            JoyUtility.TryGainRecRoomThought(pawn);
            RollForLuck();
        });
        puzzle.defaultCompleteMode = ToilCompleteMode.Delay;
        puzzle.defaultDuration = job.def.joyDuration;
        puzzle.handlingFacing = true;
        yield return puzzle;
    }

    protected void RollForLuck()
    {
        var extraLuckFromQuality = TargetThingA.GetStatValue(StatDefOf.JoyGainFactor);
        float extraLuckFromHobbitSmarts = pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt;

        var yourLuckyNumber = (1f + extraLuckFromHobbitSmarts) * extraLuckFromQuality / 100;

        Log.Message("lucky number is: " + yourLuckyNumber);

        if (!Rand.Chance(yourLuckyNumber) && !DebugSettings.godMode) return;

        var reward = ThingMaker.MakeThing(ThingDefOf.Gold);
        reward.stackCount = Rand.RangeInclusive(10, 50);
        GenSpawn.Spawn(reward, pawn.Position, pawn.Map);
        PuzzleBox.Destroy();
        Letter letter = LetterMaker.MakeLetter("LotRH_PuzzleSolvedLabel".Translate(),
            "LotRH_PuzzleSolved".Translate(pawn.Label, reward.Label), LetterDefOf.PositiveEvent);
        Find.LetterStack.ReceiveLetter(letter);
    }

    protected Action WaitTickAction()
    {
        return delegate
        {
            pawn.rotationTracker.FaceCell(TargetB.Cell);
            pawn.GainComfortFromCellIfPossible();
            var extraJoyGainFactor = TargetThingA.GetStatValue(StatDefOf.JoyGainFactor);
            JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor);
        };
    }

    //slightly modified version of Toils_Ingest.CarryIngestibleToChewSpot
    public static Toil CarryPuzzleToSpot(Pawn pawn, TargetIndex puzzleInd)
    {
        var toil = new Toil();
        toil.initAction = delegate
        {
            var actor = toil.actor;
            var intVec = IntVec3.Invalid;
            var thing2 = actor.CurJob.GetTarget(puzzleInd).Thing;

            bool baseChairValidator(Thing t)
            {
                if (t.def.building == null || !t.def.building.isSittable) return false;

                if (t.IsForbidden(pawn)) return false;

                if (!actor.CanReserve(t)) return false;

                if (!t.IsSociallyProper(actor)) return false;

                if (t.IsBurning()) return false;

                if (t.HostileTo(pawn)) return false;

                var result = false;
                for (var i = 0; i < 4; i++)
                {
                    var c = t.Position + GenAdj.CardinalDirections[i];
                    var edifice = c.GetEdifice(t.Map);
                    if (edifice == null || edifice.def.surfaceType != SurfaceType.Eat) continue;

                    result = true;
                    break;
                }

                return result;
            }

            //if you can find a table with chair, great. If not, go to your room.

            var thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell,
                TraverseParms.For(actor),
                30f, //"chair search radius"
                t => baseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);

            if (thing == null)
                if (pawn.ownership?.OwnedRoom != null)
                    (from c in pawn.ownership.OwnedRoom.Cells
                        where c.Standable(pawn.Map) && !c.IsForbidden(pawn) &&
                              pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.None)
                        select c).TryRandomElement(out intVec);

            if (thing != null)
            {
                intVec = thing.Position;
                actor.Reserve(thing, actor.CurJob);
            }

            if (intVec == IntVec3.Invalid) intVec = RCellFinder.SpotToChewStandingNear(pawn, thing2);

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