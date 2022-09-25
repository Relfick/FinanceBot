using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Net.Http.Headers;
using User = FinanceBot.Models.User;

namespace FinanceBot.Services;

public class TelegramBotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    private const string Token = "5612885286:AAG0C9ZiV8UhyRLfAFHUc4Lh-w4G-1nAksw";
    private TelegramBotClient Client { get; }

    public TelegramBotService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        Client = new TelegramBotClient(Token);
        Console.WriteLine("SUCCESS!!!");
    }
    
    public async Task StartBot()
    {
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };
        try
        {
            Client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await Client.GetMeAsync(cts.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
        }
        catch (Exception ex)
        {
            
        }
        finally
        {
            // Console.ReadLine();

            // Send cancellation request to stop bot
            // cts.Cancel();
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;

        string result = "a";
        if (messageText == "/start")
        {
            result = await RegisterUser(message.From!);
        }

        Console.WriteLine($"result: {result}");
        Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: result,
            cancellationToken: cancellationToken);
        
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "You said:\n" + messageText,
            cancellationToken: cancellationToken);
    }

    private async Task<string> RegisterUser(Telegram.Bot.Types.User user)
    {
        var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://localhost:7166/api/BotUser/{user.Id}")
        {
            Headers =
            {
                { HeaderNames.Accept, "application/json" },
                { HeaderNames.UserAgent, "HttpRequestsSample" }
            }
        };
        
        var httpClient = _httpClientFactory.CreateClient();
        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

        if (httpResponseMessage.StatusCode != HttpStatusCode.NotFound) return "user exists or error";
        
        
        var newUser = new User(user.Id, user.FirstName, user.Username ?? "");
        
        var newUserJson = new StringContent(
            JsonSerializer.Serialize(newUser),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/BotUser", newUserJson);
        return httpResponseMessage.IsSuccessStatusCode ? "registered" : "error";
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

}