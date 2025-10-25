using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lht52.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}