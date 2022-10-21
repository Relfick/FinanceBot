using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = FinanceBot.Models.User;

namespace FinanceBot.Services.TgBot;

public static class CommonMessageHandlers
{
    public static async Task<Message> CommonMessageHandler(ITelegramBotClient bot, Message message)
    {
        // var httpClient = _httpClientFactory.CreateClient();
        var httpClient = new HttpClient();
        var tgUserId = message.Chat.Id;
        
        var userWorkMode = await Utility.GetUserWorkMode(httpClient, tgUserId);
        Console.WriteLine($"Workmode: {userWorkMode.ToString()}");
        
        var action = userWorkMode switch
        {
            WorkMode.AddCategory      => CategoryHandlers.AddCategoryHandler(bot, httpClient, message, tgUserId),
            WorkMode.EditCategory     => CategoryHandlers.EditCategoryHandler(bot, httpClient, message, tgUserId),
            WorkMode.RemoveCategory   => CategoryHandlers.RemoveCategoryHandler(bot, httpClient, message, tgUserId),
            _                         => AddExpenseHandler(bot, message)
        };
        
        return await action;
    }

    private static async Task<Message> AddExpenseHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = new HttpClient();
        var tgUser = message.From!;

        var userId = tgUser.Id;
        var r = new Regex(@"(?<name>(\w+\s)+)(?<cost>\d+)\s(?<category>(\w+\s*)+)", RegexOptions.Compiled);
        var m = r.Match(message.Text!);
        if (!m.Success)
        {
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Используйте формат {покупка} {цена} {категория}",
                replyMarkup: new ReplyKeyboardRemove());
        }

        var expenseName = m.Result("${name}").Trim();
        var expenseCost = int.Parse(m.Result("${cost}"));

        var foodCategoryAliases = new List<string> { "food", "еда", "продукты" };
        var clothesCategoryAliases = new List<string> { "clothes", "одежда", "шмот", "шмотки", "шмотье", "крутая одежда" };
        var funCategoryAliases = new List<string> { "fun", "развлечение", "отдых", "кафе и рестораны" };
        var categoryAliases = new List<List<string>> { foodCategoryAliases, clothesCategoryAliases, funCategoryAliases };

        string category = m.Result("${category}");
        int categoryId;
        bool validCategory = false;
        for (categoryId = 0; categoryId < categoryAliases.Count; categoryId++)
        {
            if (categoryAliases[categoryId].Contains(category.ToLower().Trim()))
            {
                validCategory = true;
                break;
            }
        }
        if (!validCategory)
        {
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Несуществующая категория: {category}",
                replyMarkup: new ReplyKeyboardRemove()); 
        }
        
        var expenseCategory = categoryAliases[categoryId][0];

        var expenseDate = DateTime.Now;

        var expense = new Expense(
            userId,
            expenseName,
            expenseCost,
            expenseCategory,
            expenseDate);
        
        var expenseJson = new StringContent(
            JsonSerializer.Serialize(expense),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/Expense", expenseJson);
        string responseMessageText = 
            httpResponseMessage.IsSuccessStatusCode ? 
                "Добавили!" : 
                "Не получилось добавить :(";
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessageText,
            replyMarkup: new ReplyKeyboardRemove());
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
}