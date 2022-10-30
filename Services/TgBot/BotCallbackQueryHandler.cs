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
        
        var actionHandler = _callbackQuery.Data.Split(" ");
        string action = actionHandler[0];
        string handler = actionHandler[1];

        return handler switch
        {
            "category" => await CategoryCallbackQueryHandler(action),
            _          => UnknownCallbackQueryHandler(handler, action)
        };
    }

    private async Task<Message> CategoryCallbackQueryHandler(string action)
    {
        var categoryHandler = new CategoryHandler(_bot, _message);
        return await (
            action switch
            {
                "add" => categoryHandler.BaseActionCategoryShowInfo(WorkMode.AddCategory),
                "edit" => categoryHandler.BaseActionCategoryShowInfo(WorkMode.EditCategory),
                "remove" => categoryHandler.BaseActionCategoryShowInfo(WorkMode.RemoveCategory),
                "back" => categoryHandler.BackCategoryHandler(),
                _ => categoryHandler.UnknownCategoryHandler()
            }); 
    }
    
    private Message UnknownCallbackQueryHandler(string handler, string action)
    {
        throw new Exception($"Got unknown handler {handler} with action {action}");
    }
}