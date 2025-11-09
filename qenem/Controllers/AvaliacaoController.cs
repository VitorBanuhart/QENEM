using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using qenem.Data;
using qenem.Models;
using qenem.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace qenem.Controllers
{
    [Authorize]
    public class AvaliacaoController : Controller
    {
        private readonly AvaliacaoService _avaliacaoService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly QuestionService _questionService;

        public AvaliacaoController(AvaliacaoService avaliacaoService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, QuestionService questionService)
        {
            _avaliacaoService = avaliacaoService;
            _userManager = userManager;
            _context = context;
            _questionService = questionService;
        }

        
        public class AvaliacaoDto
        {
            public string Usuario { get; set; }
            public string QuestaoId { get; set; }
            public int Avaliacao { get; set; } 
        }

        // POST: /avaliacao/salvar
        [HttpPost]
        public async Task<IActionResult> Salvar([FromBody] AvaliacaoDto dto)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (dto == null)
                return BadRequest(new { success = false, message = "Payload inválido." });

            if (dto.Avaliacao < 1 || dto.Avaliacao > 3)
                return BadRequest(new { success = false, message = "Avaliacao fora do range permitido." });

            string questaoId;
            int questaoIdInt;
            if (int.TryParse(dto.QuestaoId, out questaoIdInt))
            {
                questaoId = dto.QuestaoId;
            }
            else
            {
                var question = _questionService.GetByUniqueId(dto.QuestaoId);
                if (question == null)
                    return BadRequest(new { success = false, message = "Questão não encontrada para o QuestaoId enviado." });

                questaoId = question.UniqueId;
            }

            try
            {
                await _avaliacaoService.SalvarOuAtualizarAvaliacaoAsync(new AvaliacaoService.AvaliacaoDto
                {
                    Usuario = usuarioId,
                    QuestaoId = questaoId,
                    Avaliacao = dto.Avaliacao
                });
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // registrar/logar ex.Message conforme logger do projeto
                return StatusCode(500, new { success = false, message = "Erro interno ao salvar", detail = ex.Message });
            }
        }

        // GET: avaliacao/verificar/{questaoId}
        [HttpGet]
        public async Task<IActionResult> Verificar([FromQuery] string questaoPath)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(questaoPath))
                return BadRequest(new { success = false, message = "questaoPath ausente" });

            // Resolve questaoPath -> UniqueId, quando necessário
            string questaoId = questaoPath;
            if (!int.TryParse(questaoPath, out _))
            {
                var question = _questionService.GetByUniqueId(questaoPath);
                if (question == null)
                    return NotFound(new { success = false, message = "Questão não encontrada para o caminho informado." });

                questaoId = question.UniqueId;
            }

            // Aguardamos o resultado do service corretamente
            var avaliacaoResult = await _avaliacaoService.VerificarAvaliacaoAsync(usuarioId, questaoId);

            // Retornamos apenas os dados que o JS precisa (avaliacao como inteiro / null)
            return Ok(new
            {
                success = avaliacaoResult?.Success ?? false,
                avaliacao = avaliacaoResult?.Avaliacao, // será int? ou null
                message = avaliacaoResult?.Message
            });
        }
    }
}