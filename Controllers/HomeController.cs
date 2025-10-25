using Microsoft.AspNetCore.Mvc;

namespace lht52.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}