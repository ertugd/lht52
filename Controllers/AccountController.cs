using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace istiklal_karacasu_lorawan.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /Account/Login
        [HttpGet]
        public async Task<IActionResult> LoginAsync(string returnUrl = "/")
        {         
            var props = new LoginAuthenticationPropertiesBuilder()
                      .WithRedirectUri("/callback") // callback endpoint'i
                      .Build();         
            
            return Challenge(props, Auth0Constants.AuthenticationScheme);
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // 1. ASP.NET Core cookie oturumunu kapat
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Auth0 logout URL
            var auth0Domain = _configuration["Auth0:Domain"];
            var clientId = _configuration["Auth0:ClientId"];
            var returnTo = Url.Action("LoggedOut", "Account", null, Request.Scheme);

            var logoutUrl = $"https://{auth0Domain}/v2/logout?client_id={clientId}&returnTo={returnTo}";

            // 3. Tarayıcıyı Auth0 logout sayfasına yönlendir
            return Redirect(logoutUrl);
        }

        // GET: /Account/LoggedOut
        [HttpGet]
        public IActionResult LoggedOut()
        {
            return View("LogoutPage");
        }      
    }
}
