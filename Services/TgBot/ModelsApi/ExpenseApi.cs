using System.Net;
using FinanceBot.Models;

namespace FinanceBot.Services.TgBot.ModelsApi;

public class ExpenseApi
{
    private readonly HttpClient _httpClient;

    public ExpenseApi()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:7166/");
    }

    public async Task<List<Expense>> GetExpenses(long tgUserId)
    {
        // TODO: Log it
        string errorLog = "";
        
        var httpResponseMessage = await _httpClient.GetAsync($"api/Expense/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound) 
            errorLog = "У вас пока нет ни одной траты";

        if (!httpResponseMessage.IsSuccessStatusCode)
            errorLog = "Ошибка получения трат";

        return await httpResponseMessage.Content.ReadFromJsonAsync<List<Expense>>() 
                   ?? throw new InvalidOperationException("Conversion from Content to List<Expense> failed!");
    }
    
    public async Task<bool> PostExpense(Expense expense)
    {
        var httpResponseMessage = await _httpClient.PostAsJsonAsync("api/Expense", expense);
        return httpResponseMessage.IsSuccessStatusCode;
    }
}