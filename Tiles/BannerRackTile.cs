﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using System.Linq;
using System.Collections.Generic;
using System;

namespace BannerBonanza.Tiles
{
	public class BannerRackTile : ModTile
	{
		public override void SetDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.Width = 3;
			TileObjectData.newTile.Height = 4;
			TileObjectData.newTile.Origin = new Point16(1, 0);
			TileObjectData.newTile.CoordinateHeights = new int[]
			{
				16,
				16,
				16,
				16
			};
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.SolidBottom, TileObjectData.newTile.Width, 0);

			TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(mod.GetTileEntity<BannerRackTE>().Hook_AfterPlacement, -1, 0, true);
			TileObjectData.addTile(Type);

			ModTranslation name = CreateMapEntryName();
			name.SetDefault("Banner Rack");
			AddMapEntry(new Color(13, 88, 130), name, mapEntryFunction);

			animationFrameHeight = 72;
		}

		private string mapEntryFunction(string arg1, int i, int j)
		{
			Tile tile = Main.tile[i, j];
			int left = i - (tile.frameX % 54 / 18);
			int top = j - (tile.frameY / 18);
			int index = mod.GetTileEntity<BannerRackTE>().Find(left, top);
			if (index == -1)
			{
				return arg1 + "\n" + "Error";
			}
			BannerRackTE bannerRackTE = (BannerRackTE)TileEntity.ByID[index];
			return arg1 + "\n" + bannerRackTE.GetHoverString();
		}

		public override void AnimateTile(ref int frame, ref int frameCounter)
		{
			if (++frameCounter > 4)
			{
				frameCounter = 0;
				if (++frame > 5)
				{
					frame = 0;
				}
			}
		}

		public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref Color drawColor, ref int nextSpecialDrawIndex)
		{
			Tile t = Main.tile[i, j];
			if (t.frameX == 0 && t.frameY == 0) // t.frameX % 54 == 0
			{
				Main.specX[nextSpecialDrawIndex] = i;
				Main.specY[nextSpecialDrawIndex] = j;
				nextSpecialDrawIndex++;
			}
		}

		public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Vector2 zero = new Vector2((float)Main.offScreenRange, (float)Main.offScreenRange);
			if (Main.drawToScreen)
			{
				zero = Vector2.Zero;
			}

			Tile t = Main.tile[i, j];
			//int style = t.frameX / 54;

			int index = mod.GetTileEntity<BannerRackTE>().Find(i, j);
			if (index == -1)
			{
				return;
			}
			BannerRackTE bannerRackTE = (BannerRackTE)TileEntity.ByID[index];
			if (bannerRackTE.bannerItems.Count > 0)
			{
				bannerRackTE.ClientUpdate();
				int leftItemTrim = bannerRackTE.counter / 2;
				int rightItemTrim = 16 - leftItemTrim;
				for (int drawItemNumber = 0; drawItemNumber < 3; drawItemNumber++)
				{
					Color color = Lighting.GetColor(i + drawItemNumber, j + 1);
					if (bannerRackTE.drawItemIndexs[drawItemNumber] < 0)
						continue;

					Item item = bannerRackTE.bannerItems[bannerRackTE.drawItemIndexs[drawItemNumber]];

					//if(drawItemNumber > 0 && superBannerTE.drawItemIndexs[drawItemNumber] == superBannerTE.drawItemIndexs[drawItemNumber - 1])
					//{
					//	continue;
					//}

					int itemXOffset = 8 + (-bannerRackTE.counter / 2 + drawItemNumber * 16);

					if (item != null && item.createTile > 0) // unloaded....spawn in world when you can.
					{
						var tod = TileObjectData.GetTileData(item.createTile, item.placeStyle);
						int x = (item.placeStyle % tod.StyleWrapLimit) * tod.CoordinateFullWidth;
						int y = (item.placeStyle / tod.StyleWrapLimit) * tod.CoordinateFullHeight;


						Main.instance.LoadTiles(item.createTile);
						Texture2D tileTexture = Main.tileTexture[item.createTile];
						int[] heights = tod.CoordinateHeights;
						int heightOffSet = 0;
						int heightOffSetTexture = 0;
						int leftItemNudge = 0;
						for (int sub = 0; sub < heights.Length; sub++)
						{
							//Rectangle sourceRectangle = new Rectangle(x, y + heightOffSet, 1, 1);
							Rectangle sourceRectangle = new Rectangle(x, y + heightOffSetTexture, tod.CoordinateWidth, tod.CoordinateHeights[sub]);
							if (drawItemNumber == 0)
							{
								sourceRectangle.X += leftItemTrim;
								sourceRectangle.Width -= leftItemTrim;
								leftItemNudge = leftItemTrim;
							}
							if (drawItemNumber == 2)
							{
								sourceRectangle.Width -= rightItemTrim;
							}
							Main.spriteBatch.Draw(tileTexture,
								new Vector2(
									leftItemNudge + itemXOffset + (i * 16) - (int)Main.screenPosition.X + 0,
									j * 16 - (int)Main.screenPosition.Y + 8 + heightOffSet) + zero,
								sourceRectangle, color, 0f, Vector2.Zero, 1, SpriteEffects.None, 0f);
							heightOffSet += heights[sub];
							heightOffSetTexture += heights[sub] + tod.CoordinatePadding;
						}
					}
				}
			}
		}

		public override void NearbyEffects(int i, int j, bool closer)
		{
			Tile tile = Main.tile[i, j];
			if (tile.frameX == 0 && tile.frameY == 0)
			{
				if (closer)
				{
					Player player = Main.LocalPlayer;
					int left = i - (tile.frameX % 54 / 18);
					int top = j - (tile.frameY / 18);

					int index = mod.GetTileEntity<BannerRackTE>().Find(left, top);
					if (index == -1)
					{
						return;
					}
					BannerRackTE bannerRackTE = (BannerRackTE)TileEntity.ByID[index];
					bannerRackTE.Nearby();
				}
			}
		}

		// SP, Server, Client
		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
			Tile tile = Main.tile[i, j];
			int left = i - (tile.frameX % 54 / 18);
			int top = j - (tile.frameY / 18);

			int index = mod.GetTileEntity<BannerRackTE>().Find(left, top);
			if (index == -1)
			{
				return;
			}
			BannerRackTE bannerRackTE = (BannerRackTE)TileEntity.ByID[index];
			if (bannerRackTE.bannerItems.Count > 0)
			{
				fail = true;
				if (WorldGen.destroyObject)
					fail = false;
				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					// TODO: Send ModPacket identifying selected Banner.
					return;
				}
				Item item = bannerRackTE.bannerItems[bannerRackTE.bannerItems.Count - 1];
				Item.NewItem(i * 16, j * 16, 54, 72, item.type);
				bannerRackTE.bannerItems.RemoveAt(bannerRackTE.bannerItems.Count - 1);
				bannerRackTE.updateNeeded = true;
				bannerRackTE.UpdateDrawItemIndexes();
				bannerRackTE.stringUpToDate = false;
			}

			// Old code that let you chose which banner to pick.
			//int itemIndex = -1;
			//if(superBannerTE.drawItemIndexs[1] != -1)
			//{
			//	itemIndex = superBannerTE.drawItemIndexs[1];
			//}
			//if (itemIndex >= 0)
			//{
			//	Item item = superBannerTE.bannerItems[itemIndex];
			//	Item.NewItem(i * 16, j * 16, 32, 48, item.type);
			//	superBannerTE.bannerItems.Remove(item);
			//	superBannerTE.drawItemIndexs[1] = -1;
			//	for (int k = 0; k < 3; k++)
			//	{
			//		if(superBannerTE.drawItemIndexs[k] >= itemIndex)
			//		{
			//			superBannerTE.drawItemIndexs[k] = superBannerTE.drawItemIndexs[k] - 1;
			//		}
			//	}
			//}
			//}
		}

		// SP Client and Server
		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			// Reminder: Item.NewItem does nothing on Client
			//Tile t = Main.tile[i, j];
			//if (t.active())
			{
				Item.NewItem(i * 16, j * 16, 54, 72, mod.ItemType<Items.BannerRackItem>());
			}
			mod.GetTileEntity<BannerRackTE>().Kill(i, j); // This should call OnKill
		}

		public override void MouseOver(int i, int j)
		{
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.showItemIcon = true;
			//player.showItemIcon2 = mod.ItemType<Items.SuperBannerItem>();
			player.showItemIcon2 = -1;

			Tile tile = Main.tile[i, j];
			int left = i - (tile.frameX % 54 / 18);
			int top = j - (tile.frameY / 18);

			int index = mod.GetTileEntity<BannerRackTE>().Find(left, top);
			if (index == -1)
			{
				return;
			}
			BannerRackTE bannerRackTE = (BannerRackTE)TileEntity.ByID[index];
			player.showItemIconText = bannerRackTE.GetHoverString();
			// GUI Window:
			// % Total, vanilla, modded 1/249
			// % per event too.
			// Event is based on which event is happening?
		}

		public override void RightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Tile tile = Main.tile[i, j];
			int left = i - (tile.frameX % 54 / 18);
			int top = j - (tile.frameY / 18);

			int index = mod.GetTileEntity<BannerRackTE>().Find(left, top);
			if (index == -1)
			{
				return;
			}
			BannerRackTE bannerRackTE = (BannerRackTE)TileEntity.ByID[index];

			// if Client, not SP, ask server to move items for you.
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				var packet = mod.GetPacket();
				packet.Write((byte)BannerBonanzaMessageType.RequestSuperBannerStealBanners);
				packet.Write(index);
				List<int> indexes = new List<int>();
				for (int itemIndex = 0; itemIndex < 50; itemIndex++)
				{
					Item item = player.inventory[itemIndex];
					if (!item.IsAir && !player.inventory[itemIndex].favorited && BannerRackTE.itemToBanner.ContainsKey(item.type))
					{
						// Inform Server of current item (just to be safe?)
						// TODO: I think NetMessage 5 doesn't sync mod data.
						NetMessage.SendData(5, -1, -1, null, player.whoAmI, (float)itemIndex, (float)player.inventory[itemIndex].prefix, 0f, 0, 0, 0);
						// Mimicing QuickStackAllChests: NetMessage.SendData(85, -1, -1, null, itemIndex, 0f, 0f, 0f, 0, 0, 0);
						indexes.Add(itemIndex);
						// Prevents some moving items in ui while waiting for server response.
						player.inventoryChestStack[itemIndex] = true;
					}
				}
				packet.Write(indexes.Count);
				foreach (var itemIndex in indexes)
				{
					packet.Write(itemIndex);
				}
				packet.Send();
				return;
			}
			else // Single Player
			{


				// foreach item in inventory, if 
				bool added = false;
				for (int invIndex = 0; invIndex < 54; invIndex++)
				{
					Item item = player.inventory[invIndex];
					if (!item.IsAir && BannerRackTE.itemToBanner.ContainsKey(item.type))
					{
						if (!bannerRackTE.bannerItems.Any(x => x.type == item.type))
						{
							added = true;
							Main.NewText($"Banner for {item.Name} added to Banner Rack");
							Item clone = item.Clone();
							clone.stack = 1;
							item.stack--;
							if (item.IsAir)
							{
								item.SetDefaults(0);
							}
							//bool updateNeeded = superBannerTE.bannerItems.Count < 1;
							bannerRackTE.bannerItems.Add(clone);
							bannerRackTE.stringUpToDate = false;
							//if(updateNeeded)
							//	superBannerTE.UpdateDrawItemIndexes();
						}
					}
				}
				if (!added)
				{
					Main.NewText($"No new Banners to add to Banner Rack");
					// find closest npc that I don't have banner for.
					//player.NPCBannerBuff
					int nextNPCToKill = -1;
					int nextNPCToKillLeft = 9999;
					for (int npctype = -10; npctype < NPCLoader.NPCCount; npctype++)
					{
						int vanillaBannerID = Terraria.Item.NPCtoBanner(npctype);
						if (vanillaBannerID > 0 && !NPCID.Sets.ExcludedFromDeathTally[NPCID.FromNetId(npctype)])
						{
							int vanillaBannerItemID = Item.BannerToItem(vanillaBannerID);
							if (ItemID.Sets.BannerStrength[vanillaBannerItemID].Enabled)
							{
								int killsToBanner = ItemID.Sets.KillsToBanner[vanillaBannerItemID];
								int killsLeft = killsToBanner - (NPC.killCount[vanillaBannerID] % killsToBanner);

								if (killsLeft < nextNPCToKillLeft && !bannerRackTE.bannerItems.Any(x => x.type == vanillaBannerItemID))
								{
									nextNPCToKillLeft = killsLeft;
									nextNPCToKill = npctype;
								}
							}
						}
					}
					if (nextNPCToKill != -1)
					{
						Main.NewText($"Try killing {nextNPCToKillLeft} more {Lang.GetNPCNameValue(nextNPCToKill)}");
					}
				}
			}
		}
	}
}