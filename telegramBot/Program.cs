using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using static Telegram.Bot.Types.CallbackQuery;
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
                     await  botClient.SendTextMessageAsync(message.Chat,
                        "Приветик. Введи свой город что бы узнать погоду.");
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
                    text: ""
                );
                YesNoButtons(botClient, update, cancellationToken);
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }


        static void Main(string[] args)
        {
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
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
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
            );
            if (update.CallbackQuery.Data == "yes")
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Отлично",
                    cancellationToken: cancellationToken);
                //ВЫЗОВ МЕТОДА С ПОКАЗОМ ПОГОДЫ И СОЗДАНИЕ КНОПОК В КЛАВЕ
            }
            if (update.CallbackQuery.Data == "no")
            {
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Введите другой город",
                    cancellationToken: cancellationToken);
                HandleUpdateAsync(botClient, update, cancellationToken);
                //ВЫЗОВ МЕТОДА С ПОКАЗОМ ПОГОДЫ И СОЗДАНИЕ КНОПОК В КЛАВЕ
            }
            /*botClient.OnCallbackQuery += async (object sc, CallbackQueryEventArgs ev) =>
            {
                var buttonId = ev.CallbackQuery.Data;
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: ev.CallbackQuery.Id,
                    text: $"Вы нажали кнопку {buttonId}"
                );
            };*/

        }
        public static async void TranslateButtons(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            var message = update.Message;
            /*if (message.Text != null && message.Type == MessageType.Text)
            {
                await botClient.SendTextMessageAsync(message.Chat, $"Ваш город: {message.Text}?");
            }
            else
            {
                message = update.Message;
                return;
            }*/
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] // row 1
                {
                    new KeyboardButton("Создать напоминание")
                },
                new[] // row 2
                { 
                    new KeyboardButton("Список активных напоминаний")
                }
            });

        }
    }
}