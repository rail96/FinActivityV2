using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FindActivity.Web.Models;

namespace FindActivity.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Activities");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult CommunityGuidelines()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
