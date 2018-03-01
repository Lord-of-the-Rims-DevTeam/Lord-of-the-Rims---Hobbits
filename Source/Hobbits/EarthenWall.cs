using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordOfTheRims_Hobbits
{
    public class EarthenWall : Building
    {
        private bool hasGrass = false;
        public bool HasSnow => this.MapHeld.snowGrid.GetCategory(this.PositionHeld) != SnowCategory.None;

        private float growthInt = 0.05f;
        private int unlitTicks;
        private int ageInt;
        private string cachedLabelMouseover;
        private float growDays = 3.5f;
        private float growMinGlow = 0.3f;
        private float growOptimalGlow = 1.0f;

        public override Graphic Graphic
        {
            get
            {
                if (HasSnow)
                    return this.def.building.trapUnarmedGraphicData.GraphicColoredFor(this);
                else if (LifeStage == PlantLifeStage.Mature || hasGrass)
                    return this.def.building.fullGraveGraphicData.GraphicColoredFor(this);
                return base.Graphic;
            }
        }
        
        
        public virtual PlantLifeStage LifeStage
        {
            get
            {
                if (this.growthInt < 0.001f)
                {
                    return PlantLifeStage.Sowing;
                }
                if (this.growthInt > 0.999f)
                {
                    return PlantLifeStage.Mature;
                }
                return PlantLifeStage.Growing;
            }
        }

        public virtual bool Resting => GenLocalDate.DayPercent(this) < 0.25f || GenLocalDate.DayPercent(this) > 0.8f;

        protected float GrowthPerTick
        {
            get
            {
                if (this.LifeStage != PlantLifeStage.Growing || this.Resting)
                {
                    return 0f;
                }
                float num = 1f / (60000f * growDays);
                return num * this.GrowthRate;
            }
        }

        private float GrowthRateFactor_Light
        {
            get
            {
                float num = base.Map.glowGrid.GameGlowAt(base.Position, false);
                return GenMath.InverseLerp(growMinGlow, growOptimalGlow, num);
            }
        }
        
        protected virtual bool HasEnoughLightToGrow => this.GrowthRateFactor_Light > 0.001f;

        private string GrowthPercentString => (this.growthInt + 0.0001f).ToStringPercent();

        public override string LabelMouseover
        {
            get
            {
                if (this.cachedLabelMouseover == null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(this.def.LabelCap);
                    stringBuilder.Append(" (" + "PercentGrowth".Translate(new object[]
                    {
                        this.GrowthPercentString
                    }));
                    stringBuilder.Append(")");
                    this.cachedLabelMouseover = stringBuilder.ToString();
                }
                return this.cachedLabelMouseover;
            }
        }
        
        public override void TickRare()
        {
            base.TickRare();
            if (base.Destroyed)
            {
                return;
            }

            if (Spawned)
            {
                base.Map.mapDrawer.MapMeshDirty(base.Position, MapMeshFlag.Things);
                //this.DirtyMapMesh(this.MapHeld);

                if (GenPlant.GrowthSeasonNow(base.Position, base.Map))
                {
                    this.growthInt += this.GrowthPerTick * 2000f;
                    if (this.growthInt > 1f)
                    {
                        this.growthInt = 1f;
                    }
                }
                if (!this.HasEnoughLightToGrow)
                {
                    this.unlitTicks += 2000;
                }
                else
                {
                    this.unlitTicks = 0;
                }
                this.ageInt += 2000;
                this.cachedLabelMouseover = null;

            }
        }

        private float GrowthRate
        {
            get
            {
                //return this.GrowthRateFactor_Fertility * this.GrowthRateFactor_Temperature * this.GrowthRateFactor_Light;
                return 1 * this.GrowthRateFactor_Temperature * this.GrowthRateFactor_Light;
            }
        }

        private float GrowthRateFactor_Temperature
        {
            get
            {
                float num;
                if (!GenTemperature.TryGetTemperatureForCell(base.Position, base.Map, out num))
                {
                    return 1f;
                }
                if (num < 10f)
                {
                    return Mathf.InverseLerp(0f, 10f, num);
                }
                if (num > 42f)
                {
                    return Mathf.InverseLerp(58f, 42f, num);
                }
                return 1f;
            }
        }
        
        
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.LifeStage == PlantLifeStage.Growing)
            {
                stringBuilder.AppendLine("PercentGrowth".Translate(new object[]
                {
                    this.GrowthPercentString
                }));
                stringBuilder.AppendLine("GrowthRate".Translate() + ": " + this.GrowthRate.ToStringPercent());
                    if (this.Resting)
                    {
                        stringBuilder.AppendLine("PlantResting".Translate());
                    }
                    if (!this.HasEnoughLightToGrow)
                    {
                        stringBuilder.AppendLine("PlantNeedsLightLevel".Translate() + ": " + growMinGlow.ToStringPercent());
                    }
            }
            else if (this.LifeStage == PlantLifeStage.Mature)
            {
                stringBuilder.AppendLine("Mature".Translate());
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            //if (!hasGrass)
            //{
            if (DebugSettings.godMode)
            {
                yield return new Command_Toggle()
                {
                    defaultLabel = "DEV: Switch grass mode",
                    icon = TexCommand.TreeChop,
                    isActive = () => hasGrass,
                    activateSound = SoundDefOf.Click,
                    toggleAction = () =>
                    {
                        hasGrass = !hasGrass;
                        this.DirtyMapMesh(this.MapHeld);
                    } //NeedsGrassPlanted = !NeedsGrassPlanted
                };
            } 
            //}
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.growthInt, "growth", 0f, false);
            Scribe_Values.Look<int>(ref this.ageInt, "age", 0, false);
            Scribe_Values.Look<int>(ref this.unlitTicks, "unlitTicks", 0, false);
        }
    }
}