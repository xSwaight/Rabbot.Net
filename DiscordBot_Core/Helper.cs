using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot_Core
{
    public static class Helper
    {
        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static uint GetLevel(int? exp)
        {
            uint level = (uint)Math.Sqrt((uint)exp / 50);
            return level;
        }

    }
}
