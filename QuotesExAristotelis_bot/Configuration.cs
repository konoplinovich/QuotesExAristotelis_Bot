using System;

namespace QuotesExAristotelis_bot
{
    public static class Configuration
    {
        public readonly static string BotToken;

        static Configuration()
        {
            try
            {
                string token = Environment.GetEnvironmentVariable("TOKEN");
                BotToken = token;
            }
            catch
            {
                BotToken = "";
            }
        }
    }
}