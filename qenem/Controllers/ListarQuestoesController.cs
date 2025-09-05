using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using qenem.Services;
using System.Security.Claims;

namespace qenem.Controllers
{
    public class ListarQuestoesController : Controller
    {
        private readonly QuestionService _questionService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ListarQuestoesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            QuestionService questionService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _questionService = questionService;
        }

        private const string QuestoesExibidasKey = "QuestoesExibidas";

        // Página inicial chama a versão interna que retorna lista
        public async Task<IActionResult> Index()
        {
            var questoesParaTeste = await GerarQuestoesMockInterno();
            return View(questoesParaTeste);
        }

        // 🔹 Versão interna para uso do Index (não é endpoint)
        private async Task<List<Question>> GerarQuestoesMockInterno()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _userManager.GetUserAsync(User);

            var areasSelecionadasInt = await _context.UsuarioAreas
                .Where(ua => ua.Id_Usuario == usuario.Id)
                .Select(ua => ua.Id_AreaInteresse)
                .ToListAsync();

            var areasSelecionadasString = await _context.AreasInteresse
                .Where(ai => areasSelecionadasInt.Contains(ai.IdAreaInteresse))
                .Select(ai => ai.NomeAreaInteresse)
                .ToListAsync();

            var idiomasSelecionados = new List<string>();

            if (areasSelecionadasString.Contains("ingles"))
            {
                areasSelecionadasString.Remove("ingles");
                idiomasSelecionados.Add("ingles");
            }
            if (areasSelecionadasString.Contains("espanhol"))
            {
                areasSelecionadasString.Remove("espanhol");
                idiomasSelecionados.Add("espanhol");
            }

            return _questionService.GetRandomQuestions(areasSelecionadasString, idiomasSelecionados, userId);
        }

        // 🔹 Endpoint público para AJAX (fetch)
        [HttpPost]
        public async Task<IActionResult> GerarQuestoesMock()
        {
            var questoes = await GerarQuestoesMockInterno();
            return Json(questoes); // retorna JSON para o JavaScript
        }
    }
}