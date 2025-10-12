using qenem.Data;
using qenem.Models;

namespace qenem.Services
{
    public class PontosService
    {
        private readonly ApplicationDbContext _context;

        public PontosService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool PontosSimulado (ApplicationUser idUsuario, int TotalAcertos, int TotalQuestoes)
        {
            var pontuacaoDoUsuario = _context.Pontos.FirstOrDefault(p => p.Usuario == idUsuario.Id);

            if (pontuacaoDoUsuario != null)
            {
                pontuacaoDoUsuario.TotalPontuacao += 10 * TotalAcertos;

            }
            else
            {
                var novaPontuacao = new Models.Pontos
                {
                    Usuario = idUsuario.Id,
                    TotalPontuacao = 10 * TotalAcertos
                };
                _context.Pontos.Add(novaPontuacao);
                _context.SaveChanges();
            }

            if (TotalAcertos == TotalQuestoes)
            {
                pontuacaoDoUsuario.TotalPontuacao += 50;
            }

            _context.SaveChanges();
            return true;
        }
    }
}
