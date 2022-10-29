using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinanceBot.Services.TgBot;

public class TelegramBotService
{
    private TelegramBotClient BotClient { get; }

    public TelegramBotService(IConfiguration configuration)
    {
        BotClient = new TelegramBotClient(configuration["BotToken"]);
        
        Console.WriteLine("Запустились!");
    }
    
    public async Task StartBot()
    {
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(), // receive all update types
            ThrowPendingUpdates = true,
        };
        try
        {
            await BotClient.SetMyCommandsAsync(new List<BotCommand>
            {
                new() { Command = "/help", Description = "Справка" },
                new() { Command = "/categories", Description = "Редактирование категорий трат" },
                new() { Command = "/expenses", Description = "Получение списка трат" },
            }, cancellationToken: cts.Token);
                
            BotClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: Utility.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await BotClient.GetMeAsync(cts.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            // cts.Cancel();
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message            => new BotMessageHandler(update.Message, botClient).BotOnMessageReceived(),
            UpdateType.CallbackQuery      => new BotCallbackQueryHandler(botClient, update.CallbackQuery!).OnCallbackQueryReceived(),
            _                             => Utility.UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await Utility.HandlePollingErrorAsync(botClient, exception, cancellationToken);
        }
    }
}