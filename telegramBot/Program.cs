﻿using System;
using System.Net.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using telegramBot.Config;
using Update = Telegram.Bot.Types.Update;
using UpdateType = Telegram.Bot.Types.Enums.UpdateType;



namespace telegramBot
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            MongoClient Client;
            MongoClientSettings Settings;
            var config = new Config<MainConfig>().Entries;

#if !DEBUG
            Settings = new()
            {
                Server = new MongoServerAddress(config.Host, config.Port),
                Credential = MongoCredential.CreateCredential(config.DbName,
                    config.AuthorizationName, config.AuthorizationPassword)
            };
            Client = new(Settings);
            _mongoDatabase = Client.GetDatabase("WeatherUsers");
#endif
         
#if DEBUG
            Client = new("mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&directConnection=true&ssl=false");
            _mongoDatabase = Client.GetDatabase("WeatherUsers");
#endif

            Console.WriteLine("Активизирован бот " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );


            await Task.Delay(-1);
        }
        static ITelegramBotClient bot = new TelegramBotClient(new Config<MainConfig>().Entries.Token);
        private static IMongoDatabase _mongoDatabase;

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            var message = update.Message; 
            if (update.Type == UpdateType.Message)
            {
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                         "Приветик");  
                    await botClient.SendTextMessageAsync(message.Chat,
                         "👋🏻"); 
                         botClient.SendTextMessageAsync(message.Chat,
                        "Что бы узнать погоду, введите название города на англиском языке.\nПример: Minsk");
                         
                         var userCollectionAll = _mongoDatabase.GetCollection<User>("Users");
                         
                         var updateInf = Builders<User>.Update.Set("Name", message.Chat.FirstName)
                                 .Set("NickName", message.Chat.Username)
                                 .Set("City", "")
                                 .Set("Status", "ChoiseCity");

                         userCollectionAll.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf, new UpdateOptions { IsUpsert = true });
                         message = update.Message;
                         return;
                }
                var userCollectionCitys = _mongoDatabase.GetCollection<User>("Users");
                var user2 = (await userCollectionCitys.Find(u => u.TelegramId == message.Chat.Id && u.Status=="ChoiseCity").FirstOrDefaultAsync());

                string pattern = "[a-zA-Z]+";
                if (user2 is not null && user2.Status=="ChoiseCity" && Regex.IsMatch(message.Text, pattern))
                {
                    var statusUpdate = Builders<User>.Update
                        .Set(f => f.City, update.Message.Text)
                        .Set(f => f.Status, "CitySelected");
                    
                    userCollectionCitys.UpdateOne(u => u.TelegramId == message.Chat.Id && u.Status=="ChoiseCity", statusUpdate, new UpdateOptions { IsUpsert = true });
                }
                else if(user2 is not null && user2.Status=="ChoiseCity" && !Regex.IsMatch(message.Text, pattern))
                {
                    botClient.SendTextMessageAsync(message.Chat, 
                        "Введите название города на англиском языке\nПример: Minsk");
                    return;
                }

                if (message.Text.ToLower() == "/about")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Бот погода станет верным помощником для Вас и будет каждый день уведомлять о погоде за окном.");
                    return;
                }

                if (message.Text.ToLower() == "/help")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Help");
                    return;
                }

                if (update.Type != UpdateType.Message)
                    return;

                message = update.Message;

                if (message == null || message.Type != MessageType.Text)
                    return;

                Console.WriteLine($"Received a text message in chat {message.Chat.Id}.");
                botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: " "
                );
                YesNoButtons(botClient, update, cancellationToken);
            }
        }
        
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        public static async void YesNoButtons(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var message = update.Message;
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("Посмотреть погоду")
                },
                new[]
                { 
                    new KeyboardButton("Сменить город")
                }
            });
            await botClient.SendTextMessageAsync(
                chatId: message.Chat,
                text: "Выберите действие",
                replyMarkup: keyboard
            );
            if (update.Message.Text=="Посмотреть погоду")
            {
                Weather(botClient, update, cancellationToken);
            }
            if (update.Message.Text=="Сменить город")
            {
                var userCollectionCity = _mongoDatabase.GetCollection<User>("Users");
                         
                var updateInf = Builders<User>.Update
                    .Set("City", "")
                    .Set("Status", "ChoiseCity");

                userCollectionCity.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf, new UpdateOptions { IsUpsert = true });
            }
        }
        public static async void Weather(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var message = update.Message;
            var userCollectionCitys = _mongoDatabase.GetCollection<User>("Users");
            var user = (await userCollectionCitys.Find(u => u.TelegramId == message.Chat.Id && u.City!="").FirstOrDefaultAsync());
            var cityTest = user.City;
                var apiKey = new Config<MainConfig>().Entries.ApiToken;
                var url = $"http://api.openweathermap.org/data/2.5/weather?q={cityTest}&appid={apiKey}&units=metric";

                using var httpClient = new HttpClient();
                try
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (JsonSerializer.Deserialize<JsonDocument>(responseBody) is not { } responseJson) return;
                    var main = responseJson.RootElement.GetProperty("main");
                    var degrees = main.GetProperty("temp");
                    var feel = main.GetProperty("feels_like");
                    var min = main.GetProperty("temp_min");
                    var max = main.GetProperty("temp_max");
                    var humidity = main.GetProperty("humidity");
                    await  botClient.SendTextMessageAsync(message.Chat,
                        "⛅️Погода на сегодня⛅️");
                    await botClient.SendTextMessageAsync(message.Chat,
                        $"Город: {cityTest}\nГрадусы: {degrees}°C\nОщущается как: {feel}°C\nМинимальная температура: {min}°C\nМаксимальная температура: {max}°C\nОсадки: {humidity}%");
                    string tgmessage = "";
                    if (degrees.ToString() == "")
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Такого города нет в системе, попробуйте сменить город и запросить погоду заново.");
                    }

                if (humidity.GetInt32() >= 70)
                    {
                        tgmessage+="☂️Возможны осадки, возьми с собой зонтик☂️\n";
                    }

                    if (degrees.GetDouble()-feel.GetDouble()>5)
                    {
                        tgmessage+="💨Ветренно, одевайтесь теплее💨\n";
                    }
                    
                    if (feel.GetDouble()-degrees.GetDouble()>5 || degrees.GetDouble()>27)
                    {
                        tgmessage+="☀️Жарко, не забудь кепку☀️\n";
                    }

                    if (degrees.GetDouble()<15 && degrees.GetDouble()>10)
                    {
                        tgmessage+="🧥Прохладно, возьми куртку🧥\n";
                    }

                    if (degrees.GetDouble()<-15)
                    {
                        tgmessage+="🧣Холодно, не забудь шарфик🧣\n";
                    }

                    if (tgmessage!="")
                    {
                        await botClient.SendTextMessageAsync(message.Chat, tgmessage);
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"An error occurred: {e.Message}");
                    await botClient.SendTextMessageAsync(message.Chat, "Такого города нет в системе, проверьте пожалуйста правильность написания и смените город ещё раз.");
                }
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        }
    }
}