using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Services.TgBot;

public class BotCallbackQueryHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly HttpClient _httpClient;
    private readonly CallbackQuery _callbackQuery;
    private readonly Message _message;
    
    public BotCallbackQueryHandler(ITelegramBotClient bot, CallbackQuery callbackQuery)
    {
        ArgumentNullException.ThrowIfNull(callbackQuery.Message);
        _bot = bot;
        _httpClient = new HttpClient();
        _callbackQuery = callbackQuery;
        _message = callbackQuery.Message;
    }

    // TODO: Add handler for category actions
    public async Task<Message> OnCallbackQueryReceived()
    {
        return await (
            _callbackQuery.Data switch
            {
                "add" => new CategoryHandler(_bot, _message, _httpClient).BaseActionCategoryShowInfo(WorkMode.AddCategory),
                "edit" => new CategoryHandler(_bot, _message, _httpClient).BaseActionCategoryShowInfo(WorkMode.EditCategory),
                "remove" => new CategoryHandler(_bot, _message, _httpClient).BaseActionCategoryShowInfo(WorkMode.RemoveCategory),
                "back" => new CategoryHandler(_bot, _message, _httpClient).BackCategoryHandler(),
                _ => new CategoryHandler(_bot, _message, _httpClient).UnknownCategoryHandler()
            });
    }
}