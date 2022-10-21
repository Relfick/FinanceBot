using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinanceBot.Services.TgBot;

public static class CategoryHandlers
{
    public static async Task<Message> CategoriesCommandHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = new HttpClient();
        var tgUser = message.From!;
        var tgUserId = tgUser.Id;

        var userCategories = await Utility.GetUserCategories(httpClient, tgUserId);
        var hasCategories = userCategories.Count > 0;
        
        return await ShowCategoriesMessage(bot, message.Chat.Id, userCategories, hasCategories);
    }

    private static async Task<Message> ShowCategoriesMessage(ITelegramBotClient bot, long chatId, 
        List<string> userCategories,
        bool hasCategories = true)
    {
        var responseMessage = new StringBuilder();

        InlineKeyboardMarkup inlineKeyboard;
        if (hasCategories)
        {
            responseMessage.Append("Текущие категории:\n\n");
            foreach (string category in userCategories)
            {
                responseMessage.Append($"    {category}\n");
            }
            
            inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Добавить", "add"),
                        InlineKeyboardButton.WithCallbackData("Удалить", "remove"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Изменить", "edit"),
                        InlineKeyboardButton.WithCallbackData("Назад", "back"),
                    },
                });
        }
        else
        {
            responseMessage.Append("Вы еще не добавили категории.\n\nВыберите действие:");
            
            inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Добавить", "add"),
                        InlineKeyboardButton.WithCallbackData("Назад", "back"),
                    },
                });
        }
        
        return await bot.SendTextMessageAsync(
            chatId: chatId,
            text: responseMessage.ToString(),
            replyMarkup: inlineKeyboard,
            allowSendingWithoutReply: false);
    }

    public static async Task<Message> BaseActionCategoryShowInfo(ITelegramBotClient bot, Message message, WorkMode workMode)
    {
        // If workMode does not relate to Category
        if (!new[] { WorkMode.AddCategory, WorkMode.EditCategory, WorkMode.RemoveCategory }.Contains(workMode))
            throw new Exception($"WorkMode {workMode.ToString()} does not relate to Category");
        
        bool success = await Utility.SetWorkMode(message.Chat.Id, workMode);
        if (success)
            Console.WriteLine($"поменяли режим на {workMode.ToString()}");
        else
            Console.WriteLine($"не поменяли режим на {workMode.ToString()} (((");

        string suggest = workMode switch
        {
            WorkMode.AddCategory    => "добавить",
            WorkMode.EditCategory   => "изменить, и новое название через пробел",
            WorkMode.RemoveCategory => "удалить"
        };
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Введите название категории, которую хотите {suggest}",
            replyMarkup: new ReplyKeyboardRemove()
        ); 
    }

    public static async Task<Message> AddCategoryHandler(
        ITelegramBotClient bot, HttpClient httpClient,
        Message message, long tgUserId)
    {
        if (message.Text!.Split(" ").Length > 1)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Название категории должно быть из одного слова!",
                replyMarkup: new ReplyKeyboardRemove()
            );

        var newCategory = new UserExpenseCategory(tgUserId, message.Text!);
        var newCategoryJson = new StringContent(
            JsonSerializer.Serialize(newCategory),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/UserExpenseCategory", newCategoryJson);
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Какая то ошибка...",
                replyMarkup: new ReplyKeyboardRemove()
            );

        bool success = await Utility.SetWorkMode(tgUserId, WorkMode.Default);
        if (!success)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Категорию добавли, а с воркмодом какая то ошибка...",
                replyMarkup: new ReplyKeyboardRemove()
            );
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Добавили категорию и даже поменяли воркмод!",
            replyMarkup: new ReplyKeyboardRemove()
        );
    } 
    
    public static async Task<Message> EditCategoryHandler(
        ITelegramBotClient bot, HttpClient httpClient,
        Message message, long tgUserId)
    {
        var splits = message.Text!.Split(' ', StringSplitOptions.TrimEntries);
        if (splits.Length != 2)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Используйте формат {старая категория} {новая категория}",
                replyMarkup: new ReplyKeyboardRemove()
            ); 
        
        var oldCategory = splits[0];
        var newCategory = splits[1];

        var categoriesDictionary = new Dictionary<string, string>
        {
            { "oldCategory", oldCategory },
            { "newCategory", newCategory }
        };
        
        var contentJson = new StringContent(
            JsonSerializer.Serialize(categoriesDictionary),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PutAsync($"https://localhost:7166/api/UserExpenseCategory/{tgUserId}", contentJson);
        
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"У вас нет категории {oldCategory}",
                replyMarkup: new ReplyKeyboardRemove()
            );
        
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Не удалось изменить категорию...",
                replyMarkup: new ReplyKeyboardRemove()
            );

        bool success = await Utility.SetWorkMode(tgUserId, WorkMode.Default);
        if (!success)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Категорию изменили, а с воркмодом какая то ошибка...",
                replyMarkup: new ReplyKeyboardRemove()
            );
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Изменили категорию и даже поменяли воркмод!",
            replyMarkup: new ReplyKeyboardRemove()
        );
    }
    
    public static async Task<Message> RemoveCategoryHandler(
        ITelegramBotClient bot, HttpClient httpClient,
        Message message, long tgUserId)
    {
        var categoryToRemove = message.Text!;

        var httpResponseMessage = await httpClient.DeleteAsync($"https://localhost:7166/api/UserExpenseCategory/{tgUserId}/{categoryToRemove}");
        
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Не удалось удалить категорию...",
                replyMarkup: new ReplyKeyboardRemove()
            );
        
        bool success = await Utility.SetWorkMode(tgUserId, WorkMode.Default);
        if (!success)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Категорию удалили, а воркмод вернуть не удалось...",
                replyMarkup: new ReplyKeyboardRemove()
            );
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Удалили категорию и даже поменяли воркмод!",
            replyMarkup: new ReplyKeyboardRemove()
        );
    }
    
    public static async Task<Message> BackCategoryHandler(ITelegramBotClient bot, Message message)
    {
        await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выбраное действие: Назад",
            replyMarkup: new ReplyKeyboardRemove()); 
    }
    
    public static async Task<Message> UnknownCategoryHandler(ITelegramBotClient bot, Message message)
    {
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выбраное действие: Неизвестно",
            replyMarkup: new ReplyKeyboardRemove()); 
    }
}