using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinanceBot.Models;
using NuGet.Protocol;

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

    public async Task<bool> SetWorkMode(long tgUserId, UserWorkMode workMode)
    {
        // TODO: log it
        string errorLog = "";

        var httpResponseMessage = await _httpClient.GetAsync($"api/User/{tgUserId}");
        User? user = await httpResponseMessage.Content.ReadFromJsonAsync<User>();
        if (user == null)
            return false;
        user.WorkMode = workMode;
        httpResponseMessage = await _httpClient.PutAsJsonAsync($"api/User/{tgUserId}", user);
        
        return httpResponseMessage.IsSuccessStatusCode;
    }
    
    public async Task<UserWorkMode?> GetWorkMode(long tgUserId)
    {
        // TODO: log it
        string errorLog = "";
        
        var httpResponseMessage = await _httpClient.GetAsync($"api/User/{tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            return null;

        User? user = await httpResponseMessage.Content.ReadFromJsonAsync<User>();
        return user?.WorkMode;
    } 
}