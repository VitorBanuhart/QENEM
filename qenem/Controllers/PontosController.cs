using Microsoft.AspNetCore.Mvc;
using qenem.Data;

namespace qenem.Controllers
{
    public class PontosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult PontosSimulado(String idUsuario)
        {
            var pontuacaoDoUsuario = _context.Pontos.FirstOrDefault(p => p.Usuario == idUsuario);

            if (pontuacaoDoUsuario != null)
            {
                pontuacaoDoUsuario.TotalPontuacao += 100;
            }
            else
            {
                var novaPontuacao = new Models.Pontos
                {
                    Usuario = idUsuario,
                    TotalPontuacao = 100
                };
            }
            _context.SaveChanges();

            return Ok(new { success = true, message = "Pontuação adicionada com sucesso!" });
        }
    }
}
