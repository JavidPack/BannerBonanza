using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace BannerBonanza.Items
{
	public class BannerRackItem : ModItem
	{
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.AnglerFishBanner);
			Item.createTile = TileType<Tiles.BannerRackTile>();
			Item.placeStyle = 0;
			Item.width = 48;
			Item.height = 68;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.StylistKilLaKillScissorsIWish)

				.AddIngredient(ItemID.Rope)
				.AddIngredient(ItemID.Glass, 10)
				.AddIngredient(ItemID.Topaz, 2)
				.AddRecipeGroup("Wood", 10)

				.AddTile(TileID.HeavyWorkBench)
				.Register();
		}
	}
}
