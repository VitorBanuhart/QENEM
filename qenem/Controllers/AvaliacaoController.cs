using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using qenem.Models;
using qenem.Services;
using System.Threading.Tasks;

namespace qenem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvaliacaoController : ControllerBase
    {
        private readonly AvaliacaoService _avaliacaoService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AvaliacaoController(AvaliacaoService avaliacaoService, UserManager<ApplicationUser> userManager)
        {
            _avaliacaoService = avaliacaoService;
            _userManager = userManager;
        }

        // POST: api/avaliacao/salvar
        [HttpPost("salvar")]
        public async Task<IActionResult> SalvarAvaliacao([FromBody] AvaliacaoService.AvaliacaoDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Usuário não autenticado." });
            }

            // Garante que a avaliação seja salva para o usuário logado
            dto.Usuario = user.Id;

            var result = await _avaliacaoService.SalvarOuAtualizarAvaliacaoAsync(dto);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        // GET: api/avaliacao/verificar/{questaoId}
        [HttpGet("verificar/{questaoId}")]
        public async Task<IActionResult> VerificarAvaliacao(string questaoId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Usuário não autenticado." });
            }

            var result = await _avaliacaoService.VerificarAvaliacaoAsync(user.Id, questaoId);

            return Ok(result);
        }
    }
}