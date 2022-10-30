using System.Net;
using FinanceBot.Models;

namespace FinanceBot.Services.TgBot.ModelsApi;

public class WorkModeApi
{
    private readonly HttpClient _httpClient;

    public WorkModeApi()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:7166/");
    }

    public async Task<WorkMode> GetWorkMode(long tgUserId)
    {
        var httpResponseMessage = await _httpClient.GetAsync($"api/UserWorkmode/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("Попытка получить workMode несуществующего юзера");

        var workMode = await httpResponseMessage.Content.ReadFromJsonAsync<WorkMode>();

        return workMode;
    }
    
    public async Task<bool> PutWorkMode(long tgUserId, WorkMode workMode)
    {
        var userWorkMode = new UserWorkMode(tgUserId, workMode);
        
        var httpResponseMessage = await _httpClient.PutAsJsonAsync($"api/UserWorkmode/{tgUserId}", userWorkMode);
        return httpResponseMessage.IsSuccessStatusCode;
    }

    public async Task<bool> PostWorkMode(UserWorkMode userWorkMode)
    {
        var httpResponseMessage = await _httpClient.PostAsJsonAsync("api/UserWorkmode", userWorkMode);
        return httpResponseMessage.IsSuccessStatusCode;
    }
}