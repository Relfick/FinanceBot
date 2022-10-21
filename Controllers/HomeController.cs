using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using FinanceBot.Models;
using FinanceBot.Services;
using FinanceBot.Services.TgBot;


namespace FinanceBot.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationContext _db;
    
    private TelegramBotService TelegramBotService { get; set; }
    
    public HomeController(ApplicationContext applicationContext, TelegramBotService telegramBotService)
    {
        _db = applicationContext;
        TelegramBotService = telegramBotService;
    }

    public async Task<IActionResult> Index()
    {
        await TelegramBotService.StartBot();
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}