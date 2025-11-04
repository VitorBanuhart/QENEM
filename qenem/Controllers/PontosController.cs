using Microsoft.AspNetCore.Mvc;
using qenem.Data;
using qenem.Services;

namespace qenem.Controllers
{
    public class PontosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PontosService _pontosService;


        public PontosController(ApplicationDbContext context, PontosService pontosService)
        {
            _context = context; // O DbContext é injetado aqui
            _pontosService = pontosService; // O serviço é injetado e atribuído aqui
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult PontosQuestoes (String idUsuario)
        {
            _pontosService.PontosQuestoes(idUsuario);

            return Ok(new { success = true, message = "Pontuação adicionada com sucesso!" });
        }
    }
}
