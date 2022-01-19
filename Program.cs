using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Collections.Generic;
using NewsAPI;
using NewsAPI.Entities.Enums;
using System.Collections.Concurrent;
using System.Linq;
using Telegram.Bot.Types.Enums;
using System.Threading;

namespace TelegramBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new TelegramService();
            service.Start();
            Console.ReadLine();
            service.Stop();
        }
    }
}
