using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using qenem.Models;

namespace qenem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
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
        
        [HttpGet]
        public IActionResult ToggleTheme(string theme)
        {
            Response.Cookies.Append("Theme", theme, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
