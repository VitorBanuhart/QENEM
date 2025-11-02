using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using qenem.Data;
using qenem.Models;
using qenem.Services;
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
            var usuarioId = User?.Identity?.Name;
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
        public IActionResult Verificar([FromQuery] string questaoPath)
        {
            var usuarioId = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(questaoPath))
                return BadRequest(new { success = false, message = "questaoPath ausente" });

            // Tentar converter para int se, por acaso, foi enviado um ID numérico
            if (int.TryParse(questaoPath, out int questaoIdNumeric))
            {
                var avaliacao = _avaliacaoService.VerificarAvaliacaoAsync(usuarioId, questaoPath); // adapte para seu método
                return Ok(new { success = true, avaliacao = avaliacao });
            }

            // Caso contrário, resolver usando o serviço que carrega JSON (UniqueId -> Question)
            var question = _questionService.GetByUniqueId(questaoPath); // **implemente/adapte este método se necessário**
            if (question == null)
                return NotFound(new { success = false, message = "Questão não encontrada para o caminho informado." });

            var avaliacaoPorQuestao = _avaliacaoService.VerificarAvaliacaoAsync(usuarioId, question.UniqueId); // adapte nome do método
            return Ok(new { success = true, avaliacao = avaliacaoPorQuestao });
        }
    }
}