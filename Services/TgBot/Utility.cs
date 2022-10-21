using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FinanceBot.Models;
using Microsoft.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace FinanceBot.Services.TgBot;

public static class Utility
{
    public static async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, ITelegramBotClient botClient)
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

        await botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
            results: results,
            isPersonal: true,
            cacheTime: 0);
    }

    public static Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
    {
        Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
        return Task.CompletedTask;
    }

    public static Task UnknownUpdateHandlerAsync(Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
    
    public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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
    
    public static async Task<WorkMode> GetUserWorkMode(HttpClient httpClient, long tgUserId)
    {
        var httpResponseMessage = await httpClient.GetAsync($"https://localhost:7166/api/UserWorkmode/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("Попытка получить workMode несуществующего юзера");

        var workMode = await httpResponseMessage.Content.ReadFromJsonAsync<WorkMode>();

        return workMode;
    }

    public static async Task<List<string>> GetUserCategories(HttpClient httpClient, long tgUserId)
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

    public static async Task<bool> SetWorkMode(long tgUserId, WorkMode workMode)
    {
        var httpClient = new HttpClient();
        var userWorkMode = new UserWorkMode(tgUserId, workMode);
        var newUserWorkMode = new StringContent(
            JsonSerializer.Serialize(userWorkMode),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await httpClient.PutAsync($"https://localhost:7166/api/UserWorkmode/{tgUserId}", newUserWorkMode);
        return httpResponseMessage.IsSuccessStatusCode;
    }

    public static async Task<bool> UserExists(Telegram.Bot.Types.User tgUser, HttpClient httpClient)
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
}