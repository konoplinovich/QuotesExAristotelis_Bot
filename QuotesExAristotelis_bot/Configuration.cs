using System;

namespace QuotesExAristotelis_bot
{
    public static class Configuration
    {
        public readonly static string BotToken;
        //= "1719707237:AAFwjCBU_Etx01lV9dj9tPHtI83b2ZrACRM";

        static Configuration()
        {
            try
            {
                string token = Environment.GetEnvironmentVariable("TOKEN");
                BotToken = token;
            }
            catch()
            {
                BotToken = "";
            }
        }
    }
}