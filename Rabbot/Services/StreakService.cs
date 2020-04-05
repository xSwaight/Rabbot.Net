using Discord.WebSocket;
using Rabbot.Database;
using Rabbot.Database.Rabbot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Services
{
    public class StreakService
    {
        public void AddWords(FeatureEntity userFeature, SocketMessage msg)
        {
            var wordCountBefore = userFeature.TodaysWords;

            var wordCount = Helper.MessageReplace(msg.Content).CountWords();

            if (wordCount <= 0)
                return;

            userFeature.TotalWords += wordCount;
            userFeature.TodaysWords += wordCount;
            if (userFeature.TodaysWords >= Constants.MinimumWordCount && wordCountBefore < Constants.MinimumWordCount)
            {
                userFeature.StreakLevel++;
                if(msg is SocketUserMessage userMsg)
                {
                    userMsg.AddReactionAsync(Constants.Fire);
                }
            }
        }

        public int GetWordsToday(FeatureEntity userFeature)
        {
            return userFeature.TodaysWords;
        }

        public int GetWordsTotal(FeatureEntity userFeature)
        {
            return userFeature.TotalWords;
        }

        public int GetStreakLevel(FeatureEntity userFeature)
        {
            return userFeature.StreakLevel;
        }

        public void CheckTodaysWordcount(FeatureEntity userFeature)
        {
            if (userFeature.TodaysWords < Constants.MinimumWordCount)
                userFeature.StreakLevel = 0;

            userFeature.TodaysWords = 0;
        }

        public List<FeatureEntity> GetRanking(IQueryable<FeatureEntity> userFeatures)
        {
            return userFeatures.Where(p => p.StreakLevel > 0).OrderByDescending(p => p.StreakLevel).ThenByDescending(p => p.TodaysWords).ToList();
        }
    }
}
