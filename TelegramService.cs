using NewsAPI;
using NewsAPI.Entities.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private string token { get; set; }
        private TelegramBotClient client;
        private NewsClient newsClient;
        private string WToken;
        private string EToken;
        private ConcurrentDictionary<long, int> States = new();
        private readonly HttpClient httpClient = new HttpClient();
        string[] Statuses = new string[5];

        public TelegramService(Settings settings, Statuses statuses)
        {
            token = settings.BotToken;
            WToken = settings.WeatherToken;
            EToken = settings.ExToken;
            newsClient = new NewsClient(settings.NewsToken);
            Statuses[0] = statuses.Status1;
            Statuses[1] = statuses.Status2;
            Statuses[2] = statuses.Status3;
            Statuses[3] = statuses.Status4;
            Statuses[4] = statuses.Status5;
        }

        public void Start()
        {
            Console.WriteLine($"{DateTime.Now} Загрузка бота");
            client = new TelegramBotClient(token);
            Console.WriteLine($"{DateTime.Now} Создание клиента");
            client.StartReceiving();
            Console.WriteLine($"{DateTime.Now} Загрузка успешно завершена");
            client.OnMessage += MessageStatusChecker;
            client.OnMessage += MessageCommands;
        }

        public void Stop()
        {
            Console.WriteLine($"{DateTime.Now} Остановка работы, нажмите Return.");
            client.StopReceiving();
            Console.ReadKey();
        }

        private async void MessageStatusChecker(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine($"{DateTime.Now} {msg.Chat.Id}: {msg.Text}");
            if (!States.ContainsKey(msg.Chat.Id))
                switch (msg.Text.ToLower())
                {
                    case "/start":
                        if (Statuses[0] == "true")
                        {
                            StartVoid(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "/weather":
                        if (Statuses[1] == "true")
                        {
                            WeatherVoid1(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "/news":
                        if (Statuses[2] == "true")
                        {
                            News(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "/exchange":
                        if (Statuses[3] == "true")
                        {
                            ExchangeVoid1(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "/info":
                        if (Statuses[4] == "true")
                        {
                            InfoVoid(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    default:
                        string finalstring = "";
                        if (msg.Text.ToLower().Contains("/random "))
                        {
                            foreach (char c in msg.Text.Take(8))
                            {
                                finalstring += c;
                            }
                            if (finalstring.ToLower()=="/random ")
                            {
                                RandomStatementVoid(msg);
                            }
                        }
                        break;
                }
        }

        private async void StartVoid(Message msg)
        {
            RemoveStates(msg.Chat.Id);
            Console.WriteLine($"{DateTime.Now} Вызов команды Старт");
            await client.SendTextMessageAsync(msg.Chat.Id, "Используйте выделенные кнопки или команды для взаимодействия с ботом.", replyMarkup: GetStandartButtons());
        }

        private async void WeatherVoid1(Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, "Введите название города:", replyMarkup: GetSities(), replyToMessageId: msg.MessageId);
            Console.WriteLine($"{DateTime.Now} Вызов команды Погода");
            States.TryAdd(msg.Chat.Id, 1);
            Console.WriteLine($"{DateTime.Now} {msg.Chat.Id} добавлен в список статусов со статусом 1");
        }

        private async void WeatherVoid2(Message msg)
        {
            Console.WriteLine($"{DateTime.Now} Вызов функции погоды");
            string rs = "Не удаётся найти город";
            string recomendation = "";
            try
            {
                Console.WriteLine($"{DateTime.Now} Активация токена");
                string url = $"http://api.openweathermap.org/data/2.5/weather?q={msg.Text}&lang=ru&units=metric&appid=" + WToken;
                Console.WriteLine($"{DateTime.Now} Создание Http клиента");
                var response = await httpClient.GetAsync(url);
                Console.WriteLine($"{DateTime.Now} Получение данных");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"{DateTime.Now} Создание json");
                var json = await response.Content.ReadAsStringAsync();
                rs = json.ToString();
                Rootobject wthr = JsonConvert.DeserializeObject<Rootobject>(rs);
                Console.WriteLine($"{DateTime.Now} Десереализация json");
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
                Console.WriteLine($"{DateTime.Now} Вывод погоды");
                rs = $"На данный момент в городе {wthr.name} {wthr.weather[0].description}:\n\nТемпература {Math.Floor(wthr.main.temp)}℃, ощущается как {Math.Floor(wthr.main.feels_like)}℃\nДавление ртутного столба: {wthr.main.pressure} мм\nСкорость ветра: {wthr.wind.speed} м/с";
                RemoveStates(msg.Chat.Id);
                await client.SendTextMessageAsync(msg.Chat.Id, rs, replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
                await client.SendTextMessageAsync(msg.Chat.Id, $"Рекомендация:\n{recomendation}", replyMarkup: GetStandartButtons());
                await client.SendLocationAsync(msg.Chat.Id, wthr.coord.lat, wthr.coord.lon, replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
            }
            catch
            {
                Console.WriteLine($"{DateTime.Now} Произошла ошибка");
                await client.SendStickerAsync(msg.Chat.Id, "https://tlgrm.ru/_/stickers/4dd/300/4dd300fd-0a89-3f3d-ac53-8ec93976495e/10.webp");
                await client.SendTextMessageAsync(msg.Chat.Id, rs, replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
            }
        }

        private async void News(Message msg)
        {
            Console.WriteLine($"{DateTime.Now} Вызов функции новости");
            try
            {
                Console.WriteLine($"{DateTime.Now} Создание новостного клиента");
                var result = await newsClient.FetchNewsFromSource("rt");
                Console.WriteLine($"{DateTime.Now} Выбор ресурса");
                if (result.ResponseStatus == ResponseStatus.Ok)
                {
                    Console.WriteLine($"{DateTime.Now} Статус ОК");
                    foreach (var article in result.Articles.Take(5))
                        await client.SendTextMessageAsync(msg.Chat.Id, article.Url);
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} Статус Error");
                }
                Console.WriteLine($"{DateTime.Now} Успешно!");
            }
            catch
            {
                Console.WriteLine($"{DateTime.Now} Произошла ошибка");
            }
        }

        private async void ExchangeVoid1(Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, "Введите валюту.", replyToMessageId: msg.MessageId, replyMarkup: GetExchange());
            States.TryAdd(msg.Chat.Id, 2);
            Console.WriteLine($"{DateTime.Now} {msg.Chat.Id} добавлен в список статусов со статусом 2");
        }

        private async void ExchangeVoid2(Message msg)
        {
            string rs = "Не удаётся найти валюту.";
            try
            {
                Console.WriteLine($"{DateTime.Now} Активация токена");
                string url = $"https://v6.exchangerate-api.com/v6/" + EToken + $"/pair/{msg.Text}/RUB";
                Console.WriteLine($"{DateTime.Now} Создание Http клиента");
                var response = await httpClient.GetAsync(url);
                Console.WriteLine($"{DateTime.Now} Получение данных");
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"{DateTime.Now} Создание json");
                var json = await response.Content.ReadAsStringAsync();
                rs = json.ToString();
                Rootobject1 exc = JsonConvert.DeserializeObject<Rootobject1>(rs);
                Console.WriteLine($"{DateTime.Now} Десереализация json");
                rs = $"Курс {msg.Text} к RUB: {exc.conversion_rate}";
            }
            catch { }
            RemoveStates(msg.Chat.Id);
            await client.SendTextMessageAsync(msg.Chat.Id, rs, replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
        }

        private async void RandomStatementVoid(Message msg)
        {
            string txt = msg.Text.ToLower().Replace("/random ", "");
            int ist = 0;
            string one = "", two = "";
            foreach (char c in txt)
            {
                if (c == '{' || c == '}' || c == ':')
                {
                    ist++;
                }
                else
                {
                    if (ist == 1)
                    {
                        one += c;
                    }
                    if (ist == 2)
                    {
                        two += c;
                    }
                    if (ist == 3)
                    {
                        break;
                    }
                }
            }
            if (!int.TryParse(one, out var _) || !int.TryParse(two, out var _))
            {
                await client.SendTextMessageAsync(msg.Chat.Id, "Не удаётся преобразовать числа.", replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
                Console.WriteLine($"{DateTime.Now} Не удаётся преобразовать числа.");
            }
            else
            {
                Random r = new Random();
                await client.SendTextMessageAsync(msg.Chat.Id, $"Ваше число: {r.Next(Convert.ToInt32(one), Convert.ToInt32(two))}", replyToMessageId: msg.MessageId, replyMarkup: GetStandartButtons());
                Console.WriteLine($"{DateTime.Now} Вызов команды случайного числа.");
            }
        }

        private async void InfoVoid(Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, System.IO.File.ReadAllText("InfoBot.txt"), replyToMessageId: msg.MessageId);
            Console.WriteLine($"{DateTime.Now} Вызов команды Информация");
        }

        private void MessageCommands(object sender, MessageEventArgs e)
        {
            if (States.TryGetValue(e.Message.Chat.Id, out var state))
            {
                switch (state)
                {
                    case 1:
                        WeatherVoid2(e.Message);
                        break;
                    case 2:
                        ExchangeVoid2(e.Message);
                        break;
                }
            }
        }

        private async void Err(string er, Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, "К сожалению данная функция временно отключена.", replyMarkup: GetStandartButtons());
            Console.WriteLine($"{DateTime.Now} Неудачный вызов команды {er}");
        }

        private void RemoveStates(long userid)
        {
            Console.WriteLine($"{DateTime.Now} Исключение из списка {userid}");
            States.TryRemove(userid, out var _);
        }

        private static IReplyMarkup GetStandartButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{ new KeyboardButton { Text = "/Weather" }, new KeyboardButton { Text = "/Info" }, new KeyboardButton { Text = "/News" }, new KeyboardButton { Text = "/Exchange" }}
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
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Санкт-Петербург" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Таллин" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Пори" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Карлеби" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Нарва" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "Москва" }}
                }
            };
        }
        private static IReplyMarkup GetExchange()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{ new KeyboardButton { Text = "EUR" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "USD" }},
                    new List<KeyboardButton>{ new KeyboardButton { Text = "UAH" }}
                }
            };
        }
    }
}
