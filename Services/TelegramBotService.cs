using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceBot.Models;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Net.Http.Headers;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using User = FinanceBot.Models.User;

namespace FinanceBot.Services;

public class TelegramBotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    private TelegramBotClient _botClient { get; }

    public TelegramBotService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
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
                pollingErrorHandler: HandlePollingErrorAsync,
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
            UpdateType.Message            => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage      => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery      => BotOnCallbackQueryReceived(update.CallbackQuery!),
            UpdateType.InlineQuery        => BotOnInlineQueryReceived(update.InlineQuery!),
            UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
            _                             => UnknownUpdateHandlerAsync(update)
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

    private async Task BotOnMessageReceived(Message message)
    {
        Console.WriteLine($"Receive message type: {message.Type}");
        if (message.Type != MessageType.Text)
            return;
        Console.WriteLine($"Message: {message.Text}");
        Console.WriteLine($"UserId: {message.From!.Id}");
        
        var action = message.Text!.Split(' ')[0] switch
        {
            "/start"    => RegisterUserHandler(_botClient, message),
            "/categories"    => CategoryHandler(_botClient, message),
            // "/inline"   => SendInlineKeyboard(_botClient, message),
            // "/keyboard" => SendReplyKeyboard(_botClient, message),
            // "/remove"   => RemoveKeyboard(_botClient, message),
            // "/photo"    => SendFile(_botClient, message),
            // "/request"  => RequestContactAndLocation(_botClient, message),
            "/help"     => UsageHandler(_botClient, message),
            _           => CommonMessageHandler(_botClient, message)
        };
        Message sentMessage = await action;
        Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");
        
        
        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        static async Task<Message> SendInlineKeyboard(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            // Simulate longer running task
            await Task.Delay(500);

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    },
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Choose",
                                                  replyMarkup: inlineKeyboard);
        }
        
        static async Task<Message> SendReplyKeyboard(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { "Добавить", "Удалить" },
                    new KeyboardButton[] { "Изменить", "Назад" },
                })
            {
                ResizeKeyboard = true
            };

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: replyKeyboardMarkup);
        }
        
        static async Task<Message> RemoveKeyboard(ITelegramBotClient bot, Message message)
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Removing keyboard",
                                                  replyMarkup: new ReplyKeyboardRemove());
        }

        static async Task<Message> SendFile(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            const string filePath = @"Files/tux.png";
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

            return await bot.SendPhotoAsync(chatId: message.Chat.Id,
                                            photo: new InputOnlineFile(fileStream, fileName),
                                            caption: "Nice Picture");
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup requestReplyKeyboard = new(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: "Who or Where are you?",
                                                  replyMarkup: requestReplyKeyboard);
        }

        static async Task<Message> UsageHandler(ITelegramBotClient bot, Message message)
        {
            const string usage = "Введи траты в формате {Название} {Стоимость} {Категория}\n\n" +
                                 "Доступные категории: {еда}, {одежда}, {развлечения}\n" +
                                 "(на самом деле их больше, но ты о них не узнаешь\n\n" +
                                 "Позже появится возможность редактирования категорий.";
            
            // const string usage = "Usage:\n" +
            //                      "/inline   - send inline keyboard\n" +
            //                      "/keyboard - send custom keyboard\n" +
            //                      "/remove   - remove custom keyboard\n" +
            //                      "/photo    - send a photo\n" +
            //                      "/request  - request location or contact";

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                                  text: usage,
                                                  replyMarkup: new ReplyKeyboardRemove());
        }
    }

    private async Task<Message> CategoryHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tgUser = message.From!;
        var tgUserId = tgUser.Id;

        var userCategories = await GetUserCategories(httpClient, tgUserId);
        var hasCategories = userCategories.Count > 0;
        
        return await ShowCategoriesMessage(bot, message.Chat.Id, userCategories, hasCategories);

        // return await bot.SendTextMessageAsync(
        //     chatId: message.Chat.Id,
        //     text: "some text",
        //     replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> ShowCategoriesMessage(ITelegramBotClient bot, long chatId, 
        List<string> userCategories,
        bool hasCategories = true)
    {
        var responseMessage = new StringBuilder();

        InlineKeyboardMarkup inlineKeyboard;
        if (hasCategories)
        {
            responseMessage.Append("Текущие категории:\n\n");
            foreach (string category in userCategories)
            {
                responseMessage.Append($"    {category}\n");
            }
            
            inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Добавить", "add"),
                        InlineKeyboardButton.WithCallbackData("Удалить", "remove"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Изменить", "edit"),
                        InlineKeyboardButton.WithCallbackData("Назад", "back"),
                    },
                });
        }
        else
        {
            responseMessage.Append("Вы еще не добавили категории.\n\nВыберите действие:");
            
            inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Добавить", "add"),
                        InlineKeyboardButton.WithCallbackData("Назад", "back"),
                    },
                });
        }
        
        return await bot.SendTextMessageAsync(
            chatId: chatId,
            text: responseMessage.ToString(),
            replyMarkup: inlineKeyboard,
            allowSendingWithoutReply: false);
    }

    private async Task<Message> AddCategoryShowInfo(ITelegramBotClient bot, Message message)
    {
        bool success = await SetWorkMode(_httpClientFactory.CreateClient(), message.Chat.Id, WorkMode.AddCategory);
        if (success)
            Console.WriteLine("поменяли режим");
        else
            Console.WriteLine("не поменяли режим(((");
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите название для новой категории:",
            replyMarkup: new ReplyKeyboardRemove()
            ); 
    }
    
    private async Task<Message> AddCategoryHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tgUser = message.From!;
        var tgUserId = tgUser.Id;
        
        if (message.Text!.Split(" ").Length > 1)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Название категории должно быть из одного слова!",
                replyMarkup: new ReplyKeyboardRemove()
            );

        var newCategory = new UserExpenseCategory(tgUserId, message.Text!);
        var newCategoryJson = new StringContent(
            JsonSerializer.Serialize(newCategory),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/UserExpenseCategory", newCategoryJson);
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Какая то ошибка...",
                replyMarkup: new ReplyKeyboardRemove()
            );

        bool success = await SetWorkMode(httpClient, tgUserId, WorkMode.Default);
        if (!success)
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Категорию добавли, а с воркмодом какая то ошибка...",
                replyMarkup: new ReplyKeyboardRemove()
            );
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Добавили категорию и даже поменяли воркмод!",
            replyMarkup: new ReplyKeyboardRemove()
        );
    }

    private async Task<WorkMode> GetUserWorkMode(HttpClient httpClient, long tgUserId)
    {
        var httpResponseMessage = await httpClient.GetAsync($"https://localhost:7166/api/UserWorkmode/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("Попытка получить workMode несуществующего юзера");

        var workMode = await httpResponseMessage.Content.ReadFromJsonAsync<WorkMode>();

        return workMode;
    }

    private async Task<Message> EditCategoryHandler(ITelegramBotClient bot, Message message)
    {
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выбраное действие: Редактировать",
            replyMarkup: new ReplyKeyboardRemove()); 
    }
    
    private async Task<Message> RemoveCategoryHandler(ITelegramBotClient bot, Message message)
    {
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выбраное действие: Удалить",
            replyMarkup: new ReplyKeyboardRemove()); 
    }
    
    private async Task<Message> BackCategoryHandler(ITelegramBotClient bot, Message message)
    {
        var chatId = message.Chat.Id;
        await bot.DeleteMessageAsync(message.Chat.Id, message.MessageId);
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выбраное действие: Назад",
            replyMarkup: new ReplyKeyboardRemove()); 
    }
    
    private async Task<Message> UnknownCategoryHandler(ITelegramBotClient bot, Message message)
    {
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выбраное действие: Неизвестно",
            replyMarkup: new ReplyKeyboardRemove()); 
    }
    

    private async Task<List<string>> GetUserCategories(HttpClient httpClient, long tgUserId)
    {
        var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://localhost:7166/api/UserExpenseCategory/{tgUserId}")
        {
            Headers =
            {
                { HeaderNames.Accept, "application/json" },
                { HeaderNames.UserAgent, "HttpRequestsSample" },
            }
        };
        
        // var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        var httpResponseMessage = await httpClient.GetAsync($"https://localhost:7166/api/UserExpenseCategory/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            return new List<string>();

        var userExpenseCategories = await httpResponseMessage.Content.ReadFromJsonAsync<List<UserExpenseCategory>>();
        if (userExpenseCategories == null)
            return new List<string>();

        var categories = userExpenseCategories.Select(c => c.expenseCategory).ToList();
        return categories;
    }

    private async Task<Message> CommonMessageHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tgUserId = message.From!.Id;
        
        var userWorkMode = await GetUserWorkMode(httpClient, tgUserId);
        Console.WriteLine($"Workmode: {userWorkMode.ToString()}");
        
        var action = userWorkMode switch
        {
            WorkMode.AddCategory      => AddCategoryHandler(_botClient, message),
            WorkMode.EditCategory     => EditCategoryHandler(_botClient, message),
            WorkMode.RemoveCategory   => RemoveCategoryHandler(_botClient, message),
            _                         => AddExpenseHandler(_botClient, message)
        };
        
        return await action;
    }

    private async Task<Message> AddExpenseHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tgUser = message.From!;

        var userId = tgUser.Id;
        var r = new Regex(@"(?<name>(\w+\s)+)(?<cost>\d+)\s(?<category>(\w+\s*)+)", RegexOptions.Compiled);
        var m = r.Match(message.Text!);
        if (!m.Success)
        {
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Используйте формат {покупка} {цена} {категория}",
                replyMarkup: new ReplyKeyboardRemove());
        }

        var expenseName = m.Result("${name}").Trim();
        var expenseCost = int.Parse(m.Result("${cost}"));

        var foodCategoryAliases = new List<string> { "food", "еда", "продукты" };
        var clothesCategoryAliases = new List<string> { "clothes", "одежда", "шмот", "шмотки", "шмотье", "крутая одежда" };
        var funCategoryAliases = new List<string> { "fun", "развлечение", "отдых", "кафе и рестораны" };
        var categoryAliases = new List<List<string>> { foodCategoryAliases, clothesCategoryAliases, funCategoryAliases };

        string category = m.Result("${category}");
        int categoryId;
        bool validCategory = false;
        for (categoryId = 0; categoryId < categoryAliases.Count; categoryId++)
        {
            if (categoryAliases[categoryId].Contains(category.ToLower().Trim()))
            {
                validCategory = true;
                break;
            }
        }
        if (!validCategory)
        {
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Несуществующая категория: {category}",
                replyMarkup: new ReplyKeyboardRemove()); 
        }
        
        var expenseCategory = categoryAliases[categoryId][0];

        var expenseDate = DateTime.Now;

        var expense = new Expense(
            userId,
            expenseName,
            expenseCost,
            expenseCategory,
            expenseDate);
        
        var expenseJson = new StringContent(
            JsonSerializer.Serialize(expense),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/Expense", expenseJson);
        string responseMessageText = 
            httpResponseMessage.IsSuccessStatusCode ? 
                "Добавили!" : 
                "Не получилось добавить :(";
        
        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessageText,
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> RegisterUserHandler(ITelegramBotClient bot, Message message)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tgUser = message.From!;

        if (await UserExists(tgUser, httpClient))
        {
            return await bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ты уже в списочке, не переживай.",
                replyMarkup: new ReplyKeyboardRemove());
        }
        
        var newUser = new User(tgUser.Id, tgUser.FirstName, tgUser.Username ?? "");
        var newUserJson = new StringContent(
            JsonSerializer.Serialize(newUser),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/BotUser", newUserJson);
        string responseMessageText = 
            httpResponseMessage.IsSuccessStatusCode ? 
            "Привет! Ты успешно зарегистрирован!\n\n" +
            "Доступные пока команды: \n\n" +
            "/help" : 
            "Error";

        var userWorkMode = new UserWorkMode(tgUser.Id, WorkMode.Default);
        var newUserWorkMode = new StringContent(
            JsonSerializer.Serialize(userWorkMode),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        httpResponseMessage = await httpClient.PostAsync("https://localhost:7166/api/UserWorkmode", newUserWorkMode);
        if (httpResponseMessage.IsSuccessStatusCode)
            Console.WriteLine("userWorkmode successfully added");
        else
            Console.WriteLine("userWorkmode did not added. Error!");

        return await bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseMessageText,
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<bool> SetWorkMode(HttpClient httpClient, long tgUserId, WorkMode workMode)
    {
        var userWorkMode = new UserWorkMode(tgUserId, workMode);
        var newUserWorkMode = new StringContent(
            JsonSerializer.Serialize(userWorkMode),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PutAsync($"https://localhost:7166/api/UserWorkmode/{tgUserId}", newUserWorkMode);
        return httpResponseMessage.IsSuccessStatusCode;
    }

    private async Task<bool> UserExists(Telegram.Bot.Types.User tgUser, HttpClient httpClient)
    {
       
        var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://localhost:7166/api/BotUser/{tgUser.Id}")
        {
            Headers =
            {
                { HeaderNames.Accept, "application/json" },
                { HeaderNames.UserAgent, "HttpRequestsSample" },
            }
        };
        
        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
        return httpResponseMessage.StatusCode != HttpStatusCode.NotFound;
    }

    
    // Process Inline Keyboard callback data
    private async Task<Message> BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        if (callbackQuery.Message == null)
            return await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                text: $"Received {callbackQuery.Data}");
        
        var message = callbackQuery.Message;
        return await (
            callbackQuery.Data switch
        {
            "add" => AddCategoryShowInfo(_botClient, message),
            "edit" => EditCategoryHandler(_botClient, message),
            "remove" => RemoveCategoryHandler(_botClient, message),
            "back" => BackCategoryHandler(_botClient, message),
            // _ => ShowCategoriesMessage(_botClient, message.Chat.Id, userCategories)
            _ => UnknownCategoryHandler(_botClient, message)
        });

        // return await _botClient.AnswerCallbackQueryAsync(
        //     callbackQueryId: callbackQuery.Id,
        //     text: $"Received {callbackQuery.Data}");
        //
    }

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
    {
        Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "3",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent(
                    "hello"
                )
            )
        };

        await _botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
                                                results: results,
                                                isPersonal: true,
                                                cacheTime: 0);
    }

    private Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
    {
        Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
        return Task.CompletedTask;
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
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