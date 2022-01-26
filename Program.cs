using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace TelegramBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);
            var config = builder.Build();
            var myFirstClass = config.Get<Settings>();
            var service = new TelegramService(myFirstClass);
            service.Start();
            Console.WriteLine("Нажмите Return для остановки работы");
            Console.ReadLine();
            service.Stop();
        }
    }
    public class Settings
    {
        public string BotToken { get; set; }
        public string NewsToken { get; set; }
        public string WeatherToken { get; set; }
    }
}
