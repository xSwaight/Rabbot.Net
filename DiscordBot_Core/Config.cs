using Newtonsoft.Json;
using System.IO;

namespace DiscordBot_Core
{
    class Config
    {
        private const string configFolder = "Resources";
        private const string configFile = "config.json";
        private const string levelFile = "level.json";

        public static BotConfig bot;
        public static LevelConfig level;

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

            if (!File.Exists(configFolder + "/" + levelFile))
            {
                level = new LevelConfig();
                string json = JsonConvert.SerializeObject(level, Formatting.Indented);
                File.WriteAllText(configFolder + "/" + levelFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + levelFile);
                level = JsonConvert.DeserializeObject<LevelConfig>(json);
            }
        }
    }

    public struct BotConfig
    {
        public string token;
        public string apiToken;
        public string connectionString;
        public string cmdPrefix;
        public int expMultiplier;
    }

    public struct LevelConfig
    {
        public int expTableValue;
        public int expMultiplier;
    }

}
