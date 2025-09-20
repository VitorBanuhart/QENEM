using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.DTO;
using qenem.Models;
using qenem.Services;

namespace qenem.Controllers
{
    public class SimuladoController : Controller
    {
        private readonly SimuladoService _simuladoService;
        private readonly QuestionService _questionService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public SimuladoController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SimuladoService simuladoService, QuestionService questionService)
        {
            _context = context;
            _userManager = userManager;
            _simuladoService = simuladoService;
            _questionService = questionService;
        }
        public async Task<IActionResult> Index()
        {
            var simulados = await ObterSimuladosUsuario();
            return View(simulados);
        }

        [HttpGet]
        public IActionResult ListarSimulados()
        {
            return View();
        }


        [HttpGet]
        public IActionResult CriarSimulado()
        {
            // Retorna a view para o formulário de criação do simulado
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken] 
        public async Task<IActionResult> CriarSimulado([FromBody] CriarSimuladoRequest data)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                var novoSimulado = await _simuladoService.CriarSimulado(data, user.Id);

                if (novoSimulado != null)
                {
                    return Json(new { success = true, simulado = new { Id = novoSimulado.Id } });
                }

                // Simulado nulo
                return Json(new { success = false, message = "Não foi possível criar o simulado." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocorreu um erro inesperado. Tente novamente." });
            }
        }
        /// <summary>
        /// carrega dados para a view do simulado com questao inicial ou atual do progresso salvo
        /// </summary>
        /// <param name="id"></param>
        /// <param name="questaoIndex"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> RealizaSimulado(int id, int questaoIndex = 0)
        { 
           
            var simulado = await _context.Simulados //buscar o simulado
                .Include(s => s.Respostas)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (simulado == null)
                return NotFound();

            if (simulado.Finalizado)
            {
                return RedirectToAction("Resultado", new { id = simulado.Id });
            }

            var questoesDoSimulado = await _simuladoService.ObterQuestoesSimulado(id);

            //TO DO:
            //entender a lógica para redirecionar p/ resultado
            if (questaoIndex < 0 || questaoIndex >= questoesDoSimulado.Count) 
            {
                //await _simuladoService.FinalizarSimulado(simulado.Id, /* tempoGasto */ null);
                return RedirectToAction("Resultado", new { id = simulado.Id });
            }

            var questaoAtual = questoesDoSimulado[questaoIndex];

            var respostasSalvas = (simulado.Respostas ?? new List<RespostaUsuario>())
                .Where(r => !string.IsNullOrEmpty(r.Resposta))
                .ToDictionary(r => r.QuestaoId, r => r.Resposta);

            var respostaUsuario = respostasSalvas.ContainsKey(questaoAtual.id) ? respostasSalvas[questaoAtual.id] : null;

            //monta objeto pra view
            // TO DO:
            // refatorar para ViewModel?
            // garantir/revisar dados enviados para model na view
            var dadosView = new
            {
                SimuladoId = simulado.Id,
                SimuladoNome = simulado.Nome,
                TotalQuestoes = questoesDoSimulado.Count,
                QuestaoAtualIndex = questaoIndex,
                QuestaoAtual = questaoAtual,
                RespostaUsuario = respostaUsuario,
                RespostasSalvas = respostasSalvas
            };

            return View(dadosView);
        }

        [HttpGet]
        public IActionResult Resultado()
        {
            return View();
        }

        private async Task<List<ListaQuestaoSimulado>> ObterSimuladosUsuario()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return new List<ListaQuestaoSimulado>(); // Retorna uma lista vazia se o usuário não existir.
            }

            var userId = usuario.Id;

            var simuladosExistentes = await _context.ListaSimulados
                .Where(s => s.Simulado.UsuarioId == userId)
                .ToListAsync();
            if (!simuladosExistentes.Any())
            {
                return new List<ListaQuestaoSimulado>();
            }
            return simuladosExistentes;
        }

        [HttpGet]
        public async Task<IActionResult> GetTempo(int id)
        {
            var tempo = await _simuladoService.GetTempoGastoAsync(id);

            // envia para o front em segundos
            return Json(new { seconds = (int)tempo.TotalSeconds });
        }

        [HttpPost]
        public async Task<IActionResult> SetTempo(int id, [FromBody] TempoDto dto)
        {
            if (dto == null || dto.Seconds < 0)
                return BadRequest("seconds inválido");

            var tempo = TimeSpan.FromSeconds(dto.Seconds);

            await _simuladoService.SetTempoGastoAsync(id, tempo);
            return Ok();
        }
    }
}
