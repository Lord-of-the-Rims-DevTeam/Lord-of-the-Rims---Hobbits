using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LordOfTheRims_Hobbits
{
    public class EarthenWall : Building
    {
        private bool hasGrass = false;
        public bool HasSnow => this.MapHeld.snowGrid.GetCategory(this.PositionHeld) != SnowCategory.None;

        public bool NeedsGrassPlanted = false;

        public override Graphic Graphic
        {
            get
            {
                if (HasSnow)
                    return this.def.building.trapUnarmedGraphicData.GraphicColoredFor(this);
                else if (hasGrass)
                    return this.def.building.fullGraveGraphicData.GraphicColoredFor(this);
                return base.Graphic;
            }
        }
        
        public override void TickRare()
        {
            base.TickRare();
            if (Spawned)
                this.DirtyMapMesh(this.MapHeld);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            //if (!hasGrass)
            //{
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
            //}
        }
    }
}