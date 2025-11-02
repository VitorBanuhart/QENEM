using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using qenem.Data;
using qenem.Models;
using System;
using System.Threading.Tasks;

namespace qenem.Services
{
    public class AnotacoesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnotacoesService> _logger;

        public AnotacoesService(ApplicationDbContext context, ILogger<AnotacoesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // retorna as anotações do usuário (ou null se não existir)
        public async Task<Anotacoes> GetByUserAsync(string usuarioId)
        {
            try
            {
                return await _context.Anotacoes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar anotações do usuário {UserId}", usuarioId);
                throw;
            }
        }

        // cria ou atualiza as anotações do usuário
        public async Task<Anotacoes> SaveAsync(string usuarioId, string texto)
        {
            try
            {
                var existing = await _context.Anotacoes.FirstOrDefaultAsync(a => a.UsuarioId == usuarioId);

                if (existing == null)
                {
                    existing = new Anotacoes
                    {
                        UsuarioId = usuarioId,
                        AnotacoesUsuario = texto
                    };
                    _context.Anotacoes.Add(existing);
                }
                else
                {
                    existing.AnotacoesUsuario = texto;
                    _context.Anotacoes.Update(existing);
                }

                await _context.SaveChangesAsync();
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar anotações do usuário {UserId}", usuarioId);
                throw;
            }
        }
    }
}