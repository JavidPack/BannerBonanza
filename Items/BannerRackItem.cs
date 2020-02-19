using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace BannerBonanza.Items
{
	public class BannerRackItem : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Banner Rack");
		}

		public override void SetDefaults()
		{
			item.CloneDefaults(ItemID.AnglerFishBanner);
			item.createTile = TileType<Tiles.BannerRackTile>();
			item.placeStyle = 0;
			item.width = 48;
			item.height = 68;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.StylistKilLaKillScissorsIWish);
			recipe.AddIngredient(ItemID.Rope, 10);
			recipe.AddIngredient(ItemID.Glass, 10);
			recipe.AddIngredient(ItemID.Topaz, 2);
			recipe.AddRecipeGroup("Wood", 10);
			recipe.AddTile(TileID.HeavyWorkBench);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
