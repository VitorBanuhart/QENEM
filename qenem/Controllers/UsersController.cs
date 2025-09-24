using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using qenem.ViewModels;
using System.Linq; // Necessário para o .Select()
using System.Threading.Tasks;

[Authorize] // Removido (Roles = "Admin") para que o usuário possa editar suas próprias áreas
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    // CORREÇÃO: O RoleManager gerencia IdentityRole, não AplicationUser.
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Formulário para o usuário LOGADO editar suas próprias áreas
    [HttpGet]
    public async Task<IActionResult> AreaInteresse()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario == null) return NotFound();

        var areasSelecionadas = await _context.UsuarioAreas
            .Where(ua => ua.Id_Usuario == usuario.Id)
            .Select(ua => ua.Id_AreaInteresse)
            .ToListAsync();



        // ▼▼▼ LÓGICA DE SEPARAÇÃO ADICIONADA AQUI ▼▼▼

        // 1. Define quais nomes consideramos como "linguagens". Você pode customizar esta lista.
        var nomesLinguagens = new List<string> { "ingles", "espanhol" };

        // 2. Busca todas as áreas do banco de uma só vez.
        var todasAsAreasDoBanco = await _context.AreasInteresse.OrderBy(a => a.NomeAreaInteresse).ToListAsync();

        // 3. Preenche o ViewModel com as listas separadas usando LINQ.
        var model = new UsuarioAreaViewModel
        {
            IdUsuario = usuario.Id,
            AreasSelecionadas = areasSelecionadas,
            // Filtra apenas as que são linguagens
            Linguagens = todasAsAreasDoBanco
                .Where(a => nomesLinguagens.Contains(a.NomeAreaInteresse))
                .ToList(),
            // Filtra apenas as que NÃO são linguagens
            Materias = todasAsAreasDoBanco
                .Where(a => !nomesLinguagens.Contains(a.NomeAreaInteresse))
                .ToList()
        };

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AreaInteresse(UsuarioAreaViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Se houver um erro de validação, recarregamos a lista de todas as áreas
            // e retornamos para a mesma tela para que o usuário possa corrigir.
            model.Mensagem = "Erro: Ocorreu um problema com os dados enviados.";
            return View("Index", model);
        }

        // Se o código chegou até aqui, o modelo é válido e podemos prosseguir.

        // Passo 2: Busca e remove todas as áreas de interesse antigas do usuário.
        var vinculosAntigos = await _context.UsuarioAreas
            .Where(ua => ua.Id_Usuario == model.IdUsuario)
            .ToListAsync();

        if (vinculosAntigos.Any())
        {
            _context.UsuarioAreas.RemoveRange(vinculosAntigos);
        }

        // Passo 3: Adiciona as novas áreas selecionadas.
        if (model.AreasSelecionadas != null && model.AreasSelecionadas.Any())
        {
            // Cria uma nova entidade UsuarioArea para cada ID de área selecionada.
            var novosVinculos = model.AreasSelecionadas.Select(areaId => new UsuarioArea
            {
                Id_Usuario = model.IdUsuario,
                Id_AreaInteresse = areaId
            });

            // Adiciona a coleção inteira de novos vínculos ao contexto.
            await _context.UsuarioAreas.AddRangeAsync(novosVinculos);
        }

        // Passo 4: Salva todas as alterações (remoções e adições) no banco de dados.
        await _context.SaveChangesAsync();

        model.Mensagem = "Suas áreas de interesse foram atualizadas com sucesso!";

        return RedirectToAction("Index", "ListarQuestoes");
    }
}