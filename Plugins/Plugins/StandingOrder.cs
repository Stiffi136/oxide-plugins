
namespace Oxide.Plugins
{
    [Info("Standing Order", "Stiffi136", 0.1)]
    class StandingOrder : RustPlugin
    {
        private Timer secondTimer;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        static string PayTime = "20:00:00";

        void Init()
        {
            CheckCfg("PayTime", ref PayTime);
            SaveConfig();

            secondTimer = timer.Repeat(1, 0, () => CheckTime());
        }

        void CheckTime()
        {
            if (System.DateTime.Now.ToString("HH:mm:ss") == PayTime)
            {
                Puts("Pay Time!");
            }
        }

        void Unloaded()
        {
            if (secondTimer != null)
                secondTimer.Destroy();
        }
    }
}
