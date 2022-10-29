using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = FinanceBot.Models.User;

namespace FinanceBot.Services.TgBot;

public class BotMessageHandler
{
    private readonly Message _message;
    private readonly long _tgUserId;
    private readonly ITelegramBotClient _bot;
    private readonly HttpClient _httpClient;

    public BotMessageHandler(Message? message, ITelegramBotClient bot)
    {
        ArgumentNullException.ThrowIfNull(message);
        
        _message = message;
        _tgUserId = message.Chat.Id;
        _bot = bot;
        _httpClient = new HttpClient();
    }

    public async Task BotOnMessageReceived()
    {
        ShowMessageInfo(_message);

        var action = _message.Text!.Split(' ')[0] switch
        {
            "/start" => StartCommandHandler(),
            "/categories" => new CategoryHandler(_bot, _message, _httpClient).CategoriesCommandHandler(),
            "/expenses" => new ExpenseHandler(_bot, _message, _httpClient).ExpenseCommandHandler(),
            "/help" => HelpCommandHandler(),
            _ => TextMessageHandler()
        };
        await action;
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

    private async Task<Message> HelpCommandHandler()
    {
        const string usage = "Введи траты в формате \n{Название} {Стоимость} {Категория}\n\n" +
                             "Для добавления категорий используйте команду /categories";

        return await _bot.SendTextMessageAsync(chatId: _tgUserId, text: usage);
    }
    
    private async Task<Message> StartCommandHandler()
    {
        if (await Utility.UserExists(_tgUserId, _httpClient))
            return await _bot.SendTextMessageAsync(
                chatId: _tgUserId,
                text: "Ты уже в списочке, не переживай.");

        var newUser = new User(_tgUserId, _message.Chat.FirstName ?? "", _message.Chat.Username ?? "");
        var newUserJson = new StringContent(
            JsonSerializer.Serialize(newUser),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await _httpClient.PostAsync("https://localhost:7166/api/BotUser", newUserJson);
        string responseMessageText = 
            httpResponseMessage.IsSuccessStatusCode ? 
                "Привет! Ты успешно зарегистрирован!\n\n" +
                "Доступные пока команды: \n\n" +
                "/help" : 
                "Error";

        var userWorkMode = new UserWorkMode(_tgUserId, WorkMode.Default);
        var newUserWorkMode = new StringContent(
            JsonSerializer.Serialize(userWorkMode),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        await _httpClient.PostAsync("https://localhost:7166/api/UserWorkmode", newUserWorkMode);

        return await _bot.SendTextMessageAsync(chatId: _tgUserId, text: responseMessageText);
    }

    private async Task<Message> TextMessageHandler()
    {
        var userWorkMode = await Utility.GetUserWorkMode(_httpClient, _tgUserId);
        Console.WriteLine($"Workmode: {userWorkMode.ToString()}");

        var action = userWorkMode switch
        {
            WorkMode.AddCategory => new CategoryHandler(_bot, _message, _httpClient).AddCategoryHandler(),
            WorkMode.EditCategory => new CategoryHandler(_bot, _message, _httpClient).EditCategoryHandler(),
            WorkMode.RemoveCategory => new CategoryHandler(_bot, _message, _httpClient).RemoveCategoryHandler(),
            _ => new ExpenseHandler(_bot, _message, _httpClient).AddExpenseHandler()
        };

        return await action;
    }
}