using NewsAPI;
using NewsAPI.Entities.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    public class TelegramService
    {
        private string token { get; set; } = System.IO.File.ReadAllText("token.txt");
        public TelegramBotClient client;
        public NewsClient newsClient = new NewsClient(System.IO.File.ReadAllText("newstoken.txt"));
        public ConcurrentDictionary<long, int> States = new();
        public ConcurrentDictionary<long, int> rand = new();

        public void Start()
        {
            client = new TelegramBotClient(token);
            client.StartReceiving();
            client.OnMessage += MessageStatusChecker;
            client.OnMessage += MessageCommands;
        }

        public void Stop()
        {
            client.StopReceiving();
        }

        private async void MessageStatusChecker(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (!States.ContainsKey(msg.Chat.Id))
                switch (msg.Text)
                {
                    case "Старт":
                        BotStart(msg);
                        RemoveStates(msg.Chat.Id);
                        break;
                    case "Погода":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Введите название города:", replyMarkup: GetSities(), replyToMessageId: msg.MessageId);
                        States.TryAdd(msg.Chat.Id, 1);
                        break;
                    case "Новости":
                        News(msg);
                        break;
                    case "Случайное число":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Введите минимальное число:", replyToMessageId: msg.MessageId, replyMarkup: GetNoButtons());
                        States.TryAdd(msg.Chat.Id, 2);
                        break;
                    case "Информация":
                        await client.SendTextMessageAsync(msg.Chat.Id, "Команда Старт:\nОтмена всех команд.\n\nКоманда Погода:\nУзнать погоду на данный момент в любом городе.\n\nКоманда Новости:\nНовости по России на сервисе RT на данный момент.\n\nКоманда Случайное число:\nГенерация случайного числа.", replyToMessageId: msg.MessageId);
                        States.TryAdd(msg.Chat.Id, 2);
                        break;
                }
        }

        private void MessageCommands(object sender, MessageEventArgs e)
        {
            if (States.TryGetValue(e.Message.Chat.Id, out var state))
            {
                switch (state)
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
        }
        private async void News(Message msg)
        {
            try
            {
                var result = await newsClient.FetchNewsFromSource("rt");
                if (result.ResponseStatus == ResponseStatus.Ok)
                {
                    foreach (var article in result.Articles.Take(5))
                        await client.SendTextMessageAsync(msg.Chat.Id, article.Url);
                }
            }
            catch
            {

            }
        }
        
        private async void Weather(Message msg, int k)
        {
            string rs = "Не удаётся найти город";
            string recomendation = "";
            try
            {
                string url = $"http://api.openweathermap.org/data/2.5/weather?q={msg.Text}&lang=ru&units=metric&appid=" + System.IO.File.ReadAllText("weathertoken.txt");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                rs = json.ToString();
                Rootobject wthr = JsonConvert.DeserializeObject<Rootobject>(rs);
                if (wthr.main.temp > -5 && wthr.main.temp < 15)
                {
                    await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/b23/18d/b2318d70-5188-3faf-927d-b1be87d2e83f/17.webp");
                    recomendation = "Оденьтесь потеплее.";
                }
                if (wthr.main.temp < -5)
                {
                    await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/b23/18d/b2318d70-5188-3faf-927d-b1be87d2e83f/13.webp");
                    recomendation = "Не выходите на улицу, возьмите кружку горячего чая, закутайтесь в одеяло и наслаждайтесь теплом.";
                }
                if (wthr.main.temp > 15)
                {
                    await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/b23/18d/b2318d70-5188-3faf-927d-b1be87d2e83f/12.webp");
                    recomendation = "Оденьтесь полегче.";
                }
                rs = $"На данный момент в городе {wthr.name} {wthr.weather[0].description}:\n\nТемпература {Math.Floor(wthr.main.temp)}℃, ощущается как {Math.Floor(wthr.main.feels_like)}℃\nДавление ртутного столба: {wthr.main.pressure} мм\nСкорость ветра: {wthr.wind.speed} м/с";
                RemoveStates(msg.Chat.Id);
            }
            catch
            {
                await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/4dd/300/4dd300fd-0a89-3f3d-ac53-8ec93976495e/10.webp");
            }
            await client.SendTextMessageAsync(msg.Chat.Id, rs, replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
            await client.SendTextMessageAsync(msg.Chat.Id, $"Рекомендация:\n{recomendation}", replyMarkup: GetStandartButtons());
        }

        private async void StartCount(Message msg)
        {
            int i = Int32.MinValue;
            try
            {
                i = Convert.ToInt32(msg.Text);
            }
            catch { await client.SendTextMessageAsync(msg.Chat.Id, "Невозможно преобразовать число.", replyToMessageId: msg.MessageId); }
            if (i != Int32.MinValue)
            {
                RemoveStates(msg.Chat.Id);
                States.TryAdd(msg.Chat.Id, 3);
                await client.SendTextMessageAsync(msg.Chat.Id, "Введите максимальное число.");
                rand.TryAdd(msg.Chat.Id, Convert.ToInt32(msg.Text));
            }
        }
        private async void StopCount(Message msg)
        {
            try
            {
                Convert.ToInt32(msg.Text);
                if (Convert.ToInt32(msg.Text) >= rand[msg.Chat.Id])
                {
                    RemoveStates(msg.Chat.Id);
                    Random r = new();
                    string status = "";
                    await client.SendTextMessageAsync(msg.Chat.Id, $"Какая удача! Ваше число: {r.Next(rand[msg.Chat.Id], Convert.ToInt32(msg.Text))}", replyMarkup: GetStandartButtons());
                    rand.TryRemove(msg.Chat.Id, out var _);
                }
                else
                    await client.SendTextMessageAsync(msg.Chat.Id, "Максимальное число не может быть меньше минимального.", replyToMessageId: msg.MessageId);
            }
            catch
            {
                await client.SendTextMessageAsync(msg.Chat.Id, "Невозможно преобразовать число.", replyToMessageId: msg.MessageId);
            }
        }

        private async void BotStart(Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, "Используйте выделенные кнопки или команды для взаимодействия с ботом.", replyMarkup: GetStandartButtons());
        }

        private void RemoveStates(long m)
        {
            States.TryRemove(m, out var _);
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
        private static IReplyMarkup GetNoButtons()
        {
            return new ReplyKeyboardMarkup()
            {
                Keyboard = new List<List<KeyboardButton>>
                {

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
