using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BannerBonanza
{
	class StylistShop : GlobalNPC
	{
		public override void ModifyShop(NPCShop shop) {
			if(shop.NpcType == NPCID.Stylist) {
				var item = new Item(ItemID.StylistKilLaKillScissorsIWish) {
					shopCustomPrice = Item.buyPrice(0, 50),
				};
				item.SetNameOverride("(I don't want to die)");
				shop.Add(item);
			}
		}
	}
}
