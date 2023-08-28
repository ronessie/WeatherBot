using System.Net;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using telegramBot.Config;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Timer = System.Timers.Timer;
using Update = Telegram.Bot.Types.Update;
using UpdateType = Telegram.Bot.Types.Enums.UpdateType;



namespace telegramBot
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Timer timer = new Timer(60000);
            timer.Elapsed += TimerElapsed;
            timer.Start();

            MongoClient Client;
            MongoClientSettings Settings;
            var config = new Config<MainConfig>().Entries;

#if !DEBUG
            Settings = new()
            {
                Server = new MongoServerAddress(config.Host, config.Port),
                Credential = MongoCredential.CreateCredential(config.AuthDb,
                    config.AuthorizationName, config.AuthorizationPassword)
            };
            Client = new(Settings);
            _mongoDatabase = Client.GetDatabase("WeatherUsers");
#endif

#if DEBUG
            Client = new(
                "mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&directConnection=true&ssl=false");
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
            timer.Stop();
        }

        static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var update = new Update();
            //var botClient = new TelegramBotClient("5854774014:AAGf6H0PwyQTjOAiTJ3noekH3WKs2l1_kRI");   //ТЕСТОВЫЙ ТОКЕН
            var botClient = new TelegramBotClient("5991659123:AAHSfX4vBRKa6abDFzPFXScmyTBN7yOBQog");
            DateTime currentTime = DateTime.Now;
            if (currentTime.Hour == 10 && currentTime.Minute == 00)
            {
                Spam(botClient, update);
            }
        }

        static ITelegramBotClient bot = new TelegramBotClient(new Config<MainConfig>().Entries.Token);
        private static IMongoDatabase _mongoDatabase;

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var userCollection = _mongoDatabase.GetCollection<User>("Users");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            var message = update.Message;
            if (update.Type == UpdateType.Message)
            {
                if (message.Text.ToLower() == "/spam" && message.Chat.Id == 975333201)
                {
                    var updateInf = Builders<User>.Update.Set("Spam", "MessageWrite");
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Введите текст рассылки");
                    userCollection.UpdateOne(u => u.TelegramId == 975333201, updateInf,
                        new UpdateOptions { IsUpsert = true });
                    return;
                }

                string messageSpam = "📬Рассылка📬\n";
                var user = (await userCollection.Find(u => u.TelegramId == 975333201 && u.Spam == "MessageWrite")
                    .FirstOrDefaultAsync());
                if (user is not null && user.Spam == "MessageWrite")
                {
                    messageSpam += update.Message.Text;
                    var spamUpdate = Builders<User>.Update
                        .Set(f => f.Spam, "MessageReady");

                    userCollection.UpdateOne(u => u.TelegramId == 975333201 && u.Spam == "MessageWrite",
                        spamUpdate, new UpdateOptions { IsUpsert = true });

                    var userList = await userCollection.Find(u => true).ToListAsync();

                    for (int i = 0; i < userList.Count; i++)
                    {
                        await botClient.SendTextMessageAsync(userList[i].TelegramId,
                            messageSpam);
                    }
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Рассылка успешно создана\n");
                }

                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Приветик");
                    await botClient.SendTextMessageAsync(message.Chat,
                        "👋🏻");
                    botClient.SendTextMessageAsync(message.Chat,
                        "Что бы узнать погоду, введите название города на английском языке.\nПример: Minsk");


                    var updateInf = Builders<User>.Update.Set("Name", message.Chat.FirstName)
                        .Set("NickName", message.Chat.Username)
                        .Set("City", "")
                        .Set("Status", "ChoiseCity");
                    userCollection.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf,
                        new UpdateOptions { IsUpsert = true });
                    message = update.Message;
                    return;
                }

                var user2 = (await userCollection
                    .Find(u => u.TelegramId == message.Chat.Id && u.Status == "ChoiseCity").FirstOrDefaultAsync());

                string pattern = "[a-zA-Z]+";
                if (user2 is not null && user2.Status == "ChoiseCity" && Regex.IsMatch(message.Text, pattern))
                {
                    var statusUpdate = Builders<User>.Update
                        .Set(f => f.City, update.Message.Text)
                        .Set(f => f.Status, "CitySelected");

                    userCollection.UpdateOne(u => u.TelegramId == message.Chat.Id && u.Status == "ChoiseCity", 
                        statusUpdate, new UpdateOptions { IsUpsert = true });
                    message = update.Message;
                    var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[]
                    {
                        new KeyboardButton("⛅️Посмотреть погоду⛅️")
                    },
                    new[]
                    {
                        new KeyboardButton("🏠Сменить город🏠")
                    },
                    new[]
                    {
                        new KeyboardButton("🚪Отказаться от рассылки🚪")
                    }
                });
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat,
                    text: "Город успешно установлен. Выберите действие.",
                    replyMarkup: keyboard
                );
                }
                if (update.Message.Text == "⛅️Посмотреть погоду⛅️")
                {
                    Weather(botClient, update);
                }
                if (update.Message.Text == "🚪Отказаться от рассылки🚪")
                {

                    var updateInf = Builders<User>.Update.Set("Status", "NoSpam");

                    userCollection.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf,
                        new UpdateOptions { IsUpsert = true });

                    var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                new KeyboardButton("⛅️Посмотреть погоду⛅️")
                            },
                            new[]
                            {
                                new KeyboardButton("🏠Сменить город🏠")
                            },
                            new[]
                            {
                                new KeyboardButton("🚪Подписаться на рассылку🚪")
                            }
                        });
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat,
                        text: "Вы успешно отписались от рассылки. Выберите действие.",
                        replyMarkup: keyboard
                    );
                }
                if (update.Message.Text == "🚪Подписаться на рассылку🚪")
                {

                    var updateInf = Builders<User>.Update.Set("Status", "CitySelected");

                    userCollection.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf,
                        new UpdateOptions { IsUpsert = true });
                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new KeyboardButton("⛅️Посмотреть погоду⛅️")
                        },
                        new[]
                        {
                            new KeyboardButton("🏠Сменить город🏠")
                        },
                        new[]
                        {
                            new KeyboardButton("🚪Отказаться от рассылки🚪")
                        }
                    });
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat,
                        text: "Вы успешно подписались на рассылку. Выберите действие.",
                        replyMarkup: keyboard
                    );
                }

                if (update.Message.Text == "🏠Сменить город🏠")
                {
                    var updateInf = Builders<User>.Update
                        .Set("City", "")
                        .Set("Status", "ChoiseCity");

                    userCollection.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf,
                        new UpdateOptions { IsUpsert = true });

                    botClient.SendTextMessageAsync(message.Chat,
                        "Введите название города на английском языке\nПример: Minsk");
                }

                if (message.Text.ToLower() == "/about")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Бот погода станет верным помощником для Вас и будет каждый день уведомлять о погоде за окном. Все данные взяты с сайта OpenWeather, за достоверность информации автор ответственности не несёт.");
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
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }


        public static async void Weather(ITelegramBotClient botClient, Update update)
        {
            var message = update.Message;
            var userCollectionCitys = _mongoDatabase.GetCollection<User>("Users");
            var user = (await userCollectionCitys.Find(u => u.TelegramId == message.Chat.Id && u.City != "")
                .FirstOrDefaultAsync());
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
                await botClient.SendTextMessageAsync(message.Chat,
                    "⛅️Погода на сегодня⛅️");
                if (min.ToString() == max.ToString())
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        $"Город: {cityTest}\nГрадусы: {degrees}°C\nОщущается как: {feel}°C\nОсадки: {humidity}%");
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        $"Город: {cityTest}\nГрадусы: {degrees}°C\nОщущается как: {feel}°C\nМинимальная температура: {min}°C\nМаксимальная температура: {max}°C\nОсадки: {humidity}%");
                }

                string tgmessage = "";
                if (degrees.ToString() == "")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Такого города нет в системе, попробуйте сменить город и запросить погоду заново.");
                }

                if (humidity.GetInt32() >= 70)
                {
                    tgmessage += "☂️Возможны осадки, возьми с собой зонтик☂️\n";
                }

                if (degrees.GetDouble() - feel.GetDouble() > 5)
                {
                    tgmessage += "💨Ветренно, одевайся теплее💨\n";
                }

                if (feel.GetDouble() - degrees.GetDouble() > 5 || degrees.GetDouble() > 27)
                {
                    tgmessage += "☀️Жарко, не забудь кепку☀️\n";
                }

                if (degrees.GetDouble() < 15 && degrees.GetDouble() > 10)
                {
                    tgmessage += "🧥Прохладно, возьми куртку🧥\n";
                }

                if (degrees.GetDouble() < -15)
                {
                    tgmessage += "🧣Холодно, не забудь шарфик🧣\n";
                }

                if (tgmessage != "")
                {
                    await botClient.SendTextMessageAsync(message.Chat, tgmessage);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                await botClient.SendTextMessageAsync(message.Chat,
                    "Такого города нет в системе, проверьте правильность написания");
                var userCollectionFalseCity = _mongoDatabase.GetCollection<User>("Users");
                var updateInf = Builders<User>.Update
                    .Set("City", "")
                    .Set("Status", "ChoiseCity");

                userCollectionFalseCity.UpdateOne(u => u.TelegramId == message.Chat.Id, updateInf,
                    new UpdateOptions { IsUpsert = true });

                botClient.SendTextMessageAsync(message.Chat,
                    "Введите название города на английском языке\nПример: Minsk");
            }

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        }

        public static async void Spam(ITelegramBotClient botClient, Update update)
        {
            var collection = _mongoDatabase.GetCollection<User>("Users");
            var userList = await collection.Find(u => true && u.Status != "NoSpam").ToListAsync();
            
            for (int i = 0; i < userList.Count; i++)
            {
                var user = userList[i];
                var cityTest = user.City;
                if (user is not null && user.Status == "CitySelected")
                {
                    var apiKey = new Config<MainConfig>().Entries.ApiToken;
                    var url =
                        $"http://api.openweathermap.org/data/2.5/weather?q={cityTest}&appid={apiKey}&units=metric";

                        using var httpClient = new HttpClient();
                        var response = await httpClient.GetAsync(url);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            Console.WriteLine("id "+ userList[i].TelegramId + " status "+ response.StatusCode + " cityTest " + cityTest);
                            await botClient.SendTextMessageAsync(user.TelegramId,"Такого города нет в системе, проверьте пожалуйста правильность написания и смените город ещё раз.");
                            continue;
                        }
                        var responseBody = await response.Content.ReadAsStringAsync();
                        if (JsonSerializer.Deserialize<JsonDocument>(responseBody) is not { } responseJson) return;
                        var main = responseJson.RootElement.GetProperty("main");
                        var degrees = main.GetProperty("temp");
                        var feel = main.GetProperty("feels_like");
                        var min = main.GetProperty("temp_min");
                        var max = main.GetProperty("temp_max");
                        var humidity = main.GetProperty("humidity");
                        if (degrees.ToString() == "")
                        {
                            await botClient.SendTextMessageAsync(user.TelegramId,
                                "Для получения рассылки правильно введите город.");
                        }

                        await botClient.SendTextMessageAsync(user.TelegramId,
                            "⛅️Погода на сегодня⛅️");
                        if (min.ToString() == max.ToString())
                        {
                            await botClient.SendTextMessageAsync(user.TelegramId,
                                $"Город: {cityTest}\nГрадусы: {degrees}°C\nОщущается как: {feel}°C\nОсадки: {humidity}%");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(user.TelegramId,
                                $"Город: {cityTest}\nГрадусы: {degrees}°C\nОщущается как: {feel}°C\nМинимальная температура: {min}°C\nМаксимальная температура: {max}°C\nОсадки: {humidity}%");
                        }

                        string tgmessage = "";
                        if (humidity.GetInt32() >= 70)
                        {
                            tgmessage += "☂️Возможны осадки, возьми с собой зонтик☂️\n";
                        }

                        if (degrees.GetDouble() - feel.GetDouble() > 5)
                        {
                            tgmessage += "💨Ветренно, одевайся теплее💨\n";
                        }

                        if (feel.GetDouble() - degrees.GetDouble() > 5 || degrees.GetDouble() > 27)
                        {
                            tgmessage += "☀️Жарко, не забудь кепку☀️\n";
                        }

                        if (degrees.GetDouble() < 15 && degrees.GetDouble() > 10)
                        {
                            tgmessage += "🧥Прохладно, возьми куртку🧥\n";
                        }

                        if (degrees.GetDouble() < -15)
                        {
                            tgmessage += "🧣Холодно, не забудь шарфик🧣\n";
                        }

                        if (tgmessage != "")
                        {
                            await botClient.SendTextMessageAsync(user.TelegramId, tgmessage);
                        }
                        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
                }
            }
        }
    }
}