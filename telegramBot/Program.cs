using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Update = Telegram.Bot.Types.Update;
using UpdateType = Telegram.Bot.Types.Enums.UpdateType;



namespace telegramBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            var client = new MongoClient("mongodb://localhost:27017");
            _mongoDatabase = client.GetDatabase("WeatherUsers");
         
            //var users = (await userCollection.FindAsync(u => u.UserId == telegramUser.Id)).ToList();
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
            Console.ReadLine();
        }
        static ITelegramBotClient bot = new TelegramBotClient("5854774014:AAGf6H0PwyQTjOAiTJ3noekH3WKs2l1_kRI");
        private static IMongoDatabase _mongoDatabase;

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            var message = update.Message; 
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                         "Приветик");  
                    await botClient.SendTextMessageAsync(message.Chat,
                         "👋🏻"); 
                         botClient.SendTextMessageAsync(message.Chat,
                        "Что бы узнать погоду введите название города на англиском языке с большой буквы.\nПример: Minsk");
                         User user = new User()
                         {
                             Id = new ObjectId(),
                             TelegramId = message.Chat.Id,
                             Name = message.Chat.FirstName,
                             NickName = message.Chat.Username,
                             City = "",
                             Status = "start"
                         };
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
                        "Введите название города на англиском языке с большой буквы\nПример: Minsk");
                    return;
                }

                if (message.Text.ToLower() == "/about")
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Бот погода станет верным помощником для Вас и будет каждый день уведомлять Вас о погоде за окном.");
                    return;
                }

                if (message.Text.ToLower() == "/help")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "help");
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

        public class User
        {
            public ObjectId Id { get; set; }
            public long TelegramId { get; set; }
            public string Name { get; set; }
            public string NickName { get; set; }
            public string City { get; set; }
            
            public string Status { get; set; }
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
                
                var user2 = (await userCollectionCity.Find(u => u.TelegramId == message.Chat.Id && u.Status=="ChoiseCity").FirstOrDefaultAsync());
                if (update.Message.Text=="Сменить город" && update.Message.Text=="Выберите действие")
                {
                    if (user2 is not null && user2.Status=="ChoiseCity")
                    { 
                        var statusUpdate = Builders<User>.Update
                            .Set(f => f.City, update.Message.Text)
                            .Set(f => f.Status, "CitySelected");
                        userCollectionCity.UpdateOne(u => u.TelegramId == message.Chat.Id && u.Status=="ChoiseCity", statusUpdate, new UpdateOptions { IsUpsert = true });
                    }
                }
                else
                {
                    return;
                }
            }
        }
        public static async void Weather(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var message = update.Message;
            await  botClient.SendTextMessageAsync(message.Chat,
                                    "⛅️Погода на сегодня⛅️"); 
            var userCollectionCitys = _mongoDatabase.GetCollection<User>("Users");
            var user = (await userCollectionCitys.Find(u => u.TelegramId == message.Chat.Id && u.City!="").FirstOrDefaultAsync());
            var cityTest = user.City;
                var apiKey = "60006c3bff1a26c86b0409860981b5b6";
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
                    await botClient.SendTextMessageAsync(message.Chat, $"Город: {cityTest}\nГрадусы: {degrees}°C\nОщущается как: {feel}°C\nМинимальная температура: {min}°C\nМаксимальная температура: {max}°C\nОсадки: {humidity}%");
                    string tgmessage = "";
                    if (humidity.GetInt32() >= 70)
                    {
                        tgmessage+="☂️Возможны осадки, возьми с собой зонтик☂️\n";
                    }

                    if (degrees.GetDouble()-feel.GetDouble()>5)
                    {
                        tgmessage+="💨Ветренно, одевайтесь теплее💨\n";
                    }
                    
                    if (feel.GetDouble()-degrees.GetDouble()>5)
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
                }
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        }
    }
}