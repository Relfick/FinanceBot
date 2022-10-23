using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using User = FinanceBot.Models.User;

namespace FinanceBot.Services.TgBot;

public static class GlobalHandler
{
    public static async Task BotOnMessageReceived(Message message,ITelegramBotClient botClient)
    {
        Console.WriteLine($"Receive message type: {message.Type}");
        if (message.Type != MessageType.Text)
            return;
        Console.WriteLine($"Message: {message.Text}");
        Console.WriteLine($"UserId: {message.From!.Id}");
        
        var action = message.Text!.Split(' ')[0] switch
        {
            "/start"       => RegisterUserHandler(botClient, message),
            "/categories"  => CategoryHandler.CategoriesCommandHandler(botClient, message),
            "/expenses"    => ExpenseHandler.ExpenseCommandHandler(botClient, message),
            "/help"        => HelpCommandHandler(botClient, message),
            // "/inline"   => SendInlineKeyboard(_botClient, message),
            // "/keyboard" => SendReplyKeyboard(_botClient, message),
            // "/remove"   => RemoveKeyboard(_botClient, message),
            // "/photo"    => SendFile(_botClient, message),
            // "/request"  => RequestContactAndLocation(_botClient, message),
            _              => CommonMessageHandler(botClient, message)
        };
        Message sentMessage = await action;
        Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");
        
        
        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        static async Task<Message> SendInlineKeyboard(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            // Simulate longer running task
            await Task.Delay(500);

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    },
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Choose",
                                                  replyMarkup: inlineKeyboard);
        }
        
        static async Task<Message> SendReplyKeyboard(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { "Добавить", "Удалить" },
                    new KeyboardButton[] { "Изменить", "Назад" },
                })
            {
                ResizeKeyboard = true
            };

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: replyKeyboardMarkup);
        }
        
        static async Task<Message> RemoveKeyboard(ITelegramBotClient bot, Message message)
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Removing keyboard",
                                                  replyMarkup: new ReplyKeyboardRemove());
        }

        static async Task<Message> SendFile(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            const string filePath = @"Files/tux.png";
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

            return await bot.SendPhotoAsync(chatId: message.Chat.Id,
                                            photo: new InputOnlineFile(fileStream, fileName),
                                            caption: "Nice Picture");
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup requestReplyKeyboard = new(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Who or Where are you?",
                                                  replyMarkup: requestReplyKeyboard);
        }

        static async Task<Message> HelpCommandHandler(ITelegramBotClient bot, Message message)
        {
            const string usage = "Введи траты в формате \n{Название} {Стоимость} {Категория}\n\n" +
                                 "Для добавления категорий используйте команду /categories";

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, replyMarkup: new ReplyKeyboardRemove(),
                text: usage);
        }
    }
    
    // Process Inline Keyboard callback data
    // TODO: Move to CategoryHandlers
    public static async Task<Message> BotOnCallbackQueryReceived(CallbackQuery callbackQuery, ITelegramBotClient botClient)
    {
        if (callbackQuery.Message == null)
            return await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                text: $"Received {callbackQuery.Data}");
        
        var message = callbackQuery.Message;
        return await (
            callbackQuery.Data switch
            {
                "add" => CategoryHandler.BaseActionCategoryShowInfo(botClient, message, WorkMode.AddCategory),
                "edit" => CategoryHandler.BaseActionCategoryShowInfo(botClient, message, WorkMode.EditCategory),
                "remove" => CategoryHandler.BaseActionCategoryShowInfo(botClient, message, WorkMode.RemoveCategory),
                "back" => CategoryHandler.BackCategoryHandler(botClient, message),
                _ => CategoryHandler.UnknownCategoryHandler(botClient, message)
            });
    }

    public static async Task<Message> RegisterUserHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = new HttpClient();
        var tgUser = message.From!;

        if (await Utility.UserExists(tgUser, httpClient))
        {
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ты уже в списочке, не переживай.",
                replyMarkup: new ReplyKeyboardRemove());
        }
        
        var newUser = new User(tgUser.Id, tgUser.FirstName, tgUser.Username ?? "");
        var newUserJson = new StringContent(
            JsonSerializer.Serialize(newUser),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/BotUser", newUserJson);
        string responseMessageText = 
            httpResponseMessage.IsSuccessStatusCode ? 
                "Привет! Ты успешно зарегистрирован!\n\n" +
                "Доступные пока команды: \n\n" +
                "/help" : 
                "Error";

        var userWorkMode = new UserWorkMode(tgUser.Id, WorkMode.Default);
        var newUserWorkMode = new StringContent(
            JsonSerializer.Serialize(userWorkMode),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/UserWorkmode", newUserWorkMode);
        if (httpResponseMessage.IsSuccessStatusCode)
            Console.WriteLine("userWorkmode successfully added");
        else
            Console.WriteLine("userWorkmode did not added. Error!");

        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessageText,
            replyMarkup: new ReplyKeyboardRemove());
    }
    
    public static async Task<Message> CommonMessageHandler(ITelegramBotClient bot, Message message)
    {
        // var httpClient = _httpClientFactory.CreateClient();
        var httpClient = new HttpClient();
        var tgUserId = message.Chat.Id;

        var userWorkMode = await Utility.GetUserWorkMode(httpClient, tgUserId);
        Console.WriteLine($"Workmode: {userWorkMode.ToString()}");

        var action = userWorkMode switch
        {
            WorkMode.AddCategory => CategoryHandler.AddCategoryHandler(bot, httpClient, message, tgUserId),
            WorkMode.EditCategory => CategoryHandler.EditCategoryHandler(bot, httpClient, message, tgUserId),
            WorkMode.RemoveCategory => CategoryHandler.RemoveCategoryHandler(bot, httpClient, message, tgUserId),
            _ => ExpenseHandler.AddExpenseHandler(bot, message)
        };

        return await action;
    }

}