using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinanceBot.Services.TgBot;

public class CategoryHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly Message _message;
    private readonly HttpClient _httpClient;
    private readonly long _tgUserId;
    
    public CategoryHandler(ITelegramBotClient bot, Message message, HttpClient httpClient)
    {
        this._bot = bot;
        this._message = message;
        this._httpClient = httpClient;
        this._tgUserId = message.Chat.Id;
    }
    
    public async Task<Message> CategoriesCommandHandler()
    {
        var userCategories = await Utility.GetUserCategories(_httpClient, _tgUserId);
        var hasCategories = userCategories.Count > 0;
        
        return await ShowCategoriesMessage(userCategories, hasCategories);
    }

    private async Task<Message> ShowCategoriesMessage(List<string> userCategories, bool hasCategories = true)
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
        
        return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: inlineKeyboard,
            allowSendingWithoutReply: false, text: responseMessage.ToString());
    }

    public async Task<Message> BaseActionCategoryShowInfo(WorkMode workMode)
    {
        string suggest = workMode switch
        {
            WorkMode.AddCategory    => "добавить",
            WorkMode.EditCategory   => "изменить, и новое название через пробел",
            WorkMode.RemoveCategory => "удалить",
            _                       => throw new Exception($"WorkMode {workMode.ToString()} does not relate to Category")
        };
        
        bool success = await Utility.SetWorkMode(_tgUserId, workMode);
        Console.WriteLine(success
            ? $"поменяли режим на {workMode.ToString()}"
            : $"не поменяли режим на {workMode.ToString()} (((");
        
        return await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: $"Введите название категории, которую хотите {suggest}"); 
    }

    // TODO: check if new category already exists
    public async Task<Message> AddCategoryHandler()
    {
        string messageText = _message.Text ?? throw new Exception("Null Message.Text in CategoryHandler");
        messageText = messageText.Trim().ToLower();
        if (messageText.Split(" ").Length > 1)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Название категории должно быть из одного слова!");

        var infoMessage = await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: "Пытаемся добавить...");
        Console.WriteLine(infoMessage.MessageId);
        
        var newCategory = new UserExpenseCategory(_tgUserId, messageText);
        var newCategoryJson = new StringContent(
            JsonSerializer.Serialize(newCategory),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await _httpClient.PostAsync("https://localhost:7166/api/UserExpenseCategory", newCategoryJson);
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId, 
                text: "Не удалось добавить категорию...");

        bool success = await Utility.SetWorkMode(_tgUserId, WorkMode.Default);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Категорию добавили, а с воркмодом какая то ошибка...");
        
        return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
            text: "Добавили категорию!");
    } 
    
    public async Task<Message> EditCategoryHandler()
    {
        string messageText = _message.Text ?? throw new Exception("Null Message.Text in CategoryHandler");
        messageText = messageText.Trim().ToLower();
        
        var splits = messageText.Split(' ', StringSplitOptions.TrimEntries);
        if (splits.Length != 2)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Используйте формат {старая категория} {новая категория}"); 
        
        var oldCategory = splits[0];
        var newCategory = splits[1];

        if (oldCategory == newCategory)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Вы ввели одинаковые названия");
        
        var infoMessage = await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: "Пытаемся изменить...");
        
        var contentJson = new StringContent(
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "oldCategory", oldCategory },
                { "newCategory", newCategory }
            }),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await _httpClient.PutAsync($"https://localhost:7166/api/UserExpenseCategory/{_tgUserId}", contentJson);
        
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: $"У вас нет категории {oldCategory}");
        
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Не удалось изменить категорию...");

        bool success = await Utility.SetWorkMode(_tgUserId, WorkMode.Default);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Категорию изменили, а с воркмодом какая то ошибка...");
        
        return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
            text: "Изменили категорию!");
    }
    
    // TODO: alert about existing expenses with this category
    public async Task<Message> RemoveCategoryHandler()
    {
        var infoMessage = await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: "Пытаемся удалить...");
        
        string messageText = _message.Text ?? throw new Exception("Null Message.Text in CategoryHandler");
        messageText = messageText.Trim().ToLower();
        
        var categoryToRemove = messageText;

        var httpResponseMessage = await _httpClient.DeleteAsync($"https://localhost:7166/api/UserExpenseCategory/{_tgUserId}/{categoryToRemove}");
        
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Не удалось удалить категорию...");
        
        bool success = await Utility.SetWorkMode(_tgUserId, WorkMode.Default);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Категорию удалили, а воркмод вернуть не удалось...");
        
        return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
            text: "Удалили категорию!");
    }
    
    public async Task<Message> BackCategoryHandler()
    {
        bool success = await Utility.SetWorkMode(_tgUserId, WorkMode.Default);
        Console.WriteLine(success
            ? $"поменяли режим на {WorkMode.Default.ToString()}"
            : $"не поменяли режим на {WorkMode.Default.ToString()} (((");

        await _bot.DeleteMessageAsync(_tgUserId, _message.MessageId);
        return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
            text: "Стандартный режим");
    }
    
    public async Task<Message> UnknownCategoryHandler()
    {
        return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(), 
            text: "Выбраное действие: Неизвестно");
    }
}