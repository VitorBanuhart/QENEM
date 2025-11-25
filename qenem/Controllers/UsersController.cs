using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using qenem.ViewModels;
using System.Linq;
using System.Threading.Tasks;

[Authorize] 
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> AreaInteresse()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario == null) return NotFound();

        var areasSelecionadas = await _context.UsuarioAreas
            .Where(ua => ua.Id_Usuario == usuario.Id)
            .Select(ua => ua.Id_AreaInteresse)
            .ToListAsync();
        var nomesLinguagens = new List<string> { "ingles", "espanhol" };

        var todasAsAreasDoBanco = await _context.AreasInteresse.OrderBy(a => a.NomeAreaInteresse).ToListAsync();

        var model = new UsuarioAreaViewModel
        {
            IdUsuario = usuario.Id,
            AreasSelecionadas = areasSelecionadas,
            Linguagens = todasAsAreasDoBanco
                .Where(a => nomesLinguagens.Contains(a.NomeAreaInteresse))
                .ToList(),
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
            model.Mensagem = "Erro: Ocorreu um problema com os dados enviados.";
            return View("Index", model);
        }

        var vinculosAntigos = await _context.UsuarioAreas
            .Where(ua => ua.Id_Usuario == model.IdUsuario)
            .ToListAsync();

        if (vinculosAntigos.Any())
        {
            _context.UsuarioAreas.RemoveRange(vinculosAntigos);
        }

        if (model.AreasSelecionadas != null && model.AreasSelecionadas.Any())
        {
            var novosVinculos = model.AreasSelecionadas.Select(areaId => new UsuarioArea
            {
                Id_Usuario = model.IdUsuario,
                Id_AreaInteresse = areaId
            });

            await _context.UsuarioAreas.AddRangeAsync(novosVinculos);
        }

        await _context.SaveChangesAsync();

        model.Mensagem = "Suas áreas de interesse foram atualizadas com sucesso!";

        return RedirectToAction("Index", "ListarQuestoes");
    }
}