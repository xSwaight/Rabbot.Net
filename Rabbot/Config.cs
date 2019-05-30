using Newtonsoft.Json;
using System.IO;

namespace Rabbot
{
    class Config
    {
        private const string configFolder = "Resources";
#if DEBUG
        private const string configFile = "configDebug.json";
#else
        private const string configFile = "config.json";
#endif


        public static BotConfig bot;

        static Config()
        {
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(configFolder + "/" + configFile))
            {
                bot = new BotConfig();
                string json = JsonConvert.SerializeObject(bot, Formatting.Indented);
                File.WriteAllText(configFolder + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);
                bot = JsonConvert.DeserializeObject<BotConfig>(json);
            }
        }
    }

    public struct BotConfig
    {
        public string token;
        public string apiToken;
        public string twitchToken;
        public string connectionString;
        public string cmdPrefix;
        public int expMultiplier;
    }

}
