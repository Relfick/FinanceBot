using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace FinanceBot.Services;

public class TelegramBotService
{
    private const string Token = "5612885286:AAG0C9ZiV8UhyRLfAFHUc4Lh-w4G-1nAksw";
    public TelegramBotClient Client { get; }

    public TelegramBotService()
    {
        Client = new TelegramBotClient(Token);
        // Console.WriteLine("SUCCESS!!!");
    }

    // public async Task StartBot()
    // {
    //     using var cts = new CancellationTokenSource();
    //     var receiverOptions = new ReceiverOptions
    //     {
    //         AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
    //     };
    //
    //     Client.StartReceiving(
    //         updateHandler: HandleUpdateAsync,
    //         pollingErrorHandler: HandlePollingErrorAsync,
    //         receiverOptions: receiverOptions,
    //         cancellationToken: cts.Token
    //     );
    //     
    //     var me = await Client.GetMeAsync(cts.Token);
    //
    //     Console.WriteLine($"Start listening for @{me.Username}");
    //     // Console.ReadLine();
    //
    //     // Send cancellation request to stop bot
    //     // cts.Cancel(); 
    // }
    //
    // async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    // {
    //     // Only process Message updates: https://core.telegram.org/bots/api#message
    //     if (update.Message is not { } message)
    //         return;
    //     // Only process text messages
    //     if (message.Text is not { } messageText)
    //         return;
    //     
    //     
    //
    //     var chatId = message.Chat.Id;
    //
    //     Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    //
    //     // Echo received message text
    //     Message sentMessage = await botClient.SendTextMessageAsync(
    //         chatId: chatId,
    //         text: "You said:\n" + messageText,
    //         cancellationToken: cancellationToken);
    // }
    //
    // Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    // {
    //     var ErrorMessage = exception switch
    //     {
    //         ApiRequestException apiRequestException
    //             => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
    //         _ => exception.ToString()
    //     };
    //
    //     Console.WriteLine(ErrorMessage);
    //     return Task.CompletedTask;
    // }
}