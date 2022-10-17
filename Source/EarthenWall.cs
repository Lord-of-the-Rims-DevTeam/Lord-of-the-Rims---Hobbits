using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hobbits;

public class EarthenWall : Building
{
    private readonly float growDays = 3.5f;
    private readonly float growMinGlow = 0.3f;
    private readonly float growOptimalGlow = 1.0f;
    private int ageInt;
    private string cachedLabelMouseover;

    private float growthInt = 0.05f;
    private bool hasGrass;
    private int unlitTicks;
    public bool HasSnow => MapHeld.snowGrid.GetCategory(PositionHeld) != SnowCategory.None;

    public override Graphic Graphic
    {
        get
        {
            if (HasSnow) return def.building.trapUnarmedGraphicData.GraphicColoredFor(this);

            if (LifeStage == PlantLifeStage.Mature || hasGrass)
                return def.building.fullGraveGraphicData.GraphicColoredFor(this);

            return base.Graphic;
        }
    }


    public virtual PlantLifeStage LifeStage
    {
        get
        {
            if (growthInt < 0.001f) return PlantLifeStage.Sowing;

            if (growthInt > 0.999f) return PlantLifeStage.Mature;

            return PlantLifeStage.Growing;
        }
    }

    public virtual bool Resting => GenLocalDate.DayPercent(this) < 0.25f || GenLocalDate.DayPercent(this) > 0.8f;

    protected float GrowthPerTick
    {
        get
        {
            if (LifeStage != PlantLifeStage.Growing || Resting) return 0f;

            var num = 1f / (60000f * growDays);
            return num * GrowthRate;
        }
    }

    private float GrowthRateFactor_Light
    {
        get
        {
            var num = Map.glowGrid.GameGlowAt(Position);
            return GenMath.InverseLerp(growMinGlow, growOptimalGlow, num);
        }
    }

    protected virtual bool HasEnoughLightToGrow => GrowthRateFactor_Light > 0.001f;

    private string GrowthPercentString => (growthInt + 0.0001f).ToStringPercent();

    public override string LabelMouseover
    {
        get
        {
            if (cachedLabelMouseover != null) return cachedLabelMouseover;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(def.LabelCap);
            stringBuilder.Append(" (" + "PercentGrowth".Translate(GrowthPercentString));
            stringBuilder.Append(")");
            cachedLabelMouseover = stringBuilder.ToString();

            return cachedLabelMouseover;
        }
    }

    private float GrowthRate =>
        //return this.GrowthRateFactor_Fertility * this.GrowthRateFactor_Temperature * this.GrowthRateFactor_Light;
        1 * GrowthRateFactor_Temperature * GrowthRateFactor_Light;

    private float GrowthRateFactor_Temperature
    {
        get
        {
            if (!GenTemperature.TryGetTemperatureForCell(Position, Map, out var num)) return 1f;

            if (num < 10f) return Mathf.InverseLerp(0f, 10f, num);

            if (num > 42f) return Mathf.InverseLerp(58f, 42f, num);

            return 1f;
        }
    }

    public override void TickRare()
    {
        base.TickRare();
        if (Destroyed) return;

        if (!Spawned) return;

        Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
        //this.DirtyMapMesh(this.MapHeld);

        if (PlantUtility.GrowthSeasonNow(Position, Map))
        {
            growthInt += GrowthPerTick * 2000f;
            if (growthInt > 1f) growthInt = 1f;
        }

        if (!HasEnoughLightToGrow)
            unlitTicks += 2000;
        else
            unlitTicks = 0;

        ageInt += 2000;
        cachedLabelMouseover = null;
    }


    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        if (LifeStage == PlantLifeStage.Growing)
        {
            stringBuilder.AppendLine("PercentGrowth".Translate(GrowthPercentString));
            stringBuilder.AppendLine("GrowthRate".Translate() + ": " + GrowthRate.ToStringPercent());
            if (Resting) stringBuilder.AppendLine("PlantResting".Translate());

            if (!HasEnoughLightToGrow)
                stringBuilder.AppendLine("PlantNeedsLightLevel".Translate() + ": " + growMinGlow.ToStringPercent());
        }
        else if (LifeStage == PlantLifeStage.Mature)
        {
            stringBuilder.AppendLine("Mature".Translate());
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        //if (!hasGrass)
        //{
        if (DebugSettings.godMode)
            yield return new Command_Toggle
            {
                defaultLabel = "DEV: Switch grass mode",
                icon = TexCommand.ToggleVent,
                isActive = () => hasGrass,
                activateSound = SoundDefOf.Click,
                toggleAction = () =>
                {
                    hasGrass = !hasGrass;
                    DirtyMapMesh(MapHeld);
                } //NeedsGrassPlanted = !NeedsGrassPlanted
            };
        //}
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref growthInt, "growth");
        Scribe_Values.Look(ref ageInt, "age");
        Scribe_Values.Look(ref unlitTicks, "unlitTicks");
    }
}