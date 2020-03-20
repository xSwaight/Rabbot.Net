using Discord.WebSocket;
using Rabbot.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class StreakService
    {
        public void AddWords(Userfeatures userFeature, string msg)
        {
            var wordCountBefore = userFeature.TodaysWords;

            var wordCount = msg.CountWords();

            if (wordCount <= 0)
                return;

            userFeature.TotalWords += wordCount;
            userFeature.TodaysWords += wordCount;
            if (userFeature.TodaysWords >= Constants.MinimumWordCount && wordCountBefore < Constants.MinimumWordCount)
                userFeature.StreakLevel++;
        }

        public int GetWordsToday(Userfeatures userFeature)
        {
            return userFeature.TodaysWords;
        }

        public int GetWordsTotal(Userfeatures userFeature)
        {
            return userFeature.TotalWords;
        }

        public int GetStreakLevel(Userfeatures userFeature)
        {
            return userFeature.StreakLevel;
        }

        public void CheckTodaysWordcount(Userfeatures userFeature)
        {
            if (userFeature.TodaysWords < Constants.MinimumWordCount)
                userFeature.StreakLevel = 0;

            userFeature.TodaysWords = 0;
        }
    }
}
