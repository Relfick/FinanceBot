using System.Text;
using FinanceBot.Models;
using FinanceBot.Services.TgBot.ModelsApi;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinanceBot.Services.TgBot;

public class CategoryHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly Message _message;
    private string _messageText;
    private readonly long _tgUserId;
    private readonly ExpenseCategoryApi _categoryApi;
    private readonly WorkModeApi _workModeApi;
    
    public CategoryHandler(ITelegramBotClient bot, Message message)
    {
        _bot = bot;
        _message = message;
        _messageText = _message.Text ?? throw new Exception("Message.Text = Null in CategoryHandler");
        _tgUserId = message.Chat.Id;
        _categoryApi = new ExpenseCategoryApi();
        _workModeApi = new WorkModeApi();
    }
    
    public async Task<Message> CategoriesCommandHandler()
    {
        var userCategories = await _categoryApi.GetUserCategories(_tgUserId);
        var hasCategories = userCategories.Count > 0;
        
        return await ShowCategoriesMessage(userCategories, hasCategories);
    }

    private async Task<Message> ShowCategoriesMessage(List<string> userCategories, bool hasCategories)
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
                        InlineKeyboardButton.WithCallbackData("Добавить", "category add"),
                        InlineKeyboardButton.WithCallbackData("Удалить", "category remove"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Изменить", "category edit"),
                        InlineKeyboardButton.WithCallbackData("Назад", "category back"),
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
                        InlineKeyboardButton.WithCallbackData("Добавить", "category add"),
                        InlineKeyboardButton.WithCallbackData("Назад", "category back"),
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
        
        bool success = await _workModeApi.PutWorkMode(_tgUserId, workMode);
        Console.WriteLine(success
            ? $"поменяли режим на {workMode.ToString()}"
            : $"не поменяли режим на {workMode.ToString()} (((");
        
        return await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: $"Введите название категории, которую хотите {suggest}"); 
    }

    public async Task<Message> AddCategoryHandler()
    {
        string newCategoryText = _messageText.Trim().ToLower();
        if (newCategoryText.Split(" ").Length > 1)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Название категории должно быть из одного слова!");

        var infoMessage = await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: "Пытаемся добавить...");
        
        var userCategories = await _categoryApi.GetUserCategories(_tgUserId);
        if (userCategories.Contains(newCategoryText))
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: $"У вас уже есть категория {newCategoryText}");
        
        var newCategory = new UserExpenseCategory(_tgUserId, newCategoryText);
        
        bool success = await _categoryApi.PostCategory(newCategory);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId, 
                text: "Не удалось добавить категорию...");

        success = await _workModeApi.PutWorkMode(_tgUserId, WorkMode.Default);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Категорию добавили, а с воркмодом какая то ошибка...");
        
        await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
            text: "Добавили категорию!");

        return await CategoriesCommandHandler();
    } 
    
    public async Task<Message> EditCategoryHandler()
    {
        _messageText = _messageText.Trim().ToLower();
        
        var splits = _messageText.Split(' ', StringSplitOptions.TrimEntries);
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

        var categories = new Dictionary<string, string>
        {
            { "oldCategory", oldCategory },
            { "newCategory", newCategory }
        };
        
        bool success = await _categoryApi.PutCategory(_tgUserId, categories);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Не удалось изменить категорию...");

        success = await _workModeApi.PutWorkMode(_tgUserId, WorkMode.Default);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Категорию изменили, а с воркмодом какая то ошибка...");
        
        await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
            text: "Изменили категорию!");
        
        return await CategoriesCommandHandler();
    }
    
    public async Task<Message> RemoveCategoryHandler()
    {
        var expenseApi = new ExpenseApi();
        var infoMessage = await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: "Пытаемся удалить...");
        
        var categoryToRemove = _messageText.Trim().ToLower();

        List<string> userCategories = await _categoryApi.GetUserCategories(_tgUserId);
        if (!userCategories.Contains(categoryToRemove))
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: $"У вас нет категории {categoryToRemove}. Введите название категории, которую хотите удалить:");

        // Get existing expenses with this category
        List<Expense> expenses = await expenseApi.GetExpensesWithCategory(_tgUserId, categoryToRemove);
        
        if (expenses.Count != 0)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да", $"continueRemoveCategory confirm {categoryToRemove}"),
                    InlineKeyboardButton.WithCallbackData("Нет", $"continueRemoveCategory cancel {categoryToRemove}"),
                });
                    
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                replyMarkup: inlineKeyboard,
                text: $"У вас есть {expenses.Count} трат c категорией {categoryToRemove}. " +
                       "При удалении категории они также удалятся. Желаете продолжить?");
        }

        return await ContinueRemoveCategory(categoryToRemove, true, infoMessage.MessageId);
    }

    public async Task<Message> ContinueRemoveCategory(string categoryToRemove, bool confirm, int messageId)
    {
        await _bot.DeleteMessageAsync(chatId: _tgUserId, messageId: messageId);
        
        if (!confirm)
        {
            return await _bot.SendTextMessageAsync(chatId: _tgUserId,
                text: "Не удаляем."); 
        }
        
        var infoMessage = await _bot.SendTextMessageAsync(chatId: _tgUserId,
            text: "Удаляем..."); 
        
        bool success = await _categoryApi.DeleteAsync(_tgUserId, categoryToRemove);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Не удалось удалить категорию...");
        
        success = await _workModeApi.PutWorkMode(_tgUserId, WorkMode.Default);
        if (!success)
            return await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
                text: "Категорию удалили, а воркмод вернуть не удалось...");
        
        await _bot.EditMessageTextAsync(chatId: _tgUserId, messageId: infoMessage.MessageId,
            text: "Удалили категорию!");
        
        return await CategoriesCommandHandler(); 
    }
    
    public async Task<Message> BackCategoryHandler()
    {
        bool success = await _workModeApi.PutWorkMode(_tgUserId, WorkMode.Default);
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