using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot
{
    public static class Constants
    {
        #region Streak Constants
        public const int MinimumWordCount = 200;
        public const double ExpBoostPerLevel = 0.2;
        #endregion

        #region Emotes
        public static readonly Emote Sword = Emote.Parse("<a:sword:593493621400010795>");
        public static readonly Emote Shield = Emote.Parse("<a:shield:593498755441885275>");

        public static readonly Emote Glitch = Emote.Parse("<:glitch:597053743623700490>");
        public static readonly Emote Diego = Emote.Parse("<:diego:597054124294668290>");
        public static readonly Emote Shyguy = Emote.Parse("<:shyguy:597053511951187968>");
        public static readonly Emote Goldenziege = Emote.Parse("<:goldengoat:597052540290465794>");

        public static readonly Emote Doggo = Emote.Parse("<:doggo:597065709339672576>");
        public static readonly Emote Slot = Emote.Parse("<a:slot:597872810760732672>");

        public static readonly Emote EggGoatR = Emote.Parse("<:egggoatr:695322635550064681>");
        public static readonly Emote EggGoatL = Emote.Parse("<:egggoatl:695336945760469013>");

        public static readonly Emoji Yes = new Emoji("✅");
        public static readonly Emoji No = new Emoji("❌");

        public static readonly Emoji Fire = new Emoji("🔥");

        public static readonly Emoji thumbsUp = new Emoji("👍");
        public static readonly Emoji thumbsDown = new Emoji("👎");
        #endregion

        #region API URLs
        public const string DogApi = "https://dog.ceo/api/breeds/image/random";
        public const string CatApi = "http://aws.random.cat/meow";
        public const string CoronaApi = "https://corona.lmao.ninja/countries";
        public const string RemnantsPlayerApi = "https://api.remdb.net/playercount";
        #endregion

        public const string AnnouncementIgnoreTag = "[silent]";

        #region EasterEvent
        public const int EasterDespawnTime = 4;
        public const int EasterMinRespawnTime = 5;
        public const int EasterMaxRespawnTime = 10;
        public const int EasterMinChannelUser = 50;

        public static readonly DateTime StartTime = new DateTime(2020, 4, 10, 0, 0, 0);
        public static readonly DateTime EndTime = new DateTime(2020, 4, 13, 23, 59, 59);
        #endregion
    }
}
