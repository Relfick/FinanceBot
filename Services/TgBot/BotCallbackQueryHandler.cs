using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Services.TgBot;

public class BotCallbackQueryHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly CallbackQuery _callbackQuery;
    private readonly Message _message;
    
    public BotCallbackQueryHandler(ITelegramBotClient bot, CallbackQuery callbackQuery)
    {
        ArgumentNullException.ThrowIfNull(callbackQuery.Message);
        _bot = bot;
        _callbackQuery = callbackQuery;
        _message = callbackQuery.Message;
    }

    public async Task<Message> OnCallbackQueryReceived()
    {
        if (_callbackQuery.Data == null)
            throw new Exception("Null in callbackQuery.Data");
        
        var split = _callbackQuery.Data.Split(" ");
        string handler = split[0];
        string action = split[1];
        string extraData = "";
        if (split.Length > 2)
            extraData = split[2];

        return handler switch
        {
            "category" => await CategoryCallbackQueryHandler(action),
            "continueRemoveCategory" => await ContinueRemoveCategoryCallbackQueryHandler(action, extraData),
            _          => UnknownCallbackQueryHandler(handler, action)
        };
    }

    private async Task<Message> CategoryCallbackQueryHandler(string action)
    {
        var categoryHandler = new CategoryHandler(_bot, _message);
        return await (
            action switch
            {
                "add" => categoryHandler.BaseActionCategoryShowInfo(UserWorkMode.AddCategory),
                "edit" => categoryHandler.BaseActionCategoryShowInfo(UserWorkMode.EditCategory),
                "remove" => categoryHandler.BaseActionCategoryShowInfo(UserWorkMode.RemoveCategory),
                "back" => categoryHandler.BackCategoryHandler(),
                _ => categoryHandler.UnknownCategoryHandler()
            }); 
    }
    
    private async Task<Message> ContinueRemoveCategoryCallbackQueryHandler(string action, string categoryToRemove)
    {
        if (categoryToRemove == "")
            throw new Exception("Category to remove is empty!");
        
        var categoryHandler = new CategoryHandler(_bot, _message);
        return await (
            action switch
            {
                "confirm" => categoryHandler.ContinueRemoveCategory(categoryToRemove, true, _message.MessageId),
                "cancel" => categoryHandler.ContinueRemoveCategory(categoryToRemove, false, _message.MessageId),
                _ => categoryHandler.UnknownCategoryHandler()
            }); 
    }
    
    private Message UnknownCallbackQueryHandler(string handler, string action)
    {
        throw new Exception($"Got unknown handler {handler} with action {action}");
    }
}