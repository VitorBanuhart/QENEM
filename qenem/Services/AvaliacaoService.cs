using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using System;
using System.Threading.Tasks;

namespace qenem.Services
{
    public class AvaliacaoService
    {
        private readonly ApplicationDbContext _context;

        public AvaliacaoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // DTOs
        public class AvaliacaoDto
        {
            public string Usuario { get; set; }
            public string QuestaoId { get; set; }
            public int Avaliacao { get; set; }
        }

        public class AvaliacaoResultDto
        {
            public bool Success { get; set; }
            public int? Avaliacao { get; set; }
            public string Message { get; set; }
        }

        /// Insere ou atualiza a avaliação para o par usuario+questao.
        /// Se existir, atualiza; se não, insere.

        public async Task<AvaliacaoResultDto> SalvarOuAtualizarAvaliacaoAsync(AvaliacaoDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Usuario) || string.IsNullOrWhiteSpace(dto.QuestaoId))
            {
                return new AvaliacaoResultDto
                {
                    Success = false,
                    Avaliacao = null,
                    Message = "Usuário e QuestaoId são obrigatórios."
                };
            }

            // Procura entidade
            var existente = await _context.AvaliarQuestoes
                .FirstOrDefaultAsync(a => a.Usuario == dto.Usuario && a.QuestaoId == dto.QuestaoId);

            if (existente != null)
            {
                // atualiza
                existente.Avaliacao = dto.Avaliacao;
                // sem .Update() necessário porque a entidade já está sendo rastreada
                try
                {
                    await _context.SaveChangesAsync();
                } catch (Exception e)
                {
                    Console.WriteLine($"Ocorreu um erro inesperado: {e.Message}");

                }

                return new AvaliacaoResultDto
                {
                    Success = true,
                    Avaliacao = existente.Avaliacao,
                    Message = "Avaliação atualizada com sucesso."
                };
            }

            // cria nova avaliação
            var nova = new AvaliaQuestao
            {
                Usuario = dto.Usuario,
                QuestaoId = dto.QuestaoId,
                Avaliacao = dto.Avaliacao
            };

            try
            {
                _context.AvaliarQuestoes.Add(nova);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Este é o erro mais provável vindo do Entity Framework ao salvar.
                // Você pode inspecionar 'ex.InnerException' para obter detalhes mais específicos do banco de dados.
                Console.WriteLine($"Ocorreu um erro ao salvar no banco de dados: {ex.Message}");

                // Se quiser ver a exceção interna (geralmente mais útil para depuração):
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detalhe do erro (InnerException): {ex.InnerException.Message}");
                }

                // TODO: Adicione aqui a lógica para lidar com o erro
                // (Ex: retornar uma mensagem de erro para o usuário, fazer rollback de algo, etc.)
            }
            catch (Exception ex)
            {
                // Captura qualquer outro erro inesperado que possa ocorrer.
                Console.WriteLine($"Ocorreu um erro inesperado: {ex.Message}");

                //
            }

            return new AvaliacaoResultDto
                {
                    Success = true,
                    Avaliacao = nova.Avaliacao,
                    Message = "Avaliação salva com sucesso."
                };
            
        }

        // Verifica se o usuário já avaliou a questão (retorna DTO).
        public async Task<AvaliacaoResultDto> VerificarAvaliacaoAsync(string usuario, string questaoId)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(questaoId))
            {
                return new AvaliacaoResultDto
                {
                    Success = false,
                    Avaliacao = null,
                    Message = "Usuário e QuestaoId são obrigatórios."
                };
            }

            var avaliacao = await _context.AvaliarQuestoes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Usuario == usuario && a.QuestaoId == questaoId);

            if (avaliacao != null)
            {
                return new AvaliacaoResultDto
                {
                    Success = true,
                    Avaliacao = avaliacao.Avaliacao,
                    Message = "Avaliação encontrada."
                };
            }

            return new AvaliacaoResultDto
            {
                Success = false,
                Avaliacao = null,
                Message = "Nenhuma avaliação encontrada para esse usuário/questão."
            };
        }
    }
}