using System.Net;
using FinanceBot.Models;

namespace FinanceBot.Services.TgBot.ModelsApi;

public class ExpenseCategoryApi
{
    private readonly HttpClient _httpClient;

    public ExpenseCategoryApi()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:7166/");
    }

    public async Task<List<string>> GetUserCategories(long tgUserId)
    {
        // TODO: Log it
        string errorLog = "";
        var httpResponseMessage = await _httpClient.GetAsync($"api/UserExpenseCategory/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("Error in api method");     // TODO: refactor

        var categories = await httpResponseMessage.Content.ReadFromJsonAsync<List<string>>()
                   ?? throw new InvalidOperationException("Conversion from Content to List<string> failed!");
        
        return categories;
    }

    public async Task<bool> PostCategory(UserExpenseCategory newCategory)
    {
        var httpResponseMessage = await _httpClient.PostAsJsonAsync("api/UserExpenseCategory", newCategory);
        return httpResponseMessage.IsSuccessStatusCode;
    }

    public async Task<bool> PutCategory(long tgUserId, Dictionary<string,string> categories)
    {
        // TODO: Log it
        string errorLog = "";
        
        var httpResponseMessage = await _httpClient.PutAsJsonAsync($"api/UserExpenseCategory/{tgUserId}", categories);
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            errorLog = $"У вас нет категории {categories["oldCategory"]}";

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            errorLog ="Не удалось изменить категорию...";
        }

        return httpResponseMessage.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(long tgUserId, string categoryToRemove)
    {
        // TODO: Log it
        string errorLog = "";
        var httpResponseMessage = await _httpClient.DeleteAsync(
            $"api/UserExpenseCategory/{tgUserId}/{categoryToRemove}");
        
        return httpResponseMessage.IsSuccessStatusCode;
    }
}