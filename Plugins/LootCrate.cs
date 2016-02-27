
using System;
using System.Collections.Generic;

using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Loot Crate", "Stiffi136", 1.0)]
    class LootCrate : RustPlugin
    {
        private const string InterfaceName = "GambleInterface";
        private const string InterfaceChestName = "ChestInterface";

        class GambleItem
        {
            public string itemName;
            public int itemID;
            public int skinID;
            public string itemImg;
            public string color;
        }

        class GambleRarityClass
        {
            public string name;
            public string color;
            public int chance;
            public List<object> items;
        }

        //////////////////////////
        // References
        //////////////////////////

        [PluginReference("Economics")]
        Plugin Economics;

        void OnServerInitialized()
        {
            if (!Economics) PrintWarning("Economics plugin not found. " + Name + " will not function!");
        }
        

        //////////////////////////
        // Config
        //////////////////////////

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private static Dictionary<string, object> Items = DefaultItems();
        private static Dictionary<string, object> RarityClasses = DefualtRarityClasses();

        static string MessageShowNoEconomics = "Couldn't get informations out of Economics. Is it installed?";
        static string MessageNotEnoughMoney = "You don't have enough money to open this chest.";

        static int OpeningCost = 1000;

        static string InterfaceNameLabel = "You have found a Chest!";


        private void Init()
        {
            CheckCfg("Item List", ref Items);
            CheckCfg("Rarity Classes", ref RarityClasses);
            CheckCfg("Message - Error - No Econonomics", ref MessageShowNoEconomics);
            CheckCfg("Message - Error - Not Enough Money", ref MessageNotEnoughMoney);
            CheckCfg("OpeningCost", ref OpeningCost);
            CheckCfg("InterfaceNameLabel", ref InterfaceNameLabel);
            SaveConfig();

            PrintDropChances();
        }

        static Dictionary<string, object> DefualtRarityClasses()
        {
            var classes = new Dictionary<string, object>
            {
                {
                    "Common", new Dictionary<string, object>
                    {
                        {"name", "Common"},
                        {"color", "1 1 1 0.1"},
                        {"chance", "5000"},
                        {"items", new List<object> {"Red Tshirt"}}
                    }
                },
                {
                    "Uncommon", new Dictionary<string, object>
                    {
                        {"name", "Uncommon"},
                        {"color", "0 0.6 0 0.1"},
                        {"chance", "2000"},
                        {"items", new List<object> {"Grey Longsleeve T-Shirt"}}
                    }
                },
                {
                    "Rare", new Dictionary<string, object>
                    {
                        {"name", "Rare"},
                        {"color", "0 0 1 0.1"},
                        {"chance", "1000"},
                        {"items", new List<object> {"Wood Camo Sleeping Bag"}}
                    }
                },
                {
                    "Epic", new Dictionary<string, object>
                    {
                        {"name", "Epic"},
                        {"color", "0.6 0 1 0.1"},
                        {"chance", "500"},
                        {"items", new List<object> {"Reaper Note Pistol"}}
                    }
                },
                {
                    "Legendary", new Dictionary<string, object>
                    {
                        {"name", "Legendary"},
                        {"color", "1 0.6 0 0.1"},
                        {"chance", "100"},
                        {"items", new List<object> {"Tempered AK47"}}
                    }
                }

            };
            return classes;
        }

        static Dictionary<string, object> DefaultItems()
        {
            var items = new Dictionary<string, object>
            {
                {
                    "Red Tshirt", new Dictionary<string, object>
                    {
                        {"name", "Red T-Shirt"},
                        {"itemid", "-864578046"},
                        {"skinid", "101" },
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/f/f3/Red_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053820"}
                    }
                },
                {
                    "Grey Longsleeve T-Shirt", new Dictionary<string, object>
                    {
                        {"name", "Grey Longsleeve"},
                        {"itemid", "1660607208"},
                        {"skinid", "10005" },
                        {"img", "http://vignette4.wikia.nocookie.net/play-rust/images/5/53/Grey_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195329"}
                    }
                },
                {
                    "Wood Camo Sleeping Bag", new Dictionary<string, object>
                    {
                        {"name", "Wood Camo Sleeping Bag"},
                        {"itemid", "1253290621"},
                        {"skinid", "10076" },
                        {"img", "http://vignette3.wikia.nocookie.net/play-rust/images/5/59/Wood_Camo_Sleeping_Bag_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200728"}
                    }
                },
                {
                    "Reaper Note Pistol", new Dictionary<string, object>
                    {
                        {"name", "Reaper Note Pistol"},
                        {"itemid", "548699316"},
                        {"skinid", "10081" },
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/7/70/Reaper_Note_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200711"}
                    }
                },
                {
                    "Tempered AK47", new Dictionary<string, object>
                    {
                        {"name", "Tempered AK47"},
                        {"itemid", "-1461508848"},
                        {"skinid", "10138" },
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/a/a1/Tempered_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204335"}
                    }
                },
            };
            return items;
        }

        //////////////////////////
        // RNG
        //////////////////////////

        System.Random rng = new System.Random();

        int GetRandomNumber(int pool)
        {
            return rng.Next(1, pool + 1);
        }

        int GetChanceOfRarityClass(string rClass)
        {
            if (RarityClasses.ContainsKey(rClass))
            {
                var RarityClass = (Dictionary<string, object>)RarityClasses[rClass];
                if (RarityClass.ContainsKey("chance"))
                {
                    return Convert.ToInt32(RarityClass["chance"]);
                }
                else
                {
                    PrintWarning($"Could not find chance for rarity class {rClass}");
                    return 0;
                }
                
            }
            else
            {
                PrintWarning($"Could not find Rarity Class {rClass}");
                return 0;
            }

        }

        void PrintDropChances()
        {
            int pool = 0;

            foreach (var pair in RarityClasses)
            {
                pool = pool + GetChanceOfRarityClass(pair.Key);
            }

            foreach (var pair in RarityClasses)
            {
                float percentchance = (float)GetChanceOfRarityClass(pair.Key) / (float)pool * 100f;
                Puts($"Chance to find a {pair.Key} Item is {percentchance}%");
            }
        }

        string GetRandomRarity()
        {
            int pool = 0;

            foreach (var pair in RarityClasses)
            {
                pool = pool + GetChanceOfRarityClass(pair.Key);
            }

            int randomNumber = GetRandomNumber(pool);

            int current = 0;

            foreach (var pair in RarityClasses)
            {
                int next = current + GetChanceOfRarityClass(pair.Key);
                if (randomNumber > current && randomNumber <= next)
                    return pair.Key;
                else
                    current = next;
            }

            PrintWarning("Could not get random rarity");
            return null;
        }

        string GetRandomItemOfRarityClass(string rClass)
        {
            var RarityClass = (Dictionary<string, object>)RarityClasses[rClass];
            if (RarityClass.ContainsKey("items"))
            {
                var items = (List<object>)RarityClass["items"];
                return items.GetRandom().ToString();
            }
            else
            {
                PrintWarning($"Could not find items for rarity class {rClass}");
                return null;
            }
        }

        GambleItem GetRandomItem()
        {
            string rarityName = GetRandomRarity();
            string itemName = GetRandomItemOfRarityClass(rarityName);
            GambleRarityClass rarity = LookUpRarityClass(rarityName);

            return LookUpItem(itemName, rarity);
        }


        //////////////////////////
        // Redeem Item
        //////////////////////////

        Item CreateItem(int itemid, int skin)
        {
            return ItemManager.CreateByItemID(itemid, 1, false, skin);
        }

        Sprite GetItemSprite(int itemid, int skin)
        {
            return ItemManager.CreateByItemID(itemid, 1, false, skin).info.iconSprite;
        }

        GambleRarityClass LookUpRarityClass(string classKey)
        {
            if (RarityClasses.ContainsKey(classKey))
            {
                GambleRarityClass rc = new GambleRarityClass();
                var rClass = (Dictionary<string, object>)RarityClasses[classKey];
                rc.name = Convert.ToString(rClass["name"]);
                rc.color = Convert.ToString(rClass["color"]);
                rc.chance = Convert.ToInt32(rClass["chance"]);
                rc.items = (List<object>)rClass["items"];
                return rc;
            }
            else
            {
                PrintWarning($"Could not find rarity {classKey}");
                return null;
            }
        }

        GambleItem LookUpItem(string itemKey, GambleRarityClass rClass)
        {
            if (Items.ContainsKey(itemKey))
            {
                GambleItem gi = new GambleItem();
                var item = (Dictionary<string, object>)Items[itemKey];
                gi.itemID = Convert.ToInt32(item["itemid"]);
                gi.itemImg = Convert.ToString(item["img"]);
                gi.itemName = Convert.ToString(item["name"]);
                gi.skinID = Convert.ToInt32(item["skinid"]);
                gi.color = rClass.color;
                return gi;
            }
            else
            {
                PrintWarning($"Could not find item {itemKey}");
                return null;
            }
        }

        bool GiveItem(BasePlayer player, int itemid, int skinid)
        {
            return player.inventory.GiveItem(CreateItem(itemid, skinid));
        }

        //////////////////////////
        // GUI
        //////////////////////////

        static CuiPanel _myPanel = null;

        static CuiPanel GetPanel()
        {
            if (_myPanel == null)
            {
                _myPanel = new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.8" },
                    RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.9 0.8" },
                    CursorEnabled = true
                };
            }
            return _myPanel;
        }

        private static CuiElementContainer CreateInterfaceStatics(string interfacename)
        {
            return new CuiElementContainer
            {
                {
                    GetPanel(),
                    new CuiElement().Parent,
                    InterfaceName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = InterfaceNameLabel, FontSize = 30, Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9"}
                    },
                    InterfaceName
                },
                {
                    new CuiButton
                    {
                        Button = {Command = "interface.close.all", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.4 0.15", AnchorMax = "0.6 0.2"},
                        Text = {Text = "Close", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    InterfaceName
                }
            };
        }

        private static CuiElementContainer CreateChestInterface(string url)
        {
            //Workaround for Button Bug
            int number = 1;
            number++;
            if (number > 3)
                number = 1;
            
            var rawImage = new CuiRawImageComponent();
            if (url.StartsWith("http"))
                rawImage.Url = url;
            else
                rawImage.Sprite = url;

            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Parent = InterfaceName,
                    Components =
                    {
                        rawImage,
                        new CuiRectTransformComponent {AnchorMin = "0.45 0.4", AnchorMax = "0.55 0.6"}
                    },
                    Name = "Chest",
                    
                },
                {
                    new CuiButton
                    {
                        Button = { Command = "chest.open", Color = "0.5 0.5 0.5 0.2" },
                        RectTransform = { AnchorMin = "0.4 0.25", AnchorMax = "0.6 0.3" },
                        Text = { Text = $"Open (Cost: {OpeningCost})", FontSize = 20, Align = TextAnchor.MiddleCenter }
                    },
                    InterfaceName, $"OpenButton{number}"
                }
            };

            return container;
        }

        private static CuiElementContainer CreateCountdownNumber(int number)
        {
            return new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        Text = {Text = number.ToString(), FontSize = 40, Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0.45 0.4", AnchorMax = "0.55 0.6" }
                    },
                    InterfaceName, "CountDownNumber"
                }
            };
        }

        private static CuiElementContainer CreateChestContent(GambleItem item)
        {
            //Workaround for Button Bug
            int number = 1;
            number++;
            if (number > 3)
                number = 1;

            var bgImage = new CuiRawImageComponent();
            bgImage.Color = item.color;
            var rawImage = new CuiRawImageComponent();
            if (item.itemImg.StartsWith("http"))
                rawImage.Url = item.itemImg;
            else
                rawImage.Sprite = item.itemImg;

            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Parent = InterfaceName,
                    Components =
                    {
                        bgImage,
                        new CuiRectTransformComponent {AnchorMin = "0.45 0.4", AnchorMax = "0.55 0.6"}
                    },
                    Name = "Item",
                },
                new CuiElement
                {
                    Parent = InterfaceName,
                    Components =
                    {
                        rawImage,
                        new CuiRectTransformComponent {AnchorMin = "0.45 0.4", AnchorMax = "0.55 0.6"}
                    },
                    Name = "Item",
                },
                {
                    new CuiButton
                    {
                        Button = { Command = $"chest.take {item.itemID} {item.skinID}", Color = "0.5 0.5 0.5 0.2" },
                        RectTransform = { AnchorMin = "0.55 0.25", AnchorMax = "0.7 0.3" },
                        Text = { Text = "Take", FontSize = 20, Align = TextAnchor.MiddleCenter }
                    },
                    InterfaceName, $"ChestButtonTake{number}"
                },
                {
                    new CuiButton
                    {
                        Button = { Command = "chest.toss", Color = "0.5 0.5 0.5 0.2" },
                        RectTransform = { AnchorMin = "0.3 0.25", AnchorMax = "0.45 0.3" },
                        Text = { Text = "Toss", FontSize = 20, Align = TextAnchor.MiddleCenter }
                    },
                    InterfaceName, $"ChestButtonToss{number}"
                }
            };

            return container;
        }

        void CloseGambleInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, InterfaceName);
        }

        void ShowGambleInterface(BasePlayer player)
        {
            var staticsContainer = CreateInterfaceStatics("Gamble Interface");
            var chestContainer = CreateChestInterface("http://vignette1.wikia.nocookie.net/play-rust/images/b/b2/Large_Wood_Box_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200336");

            for (int i = 1; i < 4; i++) //Workaround for Button Bug
            {
                CuiHelper.DestroyUi(player, $"OpenButton{i}");
                CuiHelper.DestroyUi(player, $"ChestButtonTake{i}");
                CuiHelper.DestroyUi(player, $"ChestButtonToss{i}");
            }
            CuiHelper.AddUi(player, staticsContainer);
            CuiHelper.AddUi(player, chestContainer);
        }

        void OpenChest(BasePlayer player)
        {
            for (int i = 1; i < 4; i++) //Workaround for Button Bug
                CuiHelper.DestroyUi(player, $"OpenButton{i}");
            CuiHelper.DestroyUi(player, "Chest");

            CuiHelper.AddUi(player, CreateCountdownNumber(3));
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", player.transform.position);
            timer.Once(1f, () =>
            {
                CuiHelper.DestroyUi(player, "CountDownNumber");
                CuiHelper.AddUi(player, CreateCountdownNumber(2));
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", player.transform.position);
            });
            timer.Once(2f, () =>
            {
                CuiHelper.DestroyUi(player, "CountDownNumber");
                CuiHelper.AddUi(player, CreateCountdownNumber(1));
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", player.transform.position);
            });
            timer.Once(3f, () =>
            {
                Effect.server.Run("assets/prefabs/building/door.hinged/effects/door-wood-open-start.prefab", player.transform.position);
                CuiHelper.DestroyUi(player, "CountDownNumber");
                ShowChestContent(player);
            });
        }

        void ShowChestContent(BasePlayer player)
        {
            GambleItem item = GetRandomItem();
            CuiHelper.AddUi(player, CreateChestContent(item));

        }

        void ReturnToStart(BasePlayer player)
        {
            for (int i = 1; i < 4; i++) //Workaround for Button Bug
            {
                CuiHelper.DestroyUi(player, $"OpenButton{i}");
                CuiHelper.DestroyUi(player, $"ChestButtonTake{i}");
                CuiHelper.DestroyUi(player, $"ChestButtonToss{i}");
            }
            CuiHelper.DestroyUi(player, "Item");

            CuiHelper.AddUi(player, CreateChestInterface("http://vignette1.wikia.nocookie.net/play-rust/images/b/b2/Large_Wood_Box_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200336"));
        }

        //////////////////////////
        // Other Stuff
        //////////////////////////

        bool CanOpen(BasePlayer player)
        {
            if (Convert.ToSingle(Economics.Call("GetPlayerMoney", player.userID)) < OpeningCost)
                return false;
            else
                return true;
        }

        void WithdrawMoney(BasePlayer player, int amount)
        {
            Economics.Call("Withdraw", player.userID, amount);
        }

        //////////////////////////
        // Chat Commands
        //////////////////////////

        [ChatCommand("lc")]
        void cmdOpenInterface(BasePlayer player, string command, string[] args)
        {
            ShowGambleInterface(player);
        }

        //////////////////////////
        // Console Commands
        //////////////////////////

        [ConsoleCommand("interface.close.all")]
        void ccmdInterfaceCloseAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;

            CloseGambleInterface(player);
        }

        [ConsoleCommand("chest.open")]
        void ccmdChestOpen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;

            if (CanOpen(player))
            {
                OpenChest(player);
                WithdrawMoney(player, OpeningCost);
            }
            else
                SendReply(player, MessageNotEnoughMoney);
        }

        [ConsoleCommand("chest.toss")]
        void ccmdChestToss(ConsoleSystem.Arg arg)
        {
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;

            ReturnToStart(player);
        }

        [ConsoleCommand("chest.take")]
        void ccmdChestTake(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(1)) return;
            var player = arg.connection?.player as BasePlayer;
            if (player == null) return;

            string itemid = arg.Args[0];
            string skinid = arg.Args[1];

            GiveItem(player, Convert.ToInt32(itemid), Convert.ToInt32(skinid));
            ReturnToStart(player);
        }

    }
}
