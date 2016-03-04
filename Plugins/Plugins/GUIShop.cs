//Reference: NLua
// ToDo:
// - [DONE]Sell Preis begrenzen
// - [DONE]kleiner Refresh bei kauf/verkauf
// - [DONE]Preise für Massenkauf anzeigen
// - Data anstatt Config für Speicherung des Stocks verwenden
// - Füllen des Stocks nach Zeit
// - [DONE]dStock in Config eintragen
// - [DONE]nonDynamic Prices
// - [DONE]Refresh wenn anderer Spieler kauft/verkauft
// - Button Bug beheben
// - [DONE]Verkaufbegrenzung senken

using System.Collections.Generic;
using System;
using System.Linq;

using NLua;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GUIShop", "Reneb / Stiffi136", "1.1.2", ResourceId = 1319)]
    class GUIShop : RustPlugin
    {
        private const string ShopOverlayName = "ShopOverlay";
        private const string ShopDescOverlay = "ShopDescOverlay";
        int playersMask;

        //////////////////////////////////////////////////////////////////////////////////////
        // References ////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [PluginReference("Economics")]
        Plugin Economics;

        [PluginReference]
        Plugin Kits;

        void OnServerInitialized()
        {
            InitializeTable();
            if (!Economics) PrintWarning("Economics plugin not found. " + Name + " will not function!");
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configs Manager ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private static Dictionary<string, object> ShopCategories = DefaultShopCategories();
        private static Dictionary<string, object> Shops = DefaultShops();

        static string MessageShowNoEconomics = "Couldn't get informations out of Economics. Is it installed?";
        static string MessageBought = "You've successfully bought {0}x {1}";
        static string MessageSold = "You've successfully sold {0}x {1}";
        static string MessageErrorNoShop = "This shop doesn't seem to exist.";
        static string MessageErrorNoActionShop = "You are not allowed to {0} in this shop";
        static string MessageErrorNoNPC = "The NPC owning this shop was not found around you";
        static string MessageErrorNoActionItem = "You are not allowed to {0} this item here";
        static string MessageErrorItemItem = "WARNING: The admin didn't set this item properly! (item)";
        static string MessageErrorItemNoValid = "WARNING: It seems like it's not a valid item";
        static string MessageErrorRedeemKit = "WARNING: There was an error while giving you this kit";
        static string MessageErrorBuyPrice = "WARNING: No buy price was given by the admin, you can't buy this item";
        static string MessageErrorSellPrice = "WARNING: No sell price was given by the admin, you can't sell this item";
        static string MessageErrorNotEnoughMoney = "You need {0} coins to buy {1} of {2}";
        static string MessageErrorNotEnoughSell = "You don't have enough of this item.";
        static string MessageErrorItemNoExist = "WARNING: The item you are trying to buy doesn't seem to exist";
        static string MessageErrorNPCRange = "You may not use the chat shop. You might need to find the NPC Shops.";

        void Init()
        {
            CheckCfg("Shop - Shop Categories", ref ShopCategories);
            CheckCfg("Shop - Shop List", ref Shops);
            CheckCfg("Message - Error - No Econonomics", ref MessageShowNoEconomics);
            CheckCfg("Message - Bought", ref MessageBought);
            CheckCfg("Message - Sold", ref MessageSold);
            CheckCfg("Message - Error - No Shop", ref MessageErrorNoShop);
            CheckCfg("Message - Error - No Action In Shop", ref MessageErrorNoActionShop);
            CheckCfg("Message - Error - No NPC", ref MessageErrorNoNPC);
            CheckCfg("Message - Error - No Action Item", ref MessageErrorNoActionItem);
            CheckCfg("Message - Error - Item Not Set Properly", ref MessageErrorItemItem);
            CheckCfg("Message - Error - Item Not Valid", ref MessageErrorItemNoValid);
            CheckCfg("Message - Error - Redeem Kit", ref MessageErrorRedeemKit);
            CheckCfg("Message - Error - No Buy Price", ref MessageErrorBuyPrice);
            CheckCfg("Message - Error - No Sell Price", ref MessageErrorSellPrice);
            CheckCfg("Message - Error - Not Enough Money", ref MessageErrorNotEnoughMoney);
            CheckCfg("Message - Error - Not Enough Items", ref MessageErrorNotEnoughSell);
            CheckCfg("Message - Error - Item Doesnt Exist", ref MessageErrorItemNoExist);
            CheckCfg("Message - Error - No Chat Shop", ref MessageErrorNPCRange);
            SaveConfig();
        }

        void OnServerSave()
        {
            SaveConfig();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Default Shops for Tutorial purpoise ///////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        static Dictionary<string, object> DefaultShops()
        {
            var shops = new Dictionary<string, object>
            {
                {
                    "chat", new Dictionary<string, object>
                    {
                        {"buy", new List<object> {"Build Kit"}},
                        {"description", "You currently have {0} coins to spend in this builders shop"},
                        {"name", "Build"}
                    }
                },
                {
                    "5498734", new Dictionary<string, object>
                    {
                        {"description", "You currently have {0} coins to spend in this weapons shop"},
                        {"name", "Weaponsmith Shop"},
                        {"buy", new List<object> {"Assault Rifle", "Bolt Action Rifle"}},
                        {"sell", new List<object> {"Assault Rifle", "Bolt Action Rifle"}}
                    }
                },
                {
                    "1234567", new Dictionary<string, object>
                    {
                        {"description", "You currently have {0} coins to spend in this farmers market"},
                        {"name", "Fruit Market"},
                        {"buy", new List<object> {"Apple", "BlueBerries", "Assault Rifle", "Bolt Action Rifle"}},
                        {"sell", new List<object> {"Apple", "BlueBerries", "Assault Rifle", "Bolt Action Rifle"}}
                    }
                }
            };
            return shops;
        }
        static Dictionary<string, object> DefaultShopCategories()
        {
            var dsc = new Dictionary<string, object>
            {
                {
                    "Assault Rifle", new Dictionary<string, object>
                    {
                        {"item", "assault rifle"},
                        {"buy", "10"},
                        {"dstock", "10"},
                        {"stock", "1"},
                        {"sell", "8"},
                        {"img", "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20150405105940"}
                    }
                },
                {
                    "Bolt Action Rifle", new Dictionary<string, object>
                    {
                        {"item", "bolt action rifle"},
                        {"buy", "10"}, {"sell", "8"}, {"dstock", "10"}, {"stock", "1"},
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20150405111457"}
                    }
                },
                {
                    "Build Kit", new Dictionary<string, object>
                    {
                        {"item", "kitbuild"},
                        {"buy", "10"},
                        {"sell", "8"},
                        {"dstock", "10"},
                        {"stock", "1"},
                        {"img", "http://oxidemod.org/data/resource_icons/0/715.jpg?1425682952"}
                    }
                },
                {
                    "Apple", new Dictionary<string, object>
                    {
                        {"item", "apple"},
                        {"buy", "1"},
                        {"sell", "1"},
                        {"dstock", "10"},
                        {"stock", "1"},
                        {"img", "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Apple_icon.png/revision/latest/scale-to-width-down/100?cb=20150405103640"}
                    }
                },
                {
                    "BlueBerries", new Dictionary<string, object>
                    {
                        {"item", "blueberries"},
                        {"buy", "1"},
                        {"dstock", "10"},
                        {"sell", "1"},
                        {"stock", "1"},
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/f/f8/Blueberries_icon.png/revision/latest/scale-to-width-down/100?cb=20150405111338"}
                    }
                }
            };
            return dsc;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Player Management /////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        class ShopPlayer{
            public BasePlayer player;
            public string shopid;
            public int page;
        }

        List<ShopPlayer> players = new List<ShopPlayer>();

        void RefreshShop(string shopid)
        {
            foreach (ShopPlayer pl in players)
            {
                if (pl.shopid == shopid)
                {
                    ShowShop(pl.player, pl.shopid, pl.page, false);
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Item Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        readonly Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();
        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            var ItemsDefinition = ItemManager.itemList;
            foreach (var itemdef in ItemsDefinition)
                displaynameToShortname.Add(itemdef.displayName.english.ToLower(), itemdef.shortname);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            playersMask = LayerMask.GetMask("Player (Server)");
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (!Shops.ContainsKey(npc.UserIDString)) return;
			
            ShowShop(player, npc.UserIDString, 0);
			
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CloseShop(player);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Price Calculations ////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static float CalcBuyPrice(object defaultStock, object defaultPrice, object stock, int amount, bool nonDynamic = false)
        {
            float ds = Convert.ToSingle(defaultStock);
            float dp = Convert.ToSingle(defaultPrice);
            float s = Convert.ToSingle(stock);
            int a = amount;
            float finalPrice = 0;

            if (nonDynamic)
                return dp * a;
                

            for (int i = a; i > 0; i--)
            {
                finalPrice = finalPrice + (ds / s * dp);
                s--;
            }

            return finalPrice;
        }

        static float CalcSellPrice(object defaultStock, object defaultPrice, object stock, int amount, bool nonDynamic = false)
        {
            float ds = Convert.ToSingle(defaultStock);
            float dp = Convert.ToSingle(defaultPrice);
            float s = Convert.ToSingle(stock);
            int a = amount;
            float finalPrice = 0;

            if (nonDynamic)
                return dp * a;

            for (int i = amount; i > 0; i--)
            {
                finalPrice = finalPrice + Math.Min(ds * dp, ds / s * dp);
                s++;
            }

            return finalPrice;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // GUI ///////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static CuiPanel _myPanel = null;

        static CuiPanel GetPanel()
        {
            if(_myPanel== null)
            {
                _myPanel = new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    CursorEnabled = true
                };
            }
            return _myPanel;
        }

        private static CuiElementContainer CreateShopOverlay(string shopname)
        {
            return new CuiElementContainer
            {
                {
                    GetPanel(),
                    new CuiElement().Parent,
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = shopname, FontSize = 30, Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Item", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.2 0.6", AnchorMax = "0.4 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Stock", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.5 0.6", AnchorMax = "0.6 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Buy", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.55 0.6", AnchorMax = "0.7 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Sell", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.75 0.6", AnchorMax = "0.9 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    //new CuiButton
                    //{
                    //    Button = {Close = ShopOverlayName, Color = "0.5 0.5 0.5 0.2"},
                    //    RectTransform = {AnchorMin = "0.5 0.15", AnchorMax = "0.7 0.2"},
                    //    Text = {Text = "Close", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    //},
                    //ShopOverlayName
                    new CuiButton
                    {
                        Button = {Command = $"shop.close", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.5 0.15", AnchorMax = "0.7 0.2"},
                        Text = {Text = "Close", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    ShopOverlayName
                }
            };
        }

        private readonly CuiLabel shopDescription = new CuiLabel
        {
            Text = { Text = "{shopdescription}", FontSize = 15, Align = TextAnchor.MiddleCenter },
            RectTransform = { AnchorMin = "0.2 0.7", AnchorMax = "0.8 0.79" }
        };

        private static CuiElementContainer CreateShopItemEntry(string price, string dStock, string stock, float ymax, float ymin, string shop, string item, string color, bool nonDynamic, bool sell = false)
        {
            string displayPrice = "0";
            var container = new CuiElementContainer();
            if (!nonDynamic)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = stock, FontSize = 15, Align = TextAnchor.UpperLeft },
                    RectTransform = { AnchorMin = $"{0.5} {ymin}", AnchorMax = $"{0.6} {ymax}" }
                }, ShopOverlayName, ShopDescOverlay);
            }

            if (sell)
                displayPrice = Math.Round(CalcSellPrice(dStock, price, stock, 1, nonDynamic), 1).ToString();
            else
                displayPrice = Math.Round(CalcBuyPrice(dStock, price, stock, 1, nonDynamic), 1).ToString();

            if (displayPrice != "Infinity")
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = displayPrice, FontSize = 15, Align = TextAnchor.UpperLeft },
                    RectTransform = { AnchorMin = $"{(sell ? 0.75 : 0.55)} {ymin}", AnchorMax = $"{(sell ? 0.78 : 0.6)} {ymax}" }
                }, ShopOverlayName, ShopDescOverlay);
            }

            var steps = new[] {1, 10, 100, 1000};
            for (var i = 0; i < steps.Length; i++)
            {
                if (Convert.ToInt32(stock) >= steps[i] || nonDynamic || sell)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Command = $"shop.{(sell ? "sell" : "buy")} {shop} {item} {steps[i]}", Color = color },
                        RectTransform = { AnchorMin = $"{(sell ? 0.8 : 0.6) + i * 0.03} {ymin}", AnchorMax = $"{(sell ? 0.83 : 0.63) + i * 0.03} {ymax}" },
                        Text = { Text = steps[i].ToString(), FontSize = 15, Align = TextAnchor.MiddleCenter }
                    }, ShopOverlayName);

                    container.Add(new CuiLabel
                    {
                        Text = { Text = Convert.ToString((sell ? Math.Round(CalcSellPrice(dStock, price, stock, steps[i], nonDynamic), 1) : Math.Round(CalcBuyPrice(dStock, price, stock, steps[i], nonDynamic), 1))), FontSize = 8, Align = TextAnchor.UpperLeft },
                        RectTransform = { AnchorMin = $"{(sell ? 0.8 : 0.6) + i * 0.03} {ymin - 0.04}", AnchorMax = $"{(sell ? 0.83 : 0.63) + i * 0.03} {ymax - 0.04}" }
                    }, ShopOverlayName, ShopDescOverlay);
                }
            }
            return container;
        }

        private static CuiElementContainer CreateShopItemIcon(string name, float ymax, float ymin, string url)
        {
            var rawImage = new CuiRawImageComponent();
            if (url.StartsWith("http"))
                rawImage.Url = url;
            else
                rawImage.Sprite = url;
            var container = new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        Text = {Text = name, FontSize = 15, Align = TextAnchor.UpperLeft},
                        RectTransform = {AnchorMin = $"0.2 {ymin}", AnchorMax = $"0.4 {ymax}"}
                    },
                    ShopOverlayName, ShopDescOverlay
                },
                new CuiElement
                {
                    Parent = ShopOverlayName,
                    Components =
                    {
                        rawImage,
                        new CuiRectTransformComponent {AnchorMin = $"0.1 {ymin}", AnchorMax = $"0.13 {ymax}"}
                    }
                }
            };
            return container;
        }

        private static CuiElementContainer CreateShopChangePage(string currentshop, int shoppageminus, int shoppageplus)
        {
            return new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button = {Command = $"shop.show {currentshop} {shoppageminus}", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.2 0.15", AnchorMax = "0.3 0.2"},
                        Text = {Text = "<<", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    ShopOverlayName
                },
                {
                    new CuiButton
                    {
                        Button = {Command = $"shop.show {currentshop} {shoppageplus}", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.35 0.15", AnchorMax = "0.45 0.2"},
                        Text = {Text = ">>", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    ShopOverlayName
                }
            };
        }

        readonly Hash<ulong, int> shopPage = new Hash<ulong, int>();
		
		void CloseShop(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, ShopOverlayName);
            players.RemoveAll(x => x.player == player);
        }
		
        void ShowShop(BasePlayer player, string shopid, int from = 0, bool fullRefresh = true)
        {
            ShopPlayer p = new ShopPlayer();
            p.player = player;
            p.shopid = shopid;
            p.page = from;
            if (!players.Exists(x => x.player == p.player))
            {
                players.Add(p);
            }
            else
            {
                players.Find(x => x.player == p.player).page = from;
            }

            shopPage[player.userID] = from;
            if (!Shops.ContainsKey(shopid))
            {
                SendReply(player, MessageErrorNoShop);
                return;
            }
			
            if (Economics == null)
            {
                SendReply(player, MessageShowNoEconomics);
                return;
            }
			
			var playerCoins = Convert.ToSingle(Economics.Call("GetPlayerMoney", player.userID));
			
			
            var shop = (Dictionary<string, object>) Shops[shopid];

			
            shopDescription.Text.Text = string.Format((string) shop["description"], playerCoins);





            if (!fullRefresh)
            {
                CuiHelper.DestroyUi(player, ShopDescOverlay);
                var container = new CuiElementContainer();

                if (from < 0)
                {
                    CuiHelper.AddUi(player, container);
                    return;
                }

                var itemslist = new Dictionary<string, Dictionary<string, bool>>();
                if (shop.ContainsKey("buy"))
                {
                    foreach (string itemname in (List<object>)shop["buy"])
                    {
                        Dictionary<string, bool> itemEntry;
                        if (!itemslist.TryGetValue(itemname, out itemEntry))
                            itemslist[itemname] = itemEntry = new Dictionary<string, bool>();
                        itemEntry["buy"] = true;
                    }
                }
                if (shop.ContainsKey("sell"))
                {
                    foreach (string itemname in (List<object>)shop["sell"])
                    {
                        Dictionary<string, bool> itemEntry;
                        if (!itemslist.TryGetValue(itemname, out itemEntry))
                            itemslist[itemname] = itemEntry = new Dictionary<string, bool>();
                        itemEntry["sell"] = true;
                    }
                }
                var current = 0;
                foreach (var pair in itemslist)
                {
                    if (!ShopCategories.ContainsKey(pair.Key)) continue;

                    if (current >= from && current < from + 7)
                    {
                        var itemdata = (Dictionary<string, object>)ShopCategories[pair.Key];
                        var pos = 0.55f - 0.05f * (current - from);
                        bool nonDynamic = true;
                        string dStock = "0";
                        string stock = "0";

                        container.AddRange(CreateShopItemIcon(pair.Key, pos + 0.05f, pos, (string)itemdata["img"]));
                        if (itemdata.ContainsKey("dstock") && itemdata.ContainsKey("stock"))
                        {
                            nonDynamic = false;
                            dStock = (string)itemdata["dstock"];
                            stock = (string)itemdata["stock"];
                        }

                        if (pair.Value.ContainsKey("buy"))
                            container.AddRange(CreateShopItemEntry((string)itemdata["buy"], dStock, stock, pos + 0.05f, pos, $"'{shopid}'", $"'{pair.Key}'", "0 0.6 0 0.1", nonDynamic));
                        if (pair.Value.ContainsKey("sell"))
                            container.AddRange(CreateShopItemEntry((string)itemdata["sell"], dStock, stock, pos + 0.05f, pos, $"'{shopid}'", $"'{pair.Key}'", "1 0 0 0.1", nonDynamic, true));
                    }
                    current++;
                }
                container.Add(shopDescription, ShopOverlayName, ShopDescOverlay);
                CuiHelper.AddUi(player, container);
            }
            else
            {
                CuiHelper.DestroyUi(player, ShopOverlayName);
                var container = CreateShopOverlay((string)shop["name"]);
                container.Add(shopDescription, ShopOverlayName, "ShopDescOverlay");

              
                if (from < 0)
                {
                    CuiHelper.AddUi(player, container);
                    return;
                }

                var itemslist = new Dictionary<string, Dictionary<string, bool>>();
                if (shop.ContainsKey("buy"))
                {
                    foreach (string itemname in (List<object>)shop["buy"])
                    {
                        Dictionary<string, bool> itemEntry;
                        if (!itemslist.TryGetValue(itemname, out itemEntry))
                            itemslist[itemname] = itemEntry = new Dictionary<string, bool>();
                        itemEntry["buy"] = true;
                    }
                }
                if (shop.ContainsKey("sell"))
                {
                    foreach (string itemname in (List<object>)shop["sell"])
                    {
                        Dictionary<string, bool> itemEntry;
                        if (!itemslist.TryGetValue(itemname, out itemEntry))
                            itemslist[itemname] = itemEntry = new Dictionary<string, bool>();
                        itemEntry["sell"] = true;
                    }
                }
                var current = 0;
                foreach (var pair in itemslist)
                {
                    if (!ShopCategories.ContainsKey(pair.Key)) continue;

                    if (current >= from && current < from + 7)
                    {
                        var itemdata = (Dictionary<string, object>)ShopCategories[pair.Key];
                        var pos = 0.55f - 0.05f * (current - from);
                        bool nonDynamic = true;
                        string dStock = "0";
                        string stock = "0";

                        container.AddRange(CreateShopItemIcon(pair.Key, pos + 0.05f, pos, (string)itemdata["img"]));
                        if (itemdata.ContainsKey("dstock") && itemdata.ContainsKey("stock"))
                        {
                            nonDynamic = false;
                            dStock = (string)itemdata["dstock"];
                            stock = (string)itemdata["stock"];
                        }

                        if (pair.Value.ContainsKey("buy"))
                            container.AddRange(CreateShopItemEntry((string)itemdata["buy"], dStock, stock, pos + 0.05f, pos, $"'{shopid}'", $"'{pair.Key}'", "0 0.6 0 0.1", nonDynamic));
                        if (pair.Value.ContainsKey("sell"))
                            container.AddRange(CreateShopItemEntry((string)itemdata["sell"], dStock, stock, pos + 0.05f, pos, $"'{shopid}'", $"'{pair.Key}'", "1 0 0 0.1", nonDynamic, true));
                    }
                    current++;
                }
                var minfrom = from <= 7 ? 0 : from - 7;
                var maxfrom = from + 7 >= current ? from : from + 7;
                container.AddRange(CreateShopChangePage(shopid, minfrom, maxfrom));
                CuiHelper.AddUi(player, container);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Shop Functions ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        object CanDoAction(BasePlayer player, string shop, string item, string ttype)
        {
            var shopdata = (Dictionary<string, object>) Shops[shop];
            if (!shopdata.ContainsKey(ttype))
                return string.Format(MessageErrorNoActionShop, ttype);
            var actiondata = (List<object>) shopdata[ttype];
            if (!actiondata.Contains(item))
                return string.Format(MessageErrorNoActionItem, ttype);
            return true;
        }

        bool CanFindNPC(Vector3 pos, string npcid)
        {
            return Physics.OverlapSphere(pos, 3f, playersMask).Select(col => col.GetComponentInParent<BasePlayer>()).Any(player => player != null && player.UserIDString == npcid);
        }

        object CanShop(BasePlayer player, string shopname)
        {
            if (!Shops.ContainsKey(shopname)) return MessageErrorNoShop;
            if (shopname != "chat" && !CanFindNPC(player.transform.position, shopname))
                return MessageErrorNoNPC;
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Buy Functions /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        object TryShopBuy(BasePlayer player, string shop, string item, int amount)
        {
            object success = CanShop(player, shop);
            if (success is string) return success;
            success = CanDoAction(player, shop, item, "buy");
            if (success is string) return success;
            success = CanBuy(player, item, amount);
            if (success is string) return success;
            success = TryGive(player, item, amount);
            if (success is string) return success;
            var itemdata = ShopCategories[item] as Dictionary<string, object>;

            bool nonDynamic = true;
            string dStock = "0";
            string stock = "0";
            if (itemdata.ContainsKey("dstock") && itemdata.ContainsKey("stock"))
            {
                nonDynamic = false;
                dStock = (string)itemdata["dstock"];
                stock = (string)itemdata["stock"];
            }

            Economics?.Call("Withdraw", player.userID, CalcBuyPrice(dStock, itemdata["buy"], stock, amount, nonDynamic));
            if(!nonDynamic)
                itemdata["stock"] = Convert.ToString(Convert.ToSingle(itemdata["stock"]) - amount);
            ShopCategories[item] = itemdata;

            return true;
        }
        object TryGive(BasePlayer player, string item, int amount)
        {
            var itemdata = (Dictionary<string, object>) ShopCategories[item];

            if(itemdata.ContainsKey("cmd"))
            {
                var cmds = (List<object>) itemdata["cmd"];
                foreach (var cmd in cmds)
                {
                    ConsoleSystem.Run.Server.Normal(cmd.ToString()
                                                                .Replace("$player.id", player.UserIDString)
                                                                .Replace("$player.name", player.displayName)
                                                                .Replace("$player.x", player.transform.position.x.ToString())
                                                                .Replace("$player.y", player.transform.position.y.ToString())
                                                                .Replace("$player.z", player.transform.position.z.ToString()));
                }
            }
            if (itemdata.ContainsKey("item"))
            {
                string itemname = itemdata["item"].ToString();
                object iskit = Kits?.Call("isKit", itemname);

                if (iskit is bool && (bool)iskit)
                {
                    object successkit = Kits.Call("GiveKit", player, itemname);
                    if (successkit is bool && !(bool)successkit) return MessageErrorRedeemKit;
                    return true;
                }
                object success = GiveItem(player, itemname, amount, player.inventory.containerMain);
                if (success is string) return success;
            }
            return true;
        }

        private object GiveItem(BasePlayer player, string itemname, int amount, ItemContainer pref)
        {
            itemname = itemname.ToLower();

            bool isBP = false;
            if (itemname.EndsWith(" bp"))
            {
                isBP = true;
                itemname = itemname.Substring(0, itemname.Length - 3);
            }
            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null) return MessageErrorItemNoExist;
            int stack = definition.stackable;
            if (isBP)
                stack = 1;
            if (stack < 1) stack = 1;
            for (var i = amount; i > 0; i = i - stack)
            {
                var giveamount = i >= stack ? stack : i;
                if (giveamount < 1) return true;
                player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, giveamount, isBP), pref);
            }
            return true;
        }
        object CanBuy(BasePlayer player, string item, int amount)
        {
            if (Economics == null) return MessageShowNoEconomics;
            var playerCoins = Convert.ToSingle(Economics.Call("GetPlayerMoney", player.userID));
            if (!ShopCategories.ContainsKey(item)) return MessageErrorItemNoValid;

            var itemdata = (Dictionary<string, object>) ShopCategories[item];

            bool nonDynamic = true;
            string dStock = "0";
            string stock = "0";
            if (itemdata.ContainsKey("dstock") && itemdata.ContainsKey("stock"))
            {
                nonDynamic = false;
                dStock = (string)itemdata["dstock"];
                stock = (string)itemdata["stock"];
            }

            if (!itemdata.ContainsKey("buy")) return MessageErrorBuyPrice;
            var buyprice = CalcBuyPrice(dStock, itemdata["buy"], stock, amount, nonDynamic);

            if (playerCoins < buyprice)
                return string.Format(MessageErrorNotEnoughMoney, buyprice, amount, item);

            if (itemdata.ContainsKey("dstock") && itemdata.ContainsKey("stock"))
                if (Convert.ToSingle(Convert.ToInt32(itemdata["stock"]) - amount) < 0)
                    return MessageErrorItemNoExist;

            return true;
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Sell Functions ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        object TryShopSell(BasePlayer player, string shop, string item, int amount)
        {
            object success = CanShop(player, shop);
            if (success is string) return success;
            success = CanDoAction(player, shop, item, "sell");
            if (success is string) return success;
            success = CanSell(player, item, amount);
            if (success is string) return success;
            success = TrySell(player, item, amount);
            if (success is string) return success;
            var itemdata = (Dictionary<string, object>) ShopCategories[item];

            bool nonDynamic = true;
            string dStock = "0";
            string stock = "0";
            if (itemdata.ContainsKey("dstock") && itemdata.ContainsKey("stock"))
            {
                nonDynamic = false;
                dStock = (string)itemdata["dstock"];
                stock = (string)itemdata["stock"];
            }

            Economics?.Call("Deposit", player.userID, CalcSellPrice(dStock, itemdata["sell"], stock, amount, nonDynamic));
            if (!nonDynamic)
                itemdata["stock"] = Convert.ToString(Convert.ToSingle(itemdata["stock"]) + amount);
            
            ShopCategories[item] = itemdata;

            return true;
        }
        object TrySell(BasePlayer player, string item, int amount)
        {
            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            if (!itemdata.ContainsKey("item")) return MessageErrorItemItem;
            string itemname = itemdata["item"].ToString();
            object iskit = Kits?.Call("isKit", itemname);

            if (iskit is bool && (bool)iskit) return "You can't sell kits";
            object success = TakeItem(player, itemname, amount);
            if (success is string) return success;
            return true;
        }
        private object TakeItem(BasePlayer player, string itemname, int amount)
        {
            itemname = itemname.ToLower();

            if (itemname.EndsWith(" bp"))
            {
                //isBP = true;
                //itemname = itemname.Substring(0, itemname.Length - 3);
                return "You can't sell blueprints";
            }
            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null) return MessageErrorItemNoExist;

            int pamount = player.inventory.GetAmount(definition.itemid);
            if (pamount < amount) return MessageErrorNotEnoughSell;

            player.inventory.Take(null, definition.itemid, amount);
            return true;
        }
        object CanSell(BasePlayer player, string item, int amount)
        {
            if (!ShopCategories.ContainsKey(item)) return MessageErrorItemNoValid;
            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            if (!itemdata.ContainsKey("sell")) return MessageErrorSellPrice;
            return true;
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("shop")]
        void cmdShop(BasePlayer player, string command, string[] args)
        {
            if(!Shops.ContainsKey("chat"))
            {
                SendReply(player, MessageErrorNPCRange);
                return;
            }
            ShowShop(player, "chat");
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Console Commands //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("shop.show")]
        void ccmdShopShow(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(2)) return;
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;
            var shopid = arg.GetString(0).Replace("'", "");
            var shoppage = arg.GetInt(1);

            ShowShop(player, shopid, shoppage);
        }

        [ConsoleCommand("shop.close")]
        void ccmdShopClose(ConsoleSystem.Arg arg)
        {
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;

            CloseShop(player);
        }

        [ConsoleCommand("shop.buy")]
        void ccmdShopBuy(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(3)) return;
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;
            object success = Interface.Call("canShop", player);
            if(success != null)
            {
                string message = "You are not allowed to shop at the moment";
                if (success is string)
                    message = (string)success;
                SendReply(player, message);
                return;
            }

            string shop = arg.Args[0].Replace("'", "");
            string item = arg.Args[1].Replace("'", "");
            int amount = arg.GetInt(2);
            success = TryShopBuy(player, shop, item, amount);
            if(success is string)
            {
                SendReply(player, (string)success);
                return;
            }
            SendReply(player, string.Format(MessageBought, amount, item));

            RefreshShop(shop);
            //ShowShop(player, shop, shopPage[player.userID], false);
        }
        [ConsoleCommand("shop.sell")]
        void ccmdShopSell(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(3)) return;
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;
            object success = Interface.Call("canShop", player);
            if (success != null)
            {
                string message = "You are not allowed to shop at the moment";
                if (success is string)
                    message = (string)success;
                SendReply(player, message);
                return;
            }
            string shop = arg.Args[0].Replace("'", "");
            string item = arg.Args[1].Replace("'", "");
            int amount = Convert.ToInt32(arg.Args[2]);
            success = TryShopSell(player, shop, item, amount);
            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }
            SendReply(player, string.Format(MessageSold, amount, item));

            RefreshShop(shop);
            //ShowShop(player, shop, shopPage[player.userID], false);
        }
    }
}
