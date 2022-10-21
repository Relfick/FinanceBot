using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinanceBot.Services.TgBot;

public class TelegramBotService
{
    private TelegramBotClient _botClient { get; }

    public TelegramBotService(IConfiguration configuration)
    {
        _botClient = new TelegramBotClient(configuration["BotToken"]);
        
        Console.WriteLine("Запустились!");
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
            await _botClient.SetMyCommandsAsync(new List<BotCommand>
            {
                new() { Command = "/help", Description = "Справка" },
                new() { Command = "/categories", Description = "Редактирование категорий трат" },
            }, cancellationToken: cts.Token);
                
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: Utility.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync(cts.Token);

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

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message            => GlobalHandler.BotOnMessageReceived(update.Message!, botClient),
            UpdateType.EditedMessage      => GlobalHandler.BotOnMessageReceived(update.EditedMessage!, botClient),
            UpdateType.CallbackQuery      => GlobalHandler.BotOnCallbackQueryReceived(update.CallbackQuery!, botClient),
            UpdateType.InlineQuery        => Utility.BotOnInlineQueryReceived(update.InlineQuery!, botClient),
            UpdateType.ChosenInlineResult => Utility.BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
            _                             => Utility.UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            // await HandlePollingErrorAsync(exception);
        }
    }
}