using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FinanceBot.Services.TgBot;

public class ExpenseHandler
{
    private readonly HttpClient _httpClient;
    private readonly ITelegramBotClient _bot;
    private readonly Message _message;
    private readonly long _tgUserId;

    public ExpenseHandler(ITelegramBotClient bot, Message message, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _bot = bot;
        _message = message;
        _tgUserId = message.Chat.Id;
    }

    public async Task<Message> AddExpenseHandler()
    {
        var r = new Regex(@"(?<name>(\w+\s)+)(?<cost>\d+)\s(?<category>(\w+\s*)+)", RegexOptions.Compiled);
        var m = r.Match(_message.Text!);
        if (!m.Success)
        {
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Используйте формат {покупка} {цена} {категория}");
        }

        var expenseName = m.Result("${name}").Trim().ToLower();
        var expenseCost = int.Parse(m.Result("${cost}"));
        string expenseCategory = m.Result("${category}").Trim().ToLower();
        
        if (expenseName.Length > 15 || expenseCategory.Length > 15)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Не используйте больше 15 символов в названии покупки/категории.");

        var userCategories = await Utility.GetUserCategories(_httpClient, _tgUserId);
        if (!userCategories.Contains(expenseCategory))
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: $"У вас нет категории {expenseCategory}");

        var expenseDate = DateTime.Now;

        var expense = new Expense(_tgUserId, expenseName, expenseCost, expenseCategory, expenseDate);
        
        var expenseJson = new StringContent(
            JsonSerializer.Serialize(expense),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        
        var httpResponseMessage = await _httpClient.PostAsJsonAsync("https://localhost:7166/api/Expense", expenseJson);
        string responseMessageText = 
            httpResponseMessage.IsSuccessStatusCode ? 
                "Добавили!" : 
                "Не удалось добавить (";

        return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
            text: responseMessageText);
    }

    public async Task<Message> ExpenseCommandHandler()
    {
        var httpResponseMessage = await _httpClient.GetAsync($"https://localhost:7166/api/Expense/{_tgUserId}");
        if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "У вас пока нет ни одной траты.");
        
        if (!httpResponseMessage.IsSuccessStatusCode)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "Ошибка получения трат ((");
                
        var expenses = await httpResponseMessage.Content.ReadFromJsonAsync<List<Expense>>();
        if (expenses == null)
            return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
                text: "expenses почему то null");
        
        var lastDate = DateTime.Now.AddDays(-3);
        var nowDate = DateTime.Now;
        var expensesLinq = 
            from expense in expenses
            where lastDate < expense.date && expense.date < nowDate
            orderby expense.date descending
            select expense;

        expenses = expensesLinq.ToList();
        
        var sb = new StringBuilder();
        sb.Append($"Ваши траты c {lastDate.ToString("dd.MM")} по {nowDate.ToString("dd.MM")}:\n\n");
        foreach (var expense in expenses)
        {
            sb.Append($"{expense.cost}  ");
            sb.Append($"{expense.name}  ");
            sb.Append($"[ {expense.expenseCategory} ]  ");
            sb.Append($"{expense.date.ToString("dd.MM")}\n");
        }

        return await _bot.SendTextMessageAsync(chatId: _tgUserId, replyMarkup: new ReplyKeyboardRemove(),
            text: sb.ToString());
    }
}