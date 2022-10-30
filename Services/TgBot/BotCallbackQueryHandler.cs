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

    // TODO: Add handler for category actions
    public async Task<Message> OnCallbackQueryReceived()
    {
        return await (
            _callbackQuery.Data switch
            {
                "add" => new CategoryHandler(_bot, _message).BaseActionCategoryShowInfo(WorkMode.AddCategory),
                "edit" => new CategoryHandler(_bot, _message).BaseActionCategoryShowInfo(WorkMode.EditCategory),
                "remove" => new CategoryHandler(_bot, _message).BaseActionCategoryShowInfo(WorkMode.RemoveCategory),
                "back" => new CategoryHandler(_bot, _message).BackCategoryHandler(),
                _ => new CategoryHandler(_bot, _message).UnknownCategoryHandler()
            });
    }
}