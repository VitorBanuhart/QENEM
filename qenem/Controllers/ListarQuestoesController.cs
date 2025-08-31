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
        public IActionResult Index()
        {
            var questoesParaTeste = GerarQuestoesMock();
            return View(questoesParaTeste);
        }

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private QuestionService _questionService;

        public ListarQuestoesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, QuestionService questionService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _questionService = questionService;
        }

        // Método privado para criar uma lista de questões para teste
        private async Task<List<Question>> GerarQuestoesMock()
        {
            var lista = new List<Question>();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var usuario = await _userManager.GetUserAsync(User);

            var areasSelecionadasInt = await _context.UsuarioAreas
                .Where(ua => ua.Id_Usuario == usuario.Id)
                .Select(ua => ua.Id_AreaInteresse)
                .ToListAsync();

            var aresSelecionasString = await _context.AreasInteresse
                .Where(ua => ua.IdAreaInteresse == areasSelecionadasInt.ToList().Count)
                .Select(ua => ua.NomeAreaInteresse).ToListAsync();


            var questions = _questionService.GetRandomQuestions(aresSelecionasString, userId);


            //for (int i = 1; i <= 10; i++)
            //{
            //    lista.Add(questions[i]);
                
            //}

            return questions;
        }
    }
}