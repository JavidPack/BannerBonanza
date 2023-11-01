using BannerBonanza.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BannerBonanza
{
	class BannerBonanza : Mod
	{
		public static BannerBonanza instance;
		public override void Load() {
			instance = this;

			if (ModLoader.TryGetMod("TerrariaOverhaul", out _)) {
				Logger.Warn("Terraria Overhaul detected, banner rack will not work because of Terraria Overhaul code changes.");
			}
		}

		public override void PostSetupContent() {
			// This code was previously in AddRecipeGroups because "I'm using this as a PostPostSetupContent so all mods are loaded before I access bannerToItem". I don't think this is right, it should be fine here.
			Tiles.BannerRackTE.itemToBanner.Clear();
			FieldInfo bannerToItemField = typeof(NPCLoader).GetField("bannerToItem", BindingFlags.NonPublic | BindingFlags.Static);
			Dictionary<int, int> bannerToItem = (Dictionary<int, int>)bannerToItemField.GetValue(null);
			foreach (var item in bannerToItem) {
				if (Tiles.BannerRackTE.itemToBanner.ContainsKey(item.Value)) {
					Logger.Warn($"Warning, multiple BannerIDs pointing to same ItemID: Banners:{Lang.GetNPCNameValue(item.Key)},{Lang.GetNPCNameValue(BannerRackTE.itemToBanner[item.Value])} Item:{Lang.GetItemNameValue(item.Value)}");
				}
				else {
					Tiles.BannerRackTE.itemToBanner.Add(item.Value, item.Key);
				}
			}

			for (int i = -10; i < NPCID.Count; i++) {
				int vanillaBannerID = Terraria.Item.NPCtoBanner(i);
				if (vanillaBannerID > 0 && !NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(i)]) {
					int vanillaBannerItemID = Item.BannerToItem(vanillaBannerID);
					if (ItemID.Sets.BannerStrength[vanillaBannerItemID].Enabled) {
						if (!Tiles.BannerRackTE.itemToBanner.ContainsKey(vanillaBannerItemID)) {
							Tiles.BannerRackTE.itemToBanner.Add(vanillaBannerItemID, vanillaBannerID);
						}
					}
				}
			}

			if (!ModLoader.TryGetMod("RecipeBrowser", out var mod) || Main.dedServ)
				return;

			mod.Call(new object[5]
			{
				"AddItemCategory",
				"Banners",
				"Tiles",
				Assets.Request<Texture2D>("RecipeBrowserBannerCategoryIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, // 24x24 icon
				(Predicate<Item>)((Item item) =>
				{
					return Tiles.BannerRackTE.itemToBanner.ContainsKey(item.type);
				})
			});
		}

		public override void Unload() {
			instance = null;
			Tiles.BannerRackTE.itemToBanner.Clear();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			BannerBonanzaMessageType msgType = (BannerBonanzaMessageType)reader.ReadByte();
			int tileEntityIndex;
			BannerRackTE bannerRackTE;
			switch (msgType) {
				// TODO: Hope NetSend only called once....
				case BannerBonanzaMessageType.RequestSuperBannerStealBanners:
					tileEntityIndex = reader.ReadInt32();
					int indexesCount = reader.ReadInt32();
					List<int> indexes = new List<int>();
					for (int i = 0; i < indexesCount; i++) {
						indexes.Add(reader.ReadInt32());
					}
					bannerRackTE = (BannerRackTE)TileEntity.ByID[tileEntityIndex];
					Player player = Main.player[whoAmI];


					PutItemsInSuperBannerTE(bannerRackTE, indexes, player);
					foreach (var itemIndex in indexes) {
						NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, whoAmI, (float)itemIndex, (float)player.inventory[itemIndex].prefix, 0f, 0, 0, 0);
					}

					//Inform all
					//var packet = GetPacket();
					//packet.Write((byte)BannerBonanzaMessageType.NotifySuperBannerStringOutOfDate);
					//packet.Send();
					//	player.inventory[itemIndex] = PutItemInSuperBannerTE(superBannerTE, player.inventory[itemIndex], player.Center);
					//	NetMessage.SendData(5, -1, -1, null, whoAmI, (float)itemIndex, (float)player.inventory[itemIndex].prefix, 0f, 0, 0, 0);


					break;
				//case BannerBonanzaMessageType.NotifySuperBannerStringOutOfDate:
				//	tileEntityIndex = reader.ReadInt32();
				//	superBannerTE = (SuperBannerTE)TileEntity.ByID[tileEntityIndex];
				//	superBannerTE.stringUpToDate = false; 
				//	break;
				default:
					Logger.Warn("Unknown Message type: " + msgType);
					break;
			}
		}

		private void PutItemsInSuperBannerTE(BannerRackTE superBannerTE, List<int> indexes, Player player) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				// uh oh
				return;
			}
			bool added = false;
			foreach (int invIndex in indexes) {
				Item item = player.inventory[invIndex];
				if (!item.IsAir && BannerRackTE.itemToBanner.ContainsKey(item.type)) {
					if (!superBannerTE.bannerItems.Any(x => x.type == item.type)) {
						added = true;
						string message = $"Banner for {item.Name} added to Banner Rack";
						Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White);
						//Main.NewText($"Banner for {item.Name} added to Banner Rack");
						Item clone = item.Clone();
						clone.stack = 1;
						item.stack--;
						if (item.IsAir) {
							item.SetDefaults(0);
						}
						//bool updateNeeded = superBannerTE.bannerItems.Count < 1;
						superBannerTE.bannerItems.Add(clone);
						//superBannerTE.stringUpToDate = false;
						//if(updateNeeded)
						//	superBannerTE.UpdateDrawItemIndexes();
					}
				}
			}
			if (!added) {
				string message = "No new Banners to add to Banner Rack";

				// find closest npc that I don't have banner for.
				//player.NPCBannerBuff
				int nextNPCToKill = -1;
				int nextNPCToKillLeft = 9999;
				for (int npctype = -10; npctype < NPCLoader.NPCCount; npctype++) {
					int vanillaBannerID = Terraria.Item.NPCtoBanner(npctype);
					if (vanillaBannerID > 0 && !NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[NPCID.FromNetId(npctype)]) {
						int vanillaBannerItemID = Item.BannerToItem(vanillaBannerID);
						if (ItemID.Sets.BannerStrength[vanillaBannerItemID].Enabled) {
							int killsToBanner = ItemID.Sets.KillsToBanner[vanillaBannerItemID];
							int killsLeft = killsToBanner - (NPC.killCount[vanillaBannerID] % killsToBanner);

							if (killsLeft < nextNPCToKillLeft && !superBannerTE.bannerItems.Any(x => x.type == vanillaBannerItemID)) {
								nextNPCToKillLeft = killsLeft;
								nextNPCToKill = npctype;
							}
						}
					}
				}
				
				if (nextNPCToKill != -1) {
					message += $"\nTry killing {nextNPCToKillLeft} more {Lang.GetNPCNameValue(nextNPCToKill)}";
				}

				Terraria.Chat.ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), Color.White, player.whoAmI);
			}
			else {
				superBannerTE.updateNeeded = true;
			}

		}

		//public static Item PutItemInSuperBannerTE(SuperBannerTE superBannerTE, Item item, Vector2 position)
		//{
		//	if (Main.netMode == 1)
		//	{
		//		return item;
		//	}
		//	bool added = false;
		//	if (!item.IsAir && SuperBannerTE.itemToBanner.ContainsKey(item.type))
		//	{
		//		if (!superBannerTE.bannerItems.Any(x => x.type == item.type))
		//		{
		//			added = true;
		//			Main.NewText($"Banner for {item.Name} added to Banner Rack");
		//			Item clone = item.Clone();
		//			clone.stack = 1;
		//			item.stack--;
		//			if (item.IsAir)
		//			{
		//				item.SetDefaults(0);
		//			}
		//			superBannerTE.bannerItems.Add(clone);
		//			superBannerTE.stringUpToDate = false;
		//		}
		//	}
		//	if (!added)
		//	{
		//		Main.NewText($"No new Banners to add to Banner Rack");
		//		// find closest npc that I don't have banner for.
		//		//player.NPCBannerBuff
		//		int nextNPCToKill = -1;
		//		int nextNPCToKillLeft = 9999;
		//		for (int npctype = -10; npctype < NPCID.Count; npctype++)
		//		{
		//			int vanillaBannerID = Terraria.Item.NPCtoBanner(npctype);
		//			if (vanillaBannerID > 0 && !NPCID.Sets.ExcludedFromDeathTally[NPCID.FromNetId(npctype)])
		//			{
		//				int vanillaBannerItemID = Item.BannerToItem(vanillaBannerID);
		//				if (ItemID.Sets.BannerStrength[vanillaBannerItemID].Enabled)
		//				{
		//					int killsToBanner = ItemID.Sets.KillsToBanner[vanillaBannerItemID];
		//					int killsLeft = killsToBanner - (NPC.killCount[vanillaBannerID] % killsToBanner);

		//					if (killsLeft < nextNPCToKillLeft && !superBannerTE.bannerItems.Any(x => x.type == vanillaBannerItemID))
		//					{
		//						nextNPCToKillLeft = killsLeft;
		//						nextNPCToKill = npctype;
		//					}
		//				}
		//			}
		//		}
		//		if (nextNPCToKill != -1)
		//		{
		//			Main.NewText($"Try killing {nextNPCToKillLeft} more {Lang.GetNPCNameValue(nextNPCToKill)}");
		//		}
		//	}

		//	return item;
		//}
	}

	// TODO: Might need a message at end with a Recipe.FindRecipes(); call.
	enum BannerBonanzaMessageType : byte
	{
		RequestSuperBannerStealBanners,
		//	NotifySuperBannerStringOutOfDate,
	}
}
