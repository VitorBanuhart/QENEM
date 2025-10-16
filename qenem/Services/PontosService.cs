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

        public void PontosSimulado (ApplicationUser idUsuario, int TotalAcertos, int TotalQuestoes)
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
        }

        public void PontosQuestoes (String idUsuario)
        {
            var pontuacaoDoUsuario = _context.Pontos.FirstOrDefault(p => p.Usuario == idUsuario);

            if (pontuacaoDoUsuario != null)
            {
                pontuacaoDoUsuario.TotalPontuacao += 10;

            }
            else
            {
                var novaPontuacao = new Models.Pontos
                {
                    Usuario = idUsuario,
                    TotalPontuacao = 10
                };
                _context.Pontos.Add(novaPontuacao);
                _context.SaveChanges();
            }

            _context.SaveChanges();
        }
    }
}
