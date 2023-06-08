using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bots.Types;
using static Telegram.Bot.Types.CallbackQuery;
using InlineKeyboardMarkup = Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup;
using Message = Telegram.Bot.Types.Message;
using Update = Telegram.Bot.Types.Update;
using UpdateType = Telegram.Bot.Types.Enums.UpdateType;



namespace telegramBot
{
    class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("5854774014:AAGf6H0PwyQTjOAiTJ3noekH3WKs2l1_kRI");
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
                         botClient.SendTextMessageAsync(message.Chat,
                        "Введи свой город на англиском языке что бы узнать погоду.");
                         message = update.Message;
                         string pattern = "[a-zA-Z]+";
                         if (!Regex.IsMatch(message.Text, pattern))
                         {
                             botClient.SendTextMessageAsync(message.Chat,
                                 "Введите название города на англиском языке с большой буквы\nПример: Minsk");
                             return;
                         }
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
            public ulong TelegramId { get; set; }
            public string Name { get; set; }
            public string NickName { get; set; }
            public string Sity { get; set; }
        }

        public class Information
        {
            public string coord { get; set; }
            public string weather { get; set; }
            public string visibility { get; set; }
            public string wind { get; set; }
            public string clouds { get; set; }
        }
        static void Main(string[] args)
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("WeatherUsers");
            var userCollection = database.GetCollection<User>("Users");
            /*var user = new User
            {
                TelegramId = 123456789,
                Name = "John Doe",
                NickName = "johndoe",
                Sity = "New York"
            };

            User.InsertOne(user);*/
            
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

        public static async void YesNoButtons(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var message = update.Message;
            await botClient.SendTextMessageAsync(message.Chat, $"Ваш город: {message.Text}");
            
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] // row 1
                {
                    new KeyboardButton("Посмотреть погоду")
                },
                new[] // row 1
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
                //await  botClient.SendTextMessageAsync(message.Chat,
                  //  "город указан верно");
                Weather(botClient, update, cancellationToken);
            }
            /*var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да", "yes")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Нет", "no")
                }
            });
            await botClient.SendTextMessageAsync(
                chatId: message.Chat,
                text: $"Ваш город: {message.Text}?",
                replyMarkup: inlineKeyboard
            );*/
            
            /*botClient.OnCallbackQuery += async (object sc, CallbackQueryEventArgs ev) =>
            {
                var buttonId = ev.CallbackQuery.Data;
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: ev.CallbackQuery.Id,
                    text: $"Вы нажали кнопку {buttonId}"
                );
            };*/

        }
        public static async void Weather(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var message = update.Message;
            /*var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] // row 1
                {
                    new KeyboardButton("Узнать погоду")
                }
            });
            if (update.Message.Text=="Узнать погоду")
            {*/
                await  botClient.SendTextMessageAsync(message.Chat,
                                    "Погода на сегодня:");
                var city = "London";
                var apiKey = "60006c3bff1a26c86b0409860981b5b6";
                var url = $"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

                using var httpClient = new HttpClient();
                try
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (JsonSerializer.Deserialize<JsonDocument>(responseBody) is not { } responseJson) return;
                    var main = responseJson.RootElement.GetProperty("main");
                    var degrees = main.GetProperty("temp");
                    await botClient.SendTextMessageAsync(message.Chat, $"weather {degrees}");
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"An error occurred: {e.Message}");
                }
            //}
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        }
    }
}