using System.Collections.Generic;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MaxBags", "Stiffi136", 0.2)]
    public class MaxBags : RustPlugin
    {
        List<string> sleepDevices = new List<string>();

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        static int MaximumBags = 1;

        void Init()
        {
            CheckCfg("MaximumBags", ref MaximumBags);
            SaveConfig();

            sleepDevices.Add("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab (UnityEngine.GameObject)");
            sleepDevices.Add("assets/prefabs/deployable/bed/bed_deployed.prefab (UnityEngine.GameObject)");
        }

        bool CheckMaxBags(ulong userid)
        {
            int count = 0;
            foreach (SleepingBag bag in SleepingBag.FindForPlayer(userid, true))
            {
                count++;
            }

            if (count > MaximumBags)
                return true;
            else
                return false;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            if (planner.ownerPlayer == null) return;

            if (sleepDevices.Exists(x => x == gameobject.ToString()))
            {
                bool check = CheckMaxBags(planner.ownerPlayer.userID);
                if (check)
                {
                    gameobject.GetComponentInParent<BaseCombatEntity>().Kill(BaseNetworkable.DestroyMode.Gib);
                    SendReply(planner.ownerPlayer, "Maximum number of sleeping bags or beds reached!");
                    if (gameobject.ToString() == "assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab (UnityEngine.GameObject)")
                        planner.ownerPlayer.inventory.GiveItem(ItemManager.CreateByItemID(1253290621, 1));
                    else
                        planner.ownerPlayer.inventory.GiveItem(ItemManager.CreateByItemID(97409, 1));
                }
            }
        }
    }
}