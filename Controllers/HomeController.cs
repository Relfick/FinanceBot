using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FinanceBot.Models;
using FinanceBot.Services;
using Telegram.Bot;

namespace FinanceBot.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index(TelegramBotService tgBotService)
    {
        var client = tgBotService.Client;
        
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