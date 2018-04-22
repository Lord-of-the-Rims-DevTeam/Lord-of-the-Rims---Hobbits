using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using Verse;

namespace Hobbits
{
	public class Building_PartyTreeGrower : Building_PlantGrower
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
					DamageInfo dinfo = new DamageInfo(DamageDefOf.Rotting, 4, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
					plant.TakeDamage(dinfo);
				}
			}
		}

		public override string GetInspectString()
		{
			return Regex.Replace(base.GetInspectString(), @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
		}

		public override void DeSpawn()
		{
			foreach (Plant plant in this.PlantsOnMe.ToList<Plant>())
			{
				plant.Destroy(DestroyMode.Vanish);
			}
			base.DeSpawn();
		}

		private ThingDef plantDefToGrow;

		private CompPowerTrader compPower;
	}
}
