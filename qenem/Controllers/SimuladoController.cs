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

        [HttpGet]
        public async Task<IActionResult> RealizaSimulado(int id, int questaoIndex = 0)
        {
            var simulado = await _simuladoService.ObterSimulado(id);
            if (simulado == null)
            {
                return NotFound();
            }

            // Carrega todas as questões
            var allQuestions = _questionService.GetAllQuestions();

            // Filtra pelos anos selecionados (se houver) e áreas de interesse
            var anosSelecionados = (simulado.AnosSelecionados != null && simulado.AnosSelecionados.Count > 0)
                ? simulado.AnosSelecionados
                : allQuestions.Select(q => q.year).Distinct().ToList();

            var areasSelecionadas = (simulado.AreasInteresse != null && simulado.AreasInteresse.Count > 0)
                ? simulado.AreasInteresse
                : allQuestions.Select(q => q.discipline).Distinct().ToList();

            // Filtra e embaralha as questões
            var questoesDoSimulado = allQuestions
                .Where(q => anosSelecionados.Contains(q.year) && areasSelecionadas.Contains(q.discipline)).OrderBy(q => Guid.NewGuid()) // Aleatoriza
                .Take(simulado.NumeroQuestoes)
                .ToList();

            if (questaoIndex < 0 || questaoIndex >= questoesDoSimulado.Count)
            {
                // Simulado terminado, vai para a página de resultados
                return RedirectToAction("Resultado", new { id = simulado.Id });
            }

            // Obtém todas as respostas do usuário para as bolinhas de progresso
            var respostasSalvas = (simulado.Respostas ?? new List<RespostaUsuario>())
                .Where(r => !string.IsNullOrEmpty(r.Resposta))
                .ToDictionary(r => r.QuestaoId, r => r.Resposta);

            // Busca a resposta do usuário para a questão atual
            var questaoAtual = questoesDoSimulado[questaoIndex];
            var respostaUsuario = respostasSalvas.ContainsKey(questaoAtual.id) ? respostasSalvas[questaoAtual.id] : null;

            // Monta objeto para a View
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

        private async Task<List<ListaSimulado>> ObterSimuladosUsuario()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return new List<ListaSimulado>(); // Retorna uma lista vazia se o usuário não existir.
            }

            var userId = usuario.Id;

            var simuladosExistentes = await _context.ListaSimulados
                .Where(s => s.Simulado.UsuarioId == userId)
                .ToListAsync();
            if (!simuladosExistentes.Any())
            {
                return new List<ListaSimulado>();
            }
            return simuladosExistentes;
        }
    }
}
