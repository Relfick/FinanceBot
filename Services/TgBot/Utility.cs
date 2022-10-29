using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FinanceBot.Models;
using Microsoft.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace FinanceBot.Services.TgBot;

public static class Utility
{
    public static Task UnknownUpdateHandlerAsync(Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
    
    public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
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

    public static async Task<bool> UserExists(long tgUserId, HttpClient httpClient)
    {
       
        var httpRequestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://localhost:7166/api/BotUser/{tgUserId}")
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