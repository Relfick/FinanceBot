using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FinanceBot.Models;
using FinanceBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinanceBot.Controllers;

public class HomeController : Controller
{
    private TelegramBotClient Client { get; set; }
    
    // private readonly ILogger<HomeController> _logger;
    private readonly ApplicationContext _db;

    public HomeController(ApplicationContext applicationContext, TelegramBotService telegramBotService)
    {
        // _logger = logger;
        
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

        Client.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );
        
        var me = await Client.GetMeAsync(cts.Token);

        Console.WriteLine($"Start listening for @{me.Username}");
        // Console.ReadLine();

        // Send cancellation request to stop bot
        // cts.Cancel(); 
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

        Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

        // Echo received message text
        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "You said:\n" + messageText,
            cancellationToken: cancellationToken);
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