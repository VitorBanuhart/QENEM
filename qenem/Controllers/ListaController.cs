// Controllers/ListaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using System.Security.Claims;

namespace qenem.Controllers
{
    [Authorize] // Apenas usuários logados podem acessar
    public class ListaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Limites definidos pelos requisitos
        private const int MAX_LISTAS_POR_USUARIO = 10;
        private const int MAX_QUESTOES_POR_LISTA = 180;

        public ListaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Lista (Página principal que mostra as listas do usuário)
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listasDoUsuario = await _context.Listas
                .Where(l => l.UsuarioId == userId)
                .OrderBy(l => l.Nome)
                .ToListAsync();

            return View(listasDoUsuario);
        }

        // POST: /Lista/Criar
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ListaCreateModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Nome) || model.Nome.Length > 30)
            {
                return BadRequest(new { success = false, message = "O nome da lista é inválido ou excede 30 caracteres." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalListas = await _context.Listas.CountAsync(l => l.UsuarioId == userId);
            if (totalListas >= MAX_LISTAS_POR_USUARIO) // RNF 4.3 e 4.4
            {
                return BadRequest(new { success = false, message = "msg_maximo_lista" }); // Mensagem de erro do requisito
            }

            var novaLista = new Lista
            {
                Nome = model.Nome,
                UsuarioId = userId
            };

            _context.Listas.Add(novaLista);
            await _context.SaveChangesAsync();

            // Retorna a lista criada para que o front-end possa atualizar a UI
            return Json(new { success = true, lista = novaLista });
        }

        // POST: /Lista/AdicionarQuestao
        [HttpPost]
        public async Task<IActionResult> AdicionarQuestao([FromBody] AdicionarQuestaoModel model)
        {
            if (model == null || model.ListaId <= 0 || string.IsNullOrEmpty(model.QuestaoId))
            {
                return BadRequest(new { success = false, message = "Dados inválidos." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == model.ListaId && l.UsuarioId == userId);

            if (lista == null)
            {
                return NotFound(new { success = false, message = "Lista não encontrada ou não pertence ao usuário." });
            }

            // RNF 4.2 - Verifica o limite de questões
            if (lista.ListaQuestoes.Count >= MAX_QUESTOES_POR_LISTA)
            {
                return BadRequest(new { success = false, message = "Esta lista já atingiu o limite de 180 questões." });
            }

            // Verifica se a questão já foi adicionada
            if (lista.ListaQuestoes.Any(q => q.QuestaoId == model.QuestaoId))
            {
                return BadRequest(new { success = false, message = "Esta questão já foi adicionada a esta lista." });
            }

            var novaQuestaoNaLista = new ListaQuestao
            {
                ListaId = model.ListaId,
                QuestaoId = model.QuestaoId
            };

            _context.ListaQuestoes.Add(novaQuestaoNaLista);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Questão adicionada com sucesso!" });
        }

        // POST: /Lista/Excluir/{id}
        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId);

            if (lista == null)
            {
                return NotFound(new { success = false, message = "Lista não encontrada." });
            }

            _context.Listas.Remove(lista); // O EF Core removerá em cascata os ListaQuestoes associados
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Lista excluída com sucesso." });
        }

        [HttpGet]
        public async Task<IActionResult> ObterListasDoUsuario()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listas = await _context.Listas
                .Where(l => l.UsuarioId == userId)
                .Select(l => new { l.Id, l.Nome }) // Seleciona apenas os dados necessários
                .ToListAsync();

            return Json(listas);
        }
    }

    // Modelos auxiliares para receber dados do front-end
    public class ListaCreateModel { public string Nome { get; set; } }
    public class AdicionarQuestaoModel { public int ListaId { get; set; } public string QuestaoId { get; set; } }
}