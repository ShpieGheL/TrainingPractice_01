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

namespace TelegramBot
{
    class Program
    {
        private static string token { get; set; } = System.IO.File.ReadAllText("token.txt");
        public static TelegramBotClient client;
        public static NewsClient newsClient = new NewsClient(System.IO.File.ReadAllText("newstoken.txt"));
        public static Message std = null;
        public static Dictionary<long, int> keys = new Dictionary<long, int>();
        public static Dictionary<long, int> rand = new Dictionary<long, int>();
        static void Main(string[] args)
        {
            client = new TelegramBotClient(token);
            client.StartReceiving();
            client.OnMessage += MessageStatusChecker;
            client.OnMessage += MessageCommands;
            Console.ReadLine();
            client.StopReceiving();
        }

        private static void MessageCommands(object sender, MessageEventArgs e)
        {
            if (keys.ContainsKey(e.Message.Chat.Id))
                switch (keys[e.Message.Chat.Id])
                {
                    case 1:
                        Weather(e.Message, 1);
                        break;
                    case 2:
                        StartCount(e.Message);
                        break;
                    case 3:
                        StopCount(e.Message);
                        break;
                    case 4:
                        Weather(e.Message, 2);
                        break;
                }
        }

        private static async void MessageStatusChecker(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (!keys.ContainsKey(msg.Chat.Id))
                switch (msg.Text)
                {
                    case "Старт":
                        start(msg);
                        remkeys(msg.Chat.Id);
                        break;
                    case "Погода":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Введите название города:", replyMarkup: GetSities(), replyToMessageId: msg.MessageId);
                        keys.Add(msg.Chat.Id, 1);
                        break;
                    case "Прогноз погоды":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Введите название города:", replyMarkup: GetSities(), replyToMessageId: msg.MessageId);
                        keys.Add(msg.Chat.Id, 1);
                        break;
                    case "Новости":
                        news(msg);
                        break;
                    case "Случайное число":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Введите минимальное число:", replyToMessageId: msg.MessageId);
                        keys.Add(msg.Chat.Id, 2);
                        break;
                    case "Информация":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Команда Старт:\nОтмена всех команд.\n\nКоманда Погода:\nУзнать погоду на данный момент в любом городе.\n\nКоманда Новости:\nНовости по России на сервисе RT на данный момент.\n\nКоманда Случайное число:\nГенерация случайного числа.", replyToMessageId: msg.MessageId);
                        keys.Add(msg.Chat.Id, 2);
                        break;
                }
        }

        private static async void news(Message msg)
        {
            var result = await newsClient.FetchNewsFromSource("rt");
            if (result.ResponseStatus == ResponseStatus.Ok)
            {
                int i = 1;
                foreach (var article in result.Articles)
                {
                    await client.SendTextMessageAsync(msg.Chat.Id, article.Url);
                    if (i == 5)
                        break;
                    i++;
                }
            }
        }

        private static async void start(Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, "Используйте выделенные кнопки или команды для взаимодействия с ботом.",replyMarkup: GetStandartButtons());
        }

        private static async void Weather(Message msg, int k)
        {
            string rs = "Не удаётся найти город";
            try
            {
                string url = $"http://api.openweathermap.org/data/2.5/weather?q={msg.Text}&lang=ru&units=metric&appid="+System.IO.File.ReadAllText("weathertoken.txt");
                HttpWebRequest wrq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse wrs = (HttpWebResponse)wrq.GetResponse();
                using var stream = new StreamReader(wrs.GetResponseStream());
                rs = stream.ReadToEnd();
                Rootobject wthr = JsonConvert.DeserializeObject<Rootobject>(rs);
                if (wthr.main.temp > -3 && wthr.main.temp < 15)
                    await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/b23/18d/b2318d70-5188-3faf-927d-b1be87d2e83f/17.webp");
                if (wthr.main.temp < -3)
                    await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/b23/18d/b2318d70-5188-3faf-927d-b1be87d2e83f/13.webp");
                if (wthr.main.temp > 15)
                    await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/b23/18d/b2318d70-5188-3faf-927d-b1be87d2e83f/12.webp");
                rs = $"Погода в городе {wthr.name}:\n\nТемпература: {Math.Floor(wthr.main.temp)} ℃\nОщущается как: {Math.Floor(wthr.main.feels_like)} ℃\nДавление ртутного столба: {wthr.main.pressure} мм\nСкорость ветра: {wthr.wind.speed} км/ч";
                remkeys(msg.Chat.Id);
            }
            catch 
            {
                await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/4dd/300/4dd300fd-0a89-3f3d-ac53-8ec93976495e/10.webp");
            }
            await client.SendTextMessageAsync(msg.Chat.Id, rs, replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
        }

        private static void remkeys(long m)
        {
            if (keys.ContainsKey(m))
                keys.Remove(m);
        }

        private static async void StartCount(Message msg)
        {
            int i = Int32.MinValue;
            try
            {
                i = Convert.ToInt32(msg.Text);
            }
            catch { await client.SendTextMessageAsync(msg.Chat.Id, "Невозможно преобразовать число.", replyToMessageId: msg.MessageId); }
            if (i != Int32.MinValue)
            {
                remkeys(msg.Chat.Id);
                keys.Add(msg.Chat.Id, 3);
                await client.SendTextMessageAsync(msg.Chat.Id, "Введите максимальное число.");
                rand.Add(msg.Chat.Id, Convert.ToInt32(msg.Text));
            }
        }

        private static async void StopCount(Message msg)
        {
            try
            {
                Convert.ToInt32(msg.Text);
                if (Convert.ToInt32(msg.Text) >= rand[msg.Chat.Id])
                {
                    remkeys(msg.Chat.Id);
                    Random r = new Random();
                    await client.SendTextMessageAsync(msg.Chat.Id, $"Какая удача! Ваше число: {r.Next(rand[msg.Chat.Id], Convert.ToInt32(msg.Text))}", replyMarkup: GetStandartButtons());
                }
                else
                    await client.SendTextMessageAsync(msg.Chat.Id, "Максимальное число не может быть меньше минимального.", replyToMessageId: msg.MessageId);
            }
            catch
            {
                await client.SendTextMessageAsync(msg.Chat.Id, "Невозможно преобразовать число.", replyToMessageId: msg.MessageId);
            }
        }

        private static IReplyMarkup GetStandartButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Погода" }, new KeyboardButton { Text = "Новости" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Случайное число" }, new KeyboardButton { Text = "Информация"}}
                }
            };
        }

        private static IReplyMarkup GetSities()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Москва" }, new KeyboardButton { Text = "Санкт-Петербург" }, new KeyboardButton { Text = "Новосибирск" }, new KeyboardButton { Text = "Екатеринбург" }, new KeyboardButton { Text = "Казань" }, new KeyboardButton { Text = "Нижний Новгород" }, new KeyboardButton { Text = "Челябинск" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Омск" }, new KeyboardButton { Text = "Самара" }, new KeyboardButton { Text = "Ростов-на-Дону" }, new KeyboardButton { Text = "Уфа" }, new KeyboardButton { Text = "Красноярск" }, new KeyboardButton { Text = "Пермь" }, new KeyboardButton { Text = "Воронеж" } }
                }
            };
        }
    }
}
