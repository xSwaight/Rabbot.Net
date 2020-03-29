﻿using Newtonsoft.Json;
using System.IO;

namespace Rabbot
{
    class Config
    {
        private const string configFolder = "Resources";
        private const string configFile = "config.json";


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
        public string Token;
        public string Environment;
        public string SentryDsn;
        public string TwitchToken;
        public string TwitchAccessToken;
        public string ConnectionString;
        public string CmdPrefix;
        public string OfficialPlayerURL;
    }

}
