using FinanceBot.Models;

namespace FinanceBot.Services.TgBot.ModelsApi;

public class UserApi
{
    private readonly HttpClient _httpClient;

    public UserApi()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:7166/");
    } 
    
    public async Task<bool> UserExists(long tgUserId)
    {
        var httpResponseMessage = await _httpClient.GetAsync($"api/User/{tgUserId}");
        return httpResponseMessage.IsSuccessStatusCode;
    }
    
    public async Task<bool> PostUser(User newUser)
    {
        var httpResponseMessage = await _httpClient.PostAsJsonAsync("api/User", newUser);
        return httpResponseMessage.IsSuccessStatusCode;
    }
}