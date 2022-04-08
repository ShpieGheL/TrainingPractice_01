using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

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
            var conf = config.Get<Settings>();
            builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("commands.json", optional: false);
            config = builder.Build();
            var conf1 = config.Get<Statuses>();
            var service = new TelegramService(conf, conf1);
            service.Start();
            int status = 0;
            string[] statuses = new string[] { conf1.Status1, conf1.Status2, conf1.Status3, conf1.Status4, conf1.Status5 };
            while (true)
            {
                string c = Console.ReadLine();
                if (c.ToLower() == "stop")
                {
                    break;
                }
                else
                {
                    if (int.TryParse(c, out var result) == true)
                    {
                        if (Convert.ToInt16(c) >= 1 && Convert.ToInt16(c) <= 5)
                        {
                            status = Convert.ToInt16(c);
                            Console.WriteLine($"Выбрана команда №{c}");
                        }
                    }
                    else if (c.ToLower() == "false" || c.ToLower() == "true")
                    {
                        statuses[status - 1] = c.ToLower();
                        Console.WriteLine($"Установлен статус {c}");
                        var statuses1 = new Statuses
                        {
                            Status1 = statuses[0],
                            Status2 = statuses[1],
                            Status3 = statuses[2],
                            Status4 = statuses[3],
                            Status5 = statuses[4]
                        };
                        using FileStream createStream = File.Create("commands.json");
                        JsonSerializer.Serialize(createStream, statuses1);
                        createStream.Dispose();
                        Console.WriteLine(File.ReadAllText("commands.json"));
                    }
                }
            }
            service.Stop();
        }
    }

    public class Settings
    {
        public string BotToken { get; set; }
        public string NewsToken { get; set; }
        public string WeatherToken { get; set; }
        public string ExToken { get; set; }
    }

    public class Statuses
    {
        public string Status1 { get; set; }
        public string Status2 { get; set; }
        public string Status3 { get; set; }
        public string Status4 { get; set; }
        public string Status5 { get; set; }
    }
}
