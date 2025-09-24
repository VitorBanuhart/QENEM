using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using qenem.Services;
using System.Security.Claims;
using Markdig; // Certifique-se de que este 'using' está presente

namespace qenem.Controllers
{
    [Authorize] // É uma boa prática proteger o controller para garantir que o usuário esteja logado
    public class ListarQuestoesController : Controller
    {
        private readonly QuestionService _questionService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // O RoleManager não estava sendo usado, então pode ser removido do construtor se não for necessário em outros métodos.
        public ListarQuestoesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            QuestionService questionService)
        {
            _context = context;
            _userManager = userManager;
            _questionService = questionService;
        }

        // GET: /ListarQuestoes/
        public async Task<IActionResult> Index()
        {
            var questoes = await GerarQuestoesMockInterno();
            return View(questoes);
        }

        // POST: /ListarQuestoes/GerarQuestoesMock
        /// <summary>
        /// Endpoint público para AJAX (fetch).
        /// MELHORIA: Agora converte o Markdown para HTML no servidor antes de enviar o JSON.
        /// Isso aumenta a segurança e simplifica o código do front-end.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GerarQuestoesMock()
        {
            var questoes = await GerarQuestoesMockInterno();

            // Transforma os dados para um formato seguro para o front-end,
            // já convertendo o Markdown para HTML no servidor.
            var resultadoProcessado = questoes.Select(q => new
            {
                q.UniqueId,
                q.title,
                q.year,
                q.discipline,
                q.language,
                q.correctAlternative,
                // Converte o Markdown para HTML aqui
                context = Markdown.ToHtml(q.context ?? string.Empty),
                alternativesIntroduction = Markdown.ToHtml(q.alternativesIntroduction ?? string.Empty),
                alternatives = q.alternatives.Select(alt => new { alt.letter, alt.text })
            });

            return Json(resultadoProcessado);
        }

        /// <summary>
        /// Método interno que busca as preferências do usuário e obtém as questões do serviço.
        /// </summary>
        private async Task<List<Question>> GerarQuestoesMockInterno()
        {
            var usuario = await _userManager.GetUserAsync(User);

            // MELHORIA: Adicionada verificação para evitar erro se o usuário não for encontrado.
            if (usuario == null)
            {
                return new List<Question>(); // Retorna uma lista vazia se o usuário não existir.
            }

            var userId = usuario.Id;

            var areasSelecionadas = await _context.UsuarioAreas
                .Where(ua => ua.Id_Usuario == userId)
                .Select(ua => ua.AreaInteresse.NomeAreaInteresse)
                .ToListAsync();

            if (!areasSelecionadas.Any())
            {
                return new List<Question>(); // Retorna lista vazia se o usuário não tiver áreas de interesse.
            }

            // MELHORIA: Lógica otimizada para separar idiomas das disciplinas.
            var idiomasSelecionados = new List<string>();
            var disciplinasSelecionadas = new List<string>();

            foreach (var area in areasSelecionadas)
            {
                if (string.Equals(area, "ingles", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(area, "espanhol", StringComparison.OrdinalIgnoreCase))
                {
                    idiomasSelecionados.Add(area.ToLower());
                }
                else
                {
                    disciplinasSelecionadas.Add(area);
                }
            }

            return _questionService.GetRandomQuestions(disciplinasSelecionadas, idiomasSelecionados, userId);
        }
    }
}