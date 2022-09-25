using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using FinanceBot.Models;
using FinanceBot.Services;
using Microsoft.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = FinanceBot.Models.User;

namespace FinanceBot.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationContext _db;
    
    private TelegramBotClient Client { get; set; }
    
    // private readonly ILogger<HomeController> _logger;
    
    public HomeController(ApplicationContext applicationContext, TelegramBotService telegramBotService,
        IHttpClientFactory httpClientFactory)
    {
        // _logger = logger;
        _httpClientFactory = httpClientFactory;
        
        _db = applicationContext;
        Client = telegramBotService.Client;
    }

    public async Task<IActionResult> Index()
    {
        await StartBot();
        Console.WriteLine("SUCCESS!!!");
        
        return View();
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
    
    async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
            result = await RegisterUserIfNeeded(message.From!);
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

    private async Task<string> RegisterUserIfNeeded(Telegram.Bot.Types.User user)
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
            MediaTypeNames.Application.Json); // using static System.Net.Mime.MediaTypeNames;

        httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/BotUser", newUserJson);
        return httpResponseMessage.IsSuccessStatusCode ? "registered" : "error";

    }
    
    Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}