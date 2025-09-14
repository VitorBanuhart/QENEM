using Microsoft.AspNetCore.Mvc;

namespace qenem.Controllers
{
    public class SimuladoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CriarSimulado()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RealizaSimulado()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Resultado()
        {
            return View();
        }
    }
}
