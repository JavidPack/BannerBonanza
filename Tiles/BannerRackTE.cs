using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.ObjectData;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using System.Linq;
using System.Text;
using System.IO;

namespace BannerBonanza.Tiles
{
	public class BannerRackTE : ModTileEntity
	{
		internal static readonly IDictionary<int, int> itemToBanner = new Dictionary<int, int>();

		internal List<Item> bannerItems;
		private List<Item> unloadedBannerItems;
		internal bool updateNeeded = false;

		// Visual
		internal int counter = 0;
		internal int[] drawItemIndexs = { -1, -1, -1 };

		public BannerRackTE()
		{
			bannerItems = new List<Item>();
			unloadedBannerItems = new List<Item>();
		}

		public override void SaveData(TagCompound tag)
		{
			tag.Add("BannerItems", bannerItems.Union(unloadedBannerItems).Select(ItemIO.Save).ToList());
		}

        public override void LoadData(TagCompound tag)
        {
			bannerItems.AddRange(tag.GetList<TagCompound>("BannerItems").Select(ItemIO.Load));
            unloadedBannerItems.AddRange(bannerItems.Where(x => x.type == ItemID.Count));
            bannerItems.RemoveAll(x => x.type == ItemID.Count);
		}

        public override void NetSend(BinaryWriter writer)
		{
			writer.Write(bannerItems.Count);
			foreach (var item in bannerItems)
			{
				writer.Write(item.type);
			}
		}

		public override void NetReceive(BinaryReader reader)
		{
			bannerItems.Clear();
			stringUpToDate = false;
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				int type = reader.ReadInt32();
				Item item = new Item();
				item.SetDefaults(type, true);
				bannerItems.Add(item);
			}
			UpdateDrawItemIndexes();
		}

		public override void Update()
		{
			if (updateNeeded)
			{
				// Sending 86 aka, TileEntitySharing, triggers NetSend. Think of it like manually calling sync.
				NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
				updateNeeded = false;
			}
		}

		// Update only runs on SP and Server. This will be called in SP and Client. This takes care of the visuals.
		public void ClientUpdate()
		{
			counter += 4;
			if (counter >= 32 && bannerItems.Count > 0)
			{
				counter = 0;
				UpdateDrawItemIndexes();
			}
		}

		internal void UpdateDrawItemIndexes()
		{
			if (bannerItems.Count > 0)
			{
				if (drawItemIndexs[0] >= bannerItems.Count)
					drawItemIndexs[0] = Main.rand.Next(bannerItems.Count);
				if (drawItemIndexs[1] >= bannerItems.Count)
					drawItemIndexs[1] = Main.rand.Next(bannerItems.Count);
				if (drawItemIndexs[2] >= bannerItems.Count)
					drawItemIndexs[2] = Main.rand.Next(bannerItems.Count);

				int next = (drawItemIndexs[2] + 1) % bannerItems.Count;
				drawItemIndexs[0] = drawItemIndexs[1];
				drawItemIndexs[1] = drawItemIndexs[2];
				drawItemIndexs[2] = next;

				//drawItemIndexs[0] = Main.rand.Next(bannerItems.Count);
				//drawItemIndexs[1] = Main.rand.Next(bannerItems.Count);
				//drawItemIndexs[2] = Main.rand.Next(bannerItems.Count);
			}
			else
			{
				drawItemIndexs[0] = -1;
				drawItemIndexs[1] = -1;
				drawItemIndexs[2] = -1;
			}
		}

		internal bool stringUpToDate = false;
		string hoverString;
		public string GetHoverString()
		{
			if (stringUpToDate)
				return hoverString;
			stringUpToDate = true;

			StringBuilder sb = new StringBuilder();
			sb.Append($"Total: {bannerItems.Count}/{itemToBanner.Count}");
			sb.Append($"\nVanilla: {bannerItems.Count(x=>x.type < ItemID.Count)}/249");

			Dictionary<Mod, int> BannersPerMod = new Dictionary<Mod, int>();
			for (int i = NPCID.Count; i < NPCLoader.NPCCount; i++)
			{
				int bannernum = Item.NPCtoBanner(i);
				int itemnum = Item.BannerToItem(bannernum);
				if (bannernum > 0 && itemnum > ItemID.Count)
				{
					ModItem item = ItemLoader.GetItem(itemnum);
					int currentCount;
					BannersPerMod.TryGetValue(item.Mod, out currentCount);
					BannersPerMod[item.Mod] = currentCount + 1;
				}
			}

			foreach (var item in BannersPerMod)
			{
				int num = bannerItems.Count(x => x.ModItem != null && x.ModItem.Mod == item.Key);
				sb.Append($"\n{item.Key.DisplayName}: {num}/{item.Value}");
			}
			if (unloadedBannerItems.Count > 0)
				sb.Append($"\nUnloaded: {unloadedBannerItems.Count}");

			//TODO missing?
			// TODO event?
			sb.Append($"\nMissing: ");
			int count = 0;
			foreach (var item in itemToBanner)
			{
				if (!bannerItems.Any(x => x.type == item.Key))
				{
					sb.Append($"[i:{item.Key}]");
					count++;
					if (count > 4)
					{
						sb.Append($"...");
						break;
					}
				}
			}

			hoverString = sb.ToString();
			return hoverString;
		}

        public override bool IsTileValidForEntity(int i, int j)
		{
			Tile tile = Main.tile[i, j];
			return tile.IsActive && tile.type == TileType<BannerRackTile>() && tile.frameX % 54 == 0 && tile.frameY == 0;
		}

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i + 1, j + 1, 4);
				NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type, 0f, 0, 0, 0);
				return -1;
			}
			int num = Place(i, j);
			return num;
		}

		// SP Client and Server
		public override void OnKill()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				foreach (var item in bannerItems.Union(unloadedBannerItems))
				{
					// TODO: Hmmmm, Item.NewItem would destroy modData...
					// TODO: Prep for stack > 1 code?
					// nobroadcast true to prevent stuff.
					int index = Item.NewItem(Position.X * 16, Position.Y * 16, 54, 72, item.type, item.stack, true);
					Vector2 position = Main.item[index].position;
					Main.item[index] = item.Clone();
					Main.item[index].position = position;
					Main.item[index].whoAmI = index;
					//Main.item[index].position = this.position;
					//if (stack != Main.item[index].stack)
					//	Main.item[index].stack = stack;

					// Sync the item for mp
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f, 0f, 0f, 0, 0, 0);
					}
				}
			}
		}

		// BannerToItem BannerID -> ItemID
		// NPCtoBanner  NPCID -> BannerID
		// ModNPC.

		// I need: Item to BannerID
		internal void Nearby()
		{
			Player player = Main.LocalPlayer;
			foreach (var item in bannerItems)
			{
				int type = item.type;
				if (type != ItemID.Count && ItemID.Sets.BannerStrength[type].Enabled)
				{
					int bannerID = itemToBanner[type];

                    Main.SceneMetrics.NPCBannerBuff[bannerID] = true;
                    Main.SceneMetrics.hasBanner = true;
                }
			}
		}
	}
}
