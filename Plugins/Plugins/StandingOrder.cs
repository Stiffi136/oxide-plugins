
using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Standing Order", "Stiffi136", 0.1)]
    class StandingOrder : RustPlugin
    {
        private Timer secondTimer;


        ////////////////////////////
        // Config
        ////////////////////////////

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        static int PayTime = 20;
        static int ResetTime = 8;


        ////////////////////////////
        // Data
        ////////////////////////////

        class StoredData
        {
            public bool wasExecuted;
            public List<OrderInfo> Orders = new List<OrderInfo>();
        }

        class OrderInfo
        {
            public ulong FromPlayerID;
            public ulong ToPlayerID;
            public float Amount;
            public int ExecutionsLeft;
        }

        StoredData storedData;



        void Init()
        {
            // Config
            CheckCfg("PayTimeHour", ref PayTime);
            CheckCfg("ResetTimeHour", ref ResetTime);
            SaveConfig();

            // Data
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("StandingOrder");

            // Timer
            secondTimer = timer.Repeat(1, 0, () => CheckTime());
        }

        void Unloaded()
        {
            if (secondTimer != null)
                secondTimer.Destroy();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("StandingOrder", storedData);
        }

        void CreateNewOrder(ulong fromPlayerID, ulong toPlayerID, float amount, int executions = -1)
        {
            var order = new OrderInfo();
            order.FromPlayerID = fromPlayerID;
            order.ToPlayerID = toPlayerID;
            order.Amount = amount;
            order.ExecutionsLeft = executions;

            storedData.Orders.Add(order);
        }

        void ShowOrder(int index)
        {
            if (storedData.Orders.Count - 1 < index || index < 0)
                return;
            Puts(storedData.Orders[index].FromPlayerID.ToString());
        }

        void CheckTime()
        {
            if (DateTime.Now.Hour <= PayTime && !storedData.wasExecuted)
            {
                storedData.wasExecuted = true;
                SaveData();
                Puts("Pay Time!");
            }

            if (DateTime.Now.Hour <= ResetTime && storedData.wasExecuted)
            {
                storedData.wasExecuted = false;
                SaveData();
            }
        }


        ////////////////////////////
        // Chat Commmands
        ////////////////////////////

        [ChatCommand("sotest")]
        void cmdStandingOrderTest(BasePlayer player, string command, string[] args)
        {
            CreateNewOrder(player.userID, player.userID, 1000);
            SaveData();
        }

        [ChatCommand("sotest2")]
        void cmdStandingOrderTest2(BasePlayer player, string command, string[] args)
        {
            ShowOrder(Convert.ToInt32(args[0]));
        }


        ////////////////////////////
        // Console Commands
        ////////////////////////////

    }
}
