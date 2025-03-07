﻿using System;
using RogueElements;
using RogueEssence.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using RogueEssence.Data;
using Newtonsoft.Json;
using RogueEssence.Dev;
using System.Runtime.Serialization;

namespace RogueEssence.Dungeon
{
    [Serializable]
    public class MapItem : IDrawableSprite, ISpawnable, IPreviewable
    {
        public bool IsMoney;
        public bool Cursed;


        [JsonConverter(typeof(ItemConverter))]
        public string Value;

        public string HiddenValue;
        public int Amount;
        public int Price;

        public string SpriteIndex
        {
            get
            {
                if (IsMoney)
                    return GraphicsManager.MoneySprite;
                else
                    return DataManager.Instance.GetItem(Value).Sprite;
            }
        }

        public Loc TileLoc;
        public Loc MapLoc { get { return TileLoc * GraphicsManager.TileSize; } }
        public int LocHeight { get { return 0; } }

        public MapItem()
        {
            Value = "";
            HiddenValue = "";
            TileLoc = new Loc();
        }

        public MapItem(string value)
        {
            Value = value;
            HiddenValue = "";
        }

        public MapItem(string value, int amount)
            : this(value)
        {
            Amount = amount;
        }

        public MapItem(string value, int amount, int price)
            : this(value, amount)
        {
            Price = price;
        }

        public MapItem(MapItem other)
        {
            IsMoney = other.IsMoney;
            Cursed = other.Cursed;
            Value = other.Value;
            HiddenValue = other.HiddenValue;
            Amount = other.Amount;
            Price = other.Price;
        }
        public ISpawnable Copy() { return new MapItem(this); }

        public MapItem(InvItem item) : this(item, new Loc()) { }

        public MapItem(InvItem item, Loc loc)
        {
            Value = item.ID;
            Cursed = item.Cursed;
            Amount = item.Amount;
            HiddenValue = item.HiddenValue;
            Price = item.Price;
            TileLoc = loc;
        }

        public InvItem MakeInvItem()
        {
            InvItem item = new InvItem(Value, Cursed);
            item.Amount = Amount;
            item.HiddenValue = HiddenValue;
            item.Price = Price;
            return item;
        }

        public static MapItem CreateMoney(int amt)
        {
            MapItem item = new MapItem();
            item.IsMoney = true;
            item.Amount = amt;
            return item;
        }

        public static MapItem CreateBox(string value, string hiddenValue, int price = 0, bool cursed = false)
        {
            MapItem item = new MapItem();
            item.Value = value;
            item.HiddenValue = hiddenValue;
            item.Cursed = cursed;
            item.Price = price;
            return item;
        }

        public int GetSellValue()
        {
            if (IsMoney)
                return 0;

            ItemData entry = DataManager.Instance.GetItem(Value);
            if (entry.MaxStack > 1)
                return entry.Price * Amount;
            else
                return entry.Price;
        }

        public string GetPriceString()
        {
            if (Price > 0)
            {
                string baseStr = Price.ToString();
                StringBuilder resultStr = new StringBuilder();
                for (int ii = 0; ii < baseStr.Length; ii++)
                {
                    int en = (int)baseStr[ii] - 0x30;
                    int un = en + 0xE100;
                    resultStr.Append((char)un);
                }
                return resultStr.ToString();
            }
            return null;
        }
        public static string GetPriceString(int price)
        {
            if (price > 0)
            {
                string baseStr = price.ToString();
                StringBuilder resultStr = new StringBuilder();
                for (int ii = 0; ii < baseStr.Length; ii++)
                {
                    int en = (int)baseStr[ii] - 0x30;
                    int un = en + 0xE100;
                    resultStr.Append((char)un);
                }
                return resultStr.ToString();
            }
            return "";
        }

        public string GetDungeonName()
        {
            if (IsMoney)
                return Text.FormatKey("MONEY_AMOUNT", Amount);
            else
            {
                ItemData entry = DataManager.Instance.GetItem(Value);

                string prefix = "";
                if (entry.Icon > -1)
                    prefix += ((char)(entry.Icon + 0xE0A0)).ToString();
                if (Cursed)
                    prefix += "\uE10B";

                string nameStr = entry.Name.ToLocal();
                if (entry.MaxStack > 1)
                    nameStr += " (" + Amount + ")";

                return String.Format("{0}[color=#FFCEFF]{1}[color]", prefix, nameStr);
            }
        }

        public override string ToString()
        {
            if (IsMoney)
                return String.Format("${0}", Amount);

            string nameStr = "";
            if (Price > 0)
                nameStr += String.Format("${0} ", Price);
            if (Cursed)
                nameStr += "[X]";

            nameStr += Value;
            if (Amount > 0)
                nameStr += String.Format("({0})", Amount);

            if (!String.IsNullOrEmpty(HiddenValue))
                nameStr += String.Format("[{0}]", HiddenValue);

            return nameStr;
        }

        public void DrawDebug(SpriteBatch spriteBatch, Loc offset) { }
        public void Draw(SpriteBatch spriteBatch, Loc offset)
        {
            Draw(spriteBatch, offset, Color.White);
        }

        public void DrawPreview(SpriteBatch spriteBatch, Loc offset, float alpha)
        {
            Draw(spriteBatch, offset, Color.White * alpha);
        }

        public void Draw(SpriteBatch spriteBatch, Loc offset, Color color)
        {
            Loc drawLoc = GetDrawLoc(offset);

            DirSheet sheet = GraphicsManager.GetItem(SpriteIndex);
            sheet.DrawDir(spriteBatch, new Vector2(drawLoc.X, drawLoc.Y), 0, Dir8.Down, color);
        }

        public Loc GetDrawLoc(Loc offset)
        {
            return new Loc(MapLoc.X + GraphicsManager.TileSize / 2 - GraphicsManager.GetItem(SpriteIndex).TileWidth / 2,
                MapLoc.Y + GraphicsManager.TileSize / 2 - GraphicsManager.GetItem(SpriteIndex).TileHeight / 2) - offset;
        }

        public Loc GetDrawSize()
        {
            return new Loc(GraphicsManager.GetItem(SpriteIndex).TileWidth,
                GraphicsManager.GetItem(SpriteIndex).TileHeight);
        }

        //TODO: Created v0.5.20, delete on v1.1
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (IsMoney)
            {
                int amt;
                if (int.TryParse(HiddenValue, out amt))
                {
                    Amount = amt;
                    HiddenValue = "";
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(Value))
                {
                    ItemData item = DataManager.Instance.GetItem(Value);

                    if (item != null)
                    {
                        int amt;
                        if (int.TryParse(HiddenValue, out amt))
                        {
                            if (item.MaxStack > 0)
                            {
                                Amount = amt;
                                HiddenValue = "";
                            }
                            else if (amt > 0)
                            {
                                string asset_name = DataManager.Instance.MapAssetName(DataManager.DataType.Item, amt);
                                HiddenValue = asset_name;
                            }
                            else
                            {
                                HiddenValue = "";
                            }
                        }
                    }
                    else
                    {
                        Value = "empty";
                    }
                }
            }
        }
    }
}
