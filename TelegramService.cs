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
using System.IO;

namespace TelegramBot
{
    public class TelegramService
    {
        private string token { get; set; }
        private TelegramBotClient client;
        private NewsClient newsClient;
        private string WToken;
        private ConcurrentDictionary<long, int> States = new();
        private ConcurrentDictionary<long, int> rand = new();
        private readonly HttpClient httpClient = new HttpClient();
        string[] Statuses = new string[5];

        public TelegramService(Settings settings, Statuses statuses)
        {
            token = settings.BotToken;
            WToken = settings.WeatherToken;
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
            Console.WriteLine($"{DateTime.Now} Остановка работы");
            client.StopReceiving();
            Console.ReadKey();
        }

        private async void MessageStatusChecker(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine($"{DateTime.Now} {msg.Chat.Id}: {msg.Text}");
            if (!States.ContainsKey(msg.Chat.Id))
                switch (msg.Text)
                {
                    case "Старт":
                        if (Statuses[0] == "true")
                        {
                            StartVoid(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "Погода":
                        if (Statuses[1] == "true")
                        {
                            WeatherVoid1(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "Новости":
                        if (Statuses[2] == "true")
                        {
                            News(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "Случайное число":
                        if (Statuses[3] == "true")
                        {
                            RandomStatementVoid1(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
                        }
                        break;
                    case "Информация":
                        if (Statuses[4] == "true")
                        {
                            InfoVoid(msg);
                        }
                        else
                        {
                            Err(msg.Text, msg);
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

        private async void RandomStatementVoid1(Message msg)
        {
            await client.SendTextMessageAsync(msg.Chat.Id, "Введите минимальное число:", replyToMessageId: msg.MessageId, replyMarkup: GetNoButtons());
            Console.WriteLine($"{DateTime.Now} Вызов команды Случайное число");
            States.TryAdd(msg.Chat.Id, 2);
            Console.WriteLine($"{DateTime.Now} {msg.Chat.Id} добавлен в список статусов со статусом 2");
        }

        private async void RandomStatementVoid2(Message msg)
        {
            Console.WriteLine($"{DateTime.Now} Вызов функции определения первого числа");
            if (!int.TryParse(msg.Text, out var result))
            {
                await client.SendTextMessageAsync(msg.Chat.Id, "Невозможно преобразовать число.", replyToMessageId: msg.MessageId);
                Console.WriteLine($"{DateTime.Now} Невозможно преобразовать число.");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} Удачное преобразование");
                RemoveStates(msg.Chat.Id);
                States.TryAdd(msg.Chat.Id, 3);
                await client.SendTextMessageAsync(msg.Chat.Id, "Введите максимальное число.");
                rand.TryAdd(msg.Chat.Id, Convert.ToInt32(msg.Text));
            }

        }
        private async void RandomStatementVoid3(Message msg)
        {
            Console.WriteLine($"{DateTime.Now} Вызов функции определения второго числа");
            if (!int.TryParse(msg.Text, out var result))
            {
                await client.SendTextMessageAsync(msg.Chat.Id, "Невозможно преобразовать число.", replyToMessageId: msg.MessageId);
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} Удачное преобразование");
                if (Convert.ToInt32(msg.Text) >= rand[msg.Chat.Id])
                {
                    RemoveStates(msg.Chat.Id);
                    Random r = new();
                    await client.SendTextMessageAsync(msg.Chat.Id, $"Какая удача! Ваше число: {r.Next(rand[msg.Chat.Id], Convert.ToInt32(msg.Text))}", replyMarkup: GetStandartButtons());
                    rand.TryRemove(msg.Chat.Id, out var _);
                    Console.WriteLine($"{DateTime.Now} Вывод случайного числа");
                }
                else { await client.SendTextMessageAsync(msg.Chat.Id, "Максимальное число не может быть меньше минимального.", replyToMessageId: msg.MessageId); Console.WriteLine("Максимальное число не может быть меньше минимального."); }
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
                        RandomStatementVoid2(e.Message);
                        break;
                    case 3:
                        RandomStatementVoid3(e.Message);
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
