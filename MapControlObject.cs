namespace CavRn.HideMinimap
{
    using Eco.Core.Items;
    using Eco.Gameplay.Components.Auth;
    using Eco.Gameplay.Components;
    using Eco.Gameplay.Items.Recipes;
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Objects;
    using Eco.Gameplay.Occupancy;
    using Eco.Gameplay.Skills;
    using Eco.Mods.TechTree;
    using Eco.Shared.Items;
    using Eco.Shared.Localization;
    using Eco.Shared.Math;
    using Eco.Shared.Serialization;
    using System.Collections.Generic;
    using System;

    [Serialized]
    [RequireComponent(typeof(MapControlComponent))]
    [RequireComponent(typeof(OccupancyRequirementComponent))]
    [RequireComponent(typeof(PropertyAuthComponent))]
    [Tag("Usable")]
    [Ecopedia("Housing Objects", "Cultural", subPageName: "Map Control")]
    public class MapControlObject : WorldObject, IRepresentsItem
    {
        public virtual Type RepresentedItemType => typeof(MapControlItem);
        public override LocString DisplayName => Localizer.DoStr("Map Control");
        public override TableTextureMode TableTexture => TableTextureMode.Wood;

        static MapControlObject()
        {
            WorldObject.AddOccupancy<MapControlObject>(new List<BlockOccupancy>()
            {
                new BlockOccupancy(new Vector3i(0, 0, 0)),
                new BlockOccupancy(new Vector3i(0, 1, 0)),
            });
        }
    }

    [Serialized]
    [LocDisplayName("Map Control")]
    [LocDescription("Reveals or hides the minimap inside one district. Configure the district map and district names, then press the button.")]
    [Weight(2000)]
    public class MapControlItem : WorldObjectItem<MapControlObject>
    {
        protected override OccupancyContext GetOccupancyContext => new SideAttachedContext(0 | DirectionAxisFlags.Down, WorldObject.GetOccupancyInfo(this.WorldObjectType));
    }

    [RequiresSkill(typeof(CarpentrySkill), 1)]
    public class MapControlRecipe : RecipeFamily
    {
        public MapControlRecipe()
        {
            var recipe = new Recipe();
            recipe.Init(
                "MapControl",  //noloc
                Localizer.DoStr("Map Control"),
                new List<IngredientElement>
                {
                    new IngredientElement("Lumber", 20, typeof(CarpentrySkill)),
                    new IngredientElement(typeof(PaperItem), 10),
                },
                new List<CraftingElement>
                {
                    new CraftingElement<MapControlItem>()
                });
            this.Recipes = new List<Recipe> { recipe };
            this.ExperienceOnCraft = 5;
            this.LaborInCalories = CreateLaborInCaloriesValue(500);
            this.CraftMinutes = CreateCraftTimeValue(5);
            this.Initialize(Localizer.DoStr("Map Control"), typeof(MapControlRecipe));
            CraftingComponent.AddRecipe(typeof(CarpentryTableObject), this);
        }
    }
}
