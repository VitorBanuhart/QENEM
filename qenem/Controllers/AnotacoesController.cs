using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using qenem.Services;

namespace qenem.Controllers
{
    [Authorize]
    public class AnotacoesController : Controller
    {
        private readonly AnotacoesService _service;
        private readonly ILogger<AnotacoesController> _logger;

        public AnotacoesController(AnotacoesService service, ILogger<AnotacoesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET /Anotacoes/Get
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Get Anotacoes: usuário não autenticado.");
                    return Unauthorized(new { error = "Usuário não autenticado." });
                }

                var anot = await _service.GetByUserAsync(userId);
                return Json(new { anotacoes = anot?.AnotacoesUsuario ?? string.Empty });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro em AnotacoesController.Get");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /Anotacoes/Save
        // body: { "anotacoes": "texto..." }
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Save Anotacoes: usuário não autenticado.");
                    return Unauthorized(new { error = "Usuário não autenticado." });
                }

                await _service.SaveAsync(userId, dto?.Anotacoes ?? string.Empty);
                return Json(new { success = true });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro em AnotacoesController.Save");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class SaveDto
        {
            public string Anotacoes { get; set; }
        }
    }
}