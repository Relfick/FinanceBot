using FinanceBot.Models;
using FinanceBot.Services.TgBot.ModelsApi;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = FinanceBot.Models.User;    // Telegram.Bot.Types contains the same Class

namespace FinanceBot.Services.TgBot;

public class BotMessageHandler
{
    private readonly Message _message;
    private readonly long _tgUserId;
    private readonly ITelegramBotClient _bot;

    public BotMessageHandler(Message? message, ITelegramBotClient bot)
    {
        ArgumentNullException.ThrowIfNull(message);
        
        _message = message;
        _tgUserId = message.Chat.Id;
        _bot = bot;
    }

    public async Task BotOnMessageReceived()
    {
        ShowMessageInfo(_message);

        var action = _message.Text!.Split(' ')[0] switch
        {
            "/start" => StartCommandHandler(),
            "/categories" => new CategoryHandler(_bot, _message).CategoriesCommandHandler(),
            "/expenses" => new ExpenseHandler(_bot, _message).ExpenseCommandHandler(),
            "/help" => HelpCommandHandler(),
            _ => TextMessageHandler()
        };
        await action;
    }

    private async Task<Message> HelpCommandHandler()
    {
        const string usage = "Введи траты в формате \n{Название} {Стоимость} {Категория}\n\n" +
                             "Для добавления категорий используйте команду /categories";

        return await _bot.SendTextMessageAsync(chatId: _tgUserId, text: usage);
    }
    
    private async Task<Message> StartCommandHandler()
    {
        var userApi = new UserApi();
        
        if (await userApi.UserExists(_tgUserId))
            return await _bot.SendTextMessageAsync(
                chatId: _tgUserId,
                text: "Ты уже в списочке, не переживай.");

        string userFirstName = _message.Chat.FirstName ?? "";
        string userUserName = _message.Chat.Username ?? "";
        var newUser = new User(_tgUserId, userFirstName, userUserName);
        
        bool success = await userApi.PostUser(newUser);
        
        string responseMessageText = success ? 
                "Привет! Ты успешно зарегистрирован!\n\n" +
                "Доступные пока команды: \n\n" +
                "/help" : 
                "Ошибка регистрации...";

        return await _bot.SendTextMessageAsync(chatId: _tgUserId, text: responseMessageText);
    }

    private async Task<Message> TextMessageHandler()
    {
        UserWorkMode? userWorkMode = await new UserApi().GetWorkMode(_tgUserId);
        if (userWorkMode == null)
            throw new NullReferenceException("Null in user.WorkMode");
        
        Console.WriteLine($"Workmode: {userWorkMode.ToString()}");

        var action = userWorkMode switch
        {
            UserWorkMode.AddCategory => new CategoryHandler(_bot, _message).AddCategoryHandler(),
            UserWorkMode.EditCategory => new CategoryHandler(_bot, _message).EditCategoryHandler(),
            UserWorkMode.RemoveCategory => new CategoryHandler(_bot, _message).RemoveCategoryHandler(),
            _ => new ExpenseHandler(_bot, _message).AddExpenseHandler()
        };

        return await action;
    }
    
    private void ShowMessageInfo(Message message)
    {
        Console.WriteLine($"Receive message type: {message.Type}");
        if (message.Type != MessageType.Text)
            return;
        Console.WriteLine($"Message: {message.Text}");
        Console.WriteLine($"MessageId: {message.MessageId}");
        Console.WriteLine($"UserId: {message.From!.Id}");
    }
}