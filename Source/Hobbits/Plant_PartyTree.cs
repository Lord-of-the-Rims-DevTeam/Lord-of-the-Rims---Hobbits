using RimWorld;
using Verse;

namespace Hobbits
{
    public class Plant_PartyTree : Plant, IObservedThoughtGiver
    {
        public override float Growth
        {
            get => base.Growth;
            set
            {
                base.Growth = value;
                var compGlower = this.TryGetComp<CompGlower>();
                compGlower.UpdateLit(Map);
            }
        }

        public Thought_Memory GiveObservedThought(Pawn observer)
        {
            if (LifeStage != PlantLifeStage.Mature)
            {
                return null;
            }

            var thought_MemoryObservation =
                (Thought_MemoryObservation) ThoughtMaker.MakeThought(ThoughtDefOf.ObservedPartyTree);
            thought_MemoryObservation.Target = this;
            return thought_MemoryObservation;
        }

        public HistoryEventDef GiveObservedHistoryEvent(Pawn observer)
        {
            return null;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var compGlower = this.TryGetComp<CompGlower>();
            compGlower.UpdateLit(map);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var map = Map;
            base.DeSpawn(mode);
            var compGlower = this.TryGetComp<CompGlower>();
            compGlower.UpdateLit(map);
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            var compGlower = this.TryGetComp<CompGlower>();
            compGlower.UpdateLit(Map);
        }
    }
}