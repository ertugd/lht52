using Microsoft.AspNetCore.Mvc;
using istiklal_karacasu_lorawan.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace istiklal_karacasu_lorawan.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {

        }

        public async Task<IActionResult> Index()
        {
           return View();   
        }

        public async Task<IActionResult> GPS()
        {
            return View();
        }
    }
}
