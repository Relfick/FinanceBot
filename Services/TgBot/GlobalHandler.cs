using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

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
            "/start"       => CommonMessageHandlers.RegisterUserHandler(botClient, message),
            "/categories"  => CategoryHandlers.CategoriesCommandHandler(botClient, message),
            "/help"        => HelpCommandHandler(botClient, message),
            // "/inline"   => SendInlineKeyboard(_botClient, message),
            // "/keyboard" => SendReplyKeyboard(_botClient, message),
            // "/remove"   => RemoveKeyboard(_botClient, message),
            // "/photo"    => SendFile(_botClient, message),
            // "/request"  => RequestContactAndLocation(_botClient, message),
            _              => CommonMessageHandlers.CommonMessageHandler(botClient, message)
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
            const string usage = "Введи траты в формате {Название} {Стоимость} {Категория}\n\n" +
                                 "Доступные категории: {еда}, {одежда}, {развлечения}\n" +
                                 "(на самом деле их больше, но ты о них не узнаешь\n\n" +
                                 "Позже появится возможность редактирования категорий.";
            
            // const string usage = "Usage:\n" +
            //                      "/inline   - send inline keyboard\n" +
            //                      "/keyboard - send custom keyboard\n" +
            //                      "/remove   - remove custom keyboard\n" +
            //                      "/photo    - send a photo\n" +
            //                      "/request  - request location or contact";

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: usage,
                                                  replyMarkup: new ReplyKeyboardRemove());
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
                "add" => CategoryHandlers.BaseActionCategoryShowInfo(botClient, message, WorkMode.AddCategory),
                "edit" => CategoryHandlers.BaseActionCategoryShowInfo(botClient, message, WorkMode.EditCategory),
                "remove" => CategoryHandlers.BaseActionCategoryShowInfo(botClient, message, WorkMode.RemoveCategory),
                "back" => CategoryHandlers.BackCategoryHandler(botClient, message),
                _ => CategoryHandlers.UnknownCategoryHandler(botClient, message)
            });
    }
}