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

        public bool PontosSimulado (ApplicationUser idUsuario)
        {
            var pontuacaoDoUsuario = _context.Pontos.FirstOrDefault(p => p.Usuario == idUsuario.Id);

            if (pontuacaoDoUsuario != null)
            {
                pontuacaoDoUsuario.TotalPontuacao += 100;
            }
            else
            {
                var novaPontuacao = new Models.Pontos
                {
                    Usuario = idUsuario.Id,
                    TotalPontuacao = 100
                };
                _context.Pontos.Add(novaPontuacao);
            }
            _context.SaveChanges();
            return true;
        }
    }
}
