using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using Verse;

namespace Hobbits
{
	public class Building_PartyTreeGrower : Building_PlantGrower, IThoughtGiver
	{
		public override void PostMake()
		{
			base.PostMake();
			this.plantDefToGrow = this.def.building.defaultPlantToGrow;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			this.compPower = base.GetComp<CompPowerTrader>();
			this.compGlower = base.GetComp<CompGlower>();
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.GrowingFood, KnowledgeAmount.Total);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<ThingDef>(ref this.plantDefToGrow, "plantDefToGrow");
		}
		
		

		public override void TickRare()
		{
			if (this.compPower != null && !this.compPower.PowerOn)
			{
				foreach (Plant plant in this.PlantsOnMe)
				{
					DamageInfo dinfo = new DamageInfo(DamageDefOf.Rotting, 4, 1f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
					plant.TakeDamage(dinfo);
				}
			}
			this.compGlower?.UpdateLit(MapHeld);
		}
		
		public override string GetInspectString()
		{
			return Regex.Replace(base.GetInspectString().TrimEndNewlines(), @"^\s+$[\r\n]*", "", RegexOptions.Multiline).TrimEndNewlines();
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			foreach (Plant plant in this.PlantsOnMe.ToList<Plant>())
			{
				plant.Destroy(DestroyMode.Vanish);
			}
			base.DeSpawn(mode);
		}

		private ThingDef plantDefToGrow;

		private CompPowerTrader compPower;
		private Graphic partyGraphicInt;
		private CompGlower compGlower;
		public Thought_Memory GiveObservedThought()
		{
			if (this.PlantsOnMe.First().LifeStage != PlantLifeStage.Mature) return null;
			var thought_MemoryObservation = (Thought_MemoryObservation) ThoughtMaker.MakeThought(ThoughtDefOf.ObservedLayingCorpse);
			thought_MemoryObservation.Target = this;
			return thought_MemoryObservation;
		}
	}
}
