// Controllers/ListaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using qenem.Services;
using System.Security.Claims;

namespace qenem.Controllers
{
    [Authorize] // Apenas usuários logados podem acessar
    public class ListaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QuestionService _questionService;

        // Limites definidos pelos requisitos
        private const int MAX_LISTAS_POR_USUARIO = 10;
        private const int MAX_QUESTOES_POR_LISTA = 180;

        public ListaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, QuestionService questionService)
        {
            _context = context;
            _userManager = userManager;
            _questionService = questionService;
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

            Console.WriteLine("ESTOU AQUI");

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

        // GET /Lista/Questoes?id=123  -> carrega view com as questões da lista
        public async Task<IActionResult> Questoes(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId);

            if (lista == null) return NotFound();

            List<Question> todasQuestoes;
            try
            {
                todasQuestoes = _questionService.GetAllQuestions();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao carregar questões do disco: {ex.Message}");
            }

            // Normaliza os IDs armazenados no banco (pode já ser full path; Path.GetFullPath também lida com caminhos "limpos")
            var questaoIdsNaListaNormalized = lista.ListaQuestoes
                .Select(lq => {
                    try { return Path.GetFullPath(lq.QuestaoId).Trim(); }
                    catch { return lq.QuestaoId?.Trim() ?? ""; }
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var todasQuestoesByFullNormalized = todasQuestoes.ToDictionary(
                q => {
                    try { return Path.GetFullPath(q.UniqueId).Trim(); }
                    catch { return q.UniqueId?.Trim() ?? ""; }
                },
                StringComparer.OrdinalIgnoreCase);

            var ordenadas = new List<Question>();

            foreach (var qidNormalized in questaoIdsNaListaNormalized)
            {
                // 1) tenta encontrar por caminho normalizado (case-insensitive)
                if (todasQuestoesByFullNormalized.TryGetValue(qidNormalized, out var qMatch))
                {
                    ordenadas.Add(qMatch);
                    continue;
                }

                // 2) fallback: tentar comparar apenas pelo nome do arquivo (ex: questionsdetails.json)
                var fileName = Path.GetFileName(qidNormalized);
                var fallback = todasQuestoes.FirstOrDefault(q =>
                    string.Equals(Path.GetFileName(q.UniqueId), fileName, StringComparison.OrdinalIgnoreCase));
                if (fallback != null)
                {
                    ordenadas.Add(fallback);
                    continue;
                }

                // 3) (opcional) procurar substrings - útil se o DB armazenou caminhos com drivers diferentes ou prefixos
                var partial = todasQuestoes.FirstOrDefault(q =>
                    q.UniqueId != null && q.UniqueId.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                if (partial != null)
                {
                    ordenadas.Add(partial);
                    continue;
                }

                // se não achar nada, ignora essa entrada (ou você pode logar)
                // aqui você pode logar: Logger.LogWarning($"Questão {qidNormalized} não encontrada no JSON.");
            }

            ViewBag.ListaId = id;
            ViewBag.NomeLista = lista.Nome;

            return View("MostrarQuestoes", ordenadas);
        }


        // GET JSON: /Lista/ObterQuestoesDaLista?id=123
        [HttpGet]
        public async Task<IActionResult> ObterQuestoesDaLista(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId);

            if (lista == null) return NotFound(new { success = false, message = "Lista não encontrada." });

            List<Question> todasQuestoes;
            try
            {
                todasQuestoes = _questionService.GetAllQuestions();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Erro ao carregar questões.", details = ex.Message });
            }

            var questaoIdsNaLista = lista.ListaQuestoes.Select(lq => lq.QuestaoId).ToList();

            var ordenadas = questaoIdsNaLista
                .Select(qid => todasQuestoes.FirstOrDefault(q => q.UniqueId == qid))
                .Where(q => q != null)
                .Select(q => new
                {
                    uniqueId = q.UniqueId,
                    title = q.title,
                    year = q.year,
                    discipline = q.discipline,
                    language = q.language,
                    context = q.context,
                    alternativesIntroduction = q.alternativesIntroduction,
                    correctAlternative = q.correctAlternative,
                    alternatives = q.alternatives?.Select(a => new { letter = a.letter, text = a.text }).ToList()
                })
                .ToList();

            return Json(new { success = true, questoes = ordenadas });
        }

        // POST: RemoverQuestaoDaLista { ListaId, QuestaoId }
        [HttpPost]
        public async Task<IActionResult> RemoverQuestaoDaLista([FromBody] RemoverQuestaoDto dto)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Dados inválidos." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == dto.ListaId && l.UsuarioId == userId);
            if (lista == null) return NotFound(new { success = false, message = "Lista não encontrada." });

            var rel = await _context.ListaQuestoes.FirstOrDefaultAsync(lq => lq.ListaId == dto.ListaId && lq.QuestaoId == dto.QuestaoId);
            if (rel == null) return NotFound(new { success = false, message = "Questão não encontrada nessa lista." });

            _context.ListaQuestoes.Remove(rel);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Questão removida da lista." });
        }

        [HttpPost]
        public async Task<IActionResult> RenomearLista([FromBody] RenomearListaDto dto)
        {
            if (dto == null || dto.ListaId <= 0 || string.IsNullOrWhiteSpace(dto.NovoNome) || dto.NovoNome.Length > 30)
            {
                return BadRequest(new { success = false, message = "Dados inválidos ou nome excede 30 caracteres." });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Usuário não autenticado." });
            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == dto.ListaId && l.UsuarioId == userId);
            if (lista == null) return NotFound(new { success = false, message = "Lista não encontrada." });
            lista.Nome = dto.NovoNome;
            _context.Listas.Update(lista);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lista renomeada com sucesso.", novoNome = dto.NovoNome });
        }
    }
    public class RemoverQuestaoDto { public int ListaId { get; set; } public string QuestaoId { get; set; } = ""; }
    public class CriarListaDto { public string Nome { get; set; } = ""; }

    // Modelos auxiliares para receber dados do front-end
    public class ListaCreateModel { public string Nome { get; set; } }
    public class AdicionarQuestaoModel { public int ListaId { get; set; } public string QuestaoId { get; set; } }
    public class RenomearListaDto { public int ListaId { get; set; } public string NovoNome { get; set; } }
}