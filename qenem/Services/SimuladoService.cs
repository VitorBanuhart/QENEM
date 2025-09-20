using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.DTO;
using qenem.Models;

namespace qenem.Services
{
    public class SimuladoService
    {
        private readonly QuestionService _questionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SimuladoService> _logger;

        public SimuladoService(
            QuestionService questionService,
            ApplicationDbContext context,
            ILogger<SimuladoService> logger)
        {
            _questionService = questionService;
            _context = context;
            _logger = logger;
        }

        // TO DO:
        // verificar se simulado está em andamento ou finalizado
        // se em andamento, carregar questões e respostas
        // se finalizado, carregar estatísticas (Resultado)

        //public async Task<Simulado?> ObterSimulado(int id) //ao acessar um simulado no historico
        //{
        //}
        public async Task<List<Simulado>> ListarSimulados(string userId)
        {
            return await _context.Simulados
                .Where(s => s.UsuarioId == userId) // Para carregar questões
                .OrderByDescending(s => s.DataCriacao)
                .ToListAsync();
        }
        public async Task<List<Question>> ObterQuestoesSimulado(int simuladoId)
        {
            var questoesIds = _context.ListaSimulados
                .Where(ls => ls.SimuladoId == simuladoId)
                .Select(ls => ls.QuestaoId)
                .ToList();

            var questoes = _questionService.GetAllQuestions()
                .Where(q => questoesIds.Contains(q.id))
                .ToList();

            return questoes;
        }

        public async Task<Simulado?> CriarSimulado(CriarSimuladoRequest request, string userId)
        {
            try
            {
                var simuladosUsuario = await _context.Simulados
                    .CountAsync(s => s.UsuarioId == userId);

                if (simuladosUsuario >= 10)
                    throw new InvalidOperationException("msg_maximo_simulado");

                if (request.NumeroQuestoes > 180)
                    throw new InvalidOperationException("O simulado não pode ter mais que 180 questões.");

                var questoesSelecionadas = _questionService.GetDistributedQuestions(
                    request.AreasSelecionadas,
                    request.AnosSelecionados,
                    request.NumeroQuestoes
                );
                if (!questoesSelecionadas.Any())
                    throw new InvalidOperationException("Nenhuma questão encontrada para os critérios selecionados.");

                var simulado = new Simulado
                {
                    UsuarioId = userId,
                    Nome = request.NomeSimulado.Length > 30 ? request.NomeSimulado[..30] : request.NomeSimulado,
                    AreasInteresse = request.AreasSelecionadas,
                    AnosSelecionados = request.AnosSelecionados,
                    NumeroQuestoes = request.NumeroQuestoes,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Simulados.Add(simulado);
                await _context.SaveChangesAsync(); //obtem o Id do simulado

                var questoesParaSimulado = questoesSelecionadas.Take(request.NumeroQuestoes).ToList();

                //vincula questões ao simulado
                await VincularQuestoesSimulado(simulado.Id, questoesParaSimulado); 


                _logger.LogInformation("Simulado criado: {SimuladoId} para usuário {UserId} com {QtdQuestoes} questões",
                    simulado.Id, userId, request.NumeroQuestoes);

                return simulado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar simulado para usuário {UserId}", userId);
                throw;
            }
        }
        /// <summary>
        /// Salva a resposta do usuário para uma questão específica do simulado
        /// </summary>
        /// <param name="simuladoId"></param>
        /// <param name="questaoId"></param>
        /// <param name="resposta"></param>
        /// <returns></returns>
        public async Task<bool> RegistrarResposta(int simuladoId, int questaoId, string resposta)
        {
            try
            {
                var respostaUsuario = await _context.RespostasUsuario
                    .FirstOrDefaultAsync(r => r.QuestaoId == questaoId);

                bool estaCorreta = await ValidarResposta(simuladoId, questaoId, resposta);

                if (respostaUsuario == null)
                {
                    respostaUsuario = new RespostaUsuario
                    {
                        QuestaoId = questaoId,
                        Resposta = resposta,
                        EstaCorreta = estaCorreta,
                        DataResposta = DateTime.UtcNow
                    };
                    _context.RespostasUsuario.Add(respostaUsuario);
                }
                else
                {
                    respostaUsuario.Resposta = resposta;
                    respostaUsuario.EstaCorreta = estaCorreta;
                    respostaUsuario.DataResposta = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar resposta para questão {QuestaoId} no simulado {SimuladoId}", questaoId, simuladoId);
                return false;
            }
        }

        public async Task<SimuladoResultadoDto?> ObterResultadoSimulado(int simuladoId)
        {
            // carrega simulado com respostas (se existir relação populada)
            var simulado = await _context.Simulados
                .Include(s => s.Respostas)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == simuladoId);

            if (simulado == null)
                return null;

            // carregue lista de questões vinculadas ao simulado (com AreaQuestao)
            var listaQuestoes = await _context.ListaSimulados
                .AsNoTracking()
                .Where(ls => ls.SimuladoId == simuladoId)
                .ToListAsync();

            // obtenha respostas do simulado (usa navigation se presente, senão consulta por SimuladoId)
            var respostas = (simulado.Respostas != null && simulado.Respostas.Any())
                ? simulado.Respostas
                : (await _context.RespostasUsuario
                    .AsNoTracking()
                    .Where(r => r.SimuladoId == simuladoId)
                    .ToListAsync());

            // agrupa por área
            var areas = listaQuestoes
                .GroupBy(ls => string.IsNullOrWhiteSpace(ls.AreaQuestao) ? "Outros" : ls.AreaQuestao)
                .Select(g =>
                {
                    var questoesIds = g.Select(x => x.QuestaoId).ToHashSet();
                    var total = g.Count();
                    var respondidas = respostas.Count(r => questoesIds.Contains(r.QuestaoId));
                    var acertos = respostas.Count(r => questoesIds.Contains(r.QuestaoId) && r.EstaCorreta == true);
                    // porcentagem calculada sobre o total de questões da área (mantive a regra que você mostrou)
                    var perc = total == 0 ? 0.0 : Math.Round((double)acertos * 100.0 / total, 2);

                    return new AreaResultadoDto
                    {
                        Area = g.Key,
                        TotalQuestoes = total,
                        QuestoesRespondidas = respondidas,
                        Acertos = acertos,
                        Porcentagem = perc
                    };
                })
                .OrderByDescending(a => a.TotalQuestoes)
                .ToList();

            var totalQuestoes = listaQuestoes.Count;
            var totalAcertos = areas.Sum(a => a.Acertos);
            var totalRespondidas = respostas.Count;
            var percGeral = totalQuestoes == 0 ? 0.0 : Math.Round((double)totalAcertos * 100.0 / totalQuestoes, 2);

            var resultado = new SimuladoResultadoDto
            {
                SimuladoId = simulado.Id,
                Nome = simulado.Nome ?? "Simulado",
                DataCriacao = simulado.DataCriacao,
                TempoGasto = simulado.TempoGasto,
                TotalQuestoes = totalQuestoes,
                QuestoesRespondidas = totalRespondidas,
                TotalAcertos = totalAcertos,
                PorcentagemGeral = percGeral,
                ResultadosPorArea = areas
            };

            return resultado;
        }

        public async Task<TimeSpan> GetTempoGastoAsync(int simuladoId)
        {
            var simulado = await _context.Simulados
                .Where(s => s.Id == simuladoId)
                .Select(s => new { s.TempoGasto })
                .FirstOrDefaultAsync();

            if (simulado == null || simulado.TempoGasto == null) return TimeSpan.Zero;
            return simulado.TempoGasto.Value;
        }

        public async Task SetTempoGastoAsync(int simuladoId, TimeSpan tempoTotal)
        {
            var simulado = await _context.Simulados.FindAsync(simuladoId);
            if (simulado == null) throw new InvalidOperationException("Simulado não encontrado.");

            simulado.TempoGasto = tempoTotal;
            _context.Simulados.Update(simulado);
            await _context.SaveChangesAsync();
        }

        public async Task AddTempoGastoAsync(int simuladoId, TimeSpan delta)
        {
            var simulado = await _context.Simulados.FindAsync(simuladoId);
            if (simulado == null) throw new InvalidOperationException("Simulado não encontrado.");
            simulado.TempoGasto = (simulado.TempoGasto ?? TimeSpan.Zero).Add(delta);
            _context.Simulados.Update(simulado);
            await _context.SaveChangesAsync();
        }

        #region Métodos auxiliares
        private async Task<bool> ValidarResposta(int simuladoId, int questaoId, string resposta)
        {
            try
            {
                var questoesDoSimulado = await ObterQuestoesSimulado(simuladoId);
                var questao = questoesDoSimulado.FirstOrDefault(q => q.id == questaoId);

                if (questao?.correctAlternative != null)
                {
                    return questao.correctAlternative.Equals(resposta, StringComparison.OrdinalIgnoreCase);
                }

                _logger.LogWarning("Questão {QuestaoId} não encontrada para verificação de resposta", questaoId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar resposta da questão {QuestaoId}", questaoId);
                return false;
            }
        }

        private async Task VincularQuestoesSimulado(int simuladoId, List<Question> questoes)
        {
            var seletores = new Dictionary<string, Func<Question, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "ingles", q => q.language },
                { "espanhol", q => q.language }
            };

            foreach (var questao in questoes)
            {
                string area;
                if (!string.IsNullOrEmpty(questao.language) && seletores.ContainsKey(questao.language))
                    area = seletores[questao.language](questao);
                else
                    area = questao.discipline;

                var listaSimulado = new ListaQuestaoSimulado
                {
                    SimuladoId = simuladoId,
                    QuestaoId = questao.id,
                    AreaQuestao = area
                };

                _context.ListaSimulados.Add(listaSimulado);
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        /// <summary>
        /// Obtém relatório completo do simulado com estatísticas
        /// </summary>
        //public async Task<SimuladoRelatorio?> ObterRelatorioSimulado(int simuladoId)
        //{
        //    var simulado = await ObterSimulado(simuladoId);

        //    if (simulado == null)
        //        return null;

        //    var questoesRespondidas = simulado.Questoes.Where(q => q.id != null).ToList();
        //    var acertos = simulado.Questoes.Where(q => q.correctAlternative == true).ToList();

        //    return new SimuladoRelatorio
        //    {
        //        SimuladoId = simuladoId,
        //        Nome = simulado.Nome,
        //        DataCriacao = simulado.DataCriacao,
        //        TempoGasto = simulado.TempoGasto,
        //        TotalQuestoes = simulado.NumeroQuestoes,
        //        QuestoesRespondidas = questoesRespondidas.Count,
        //        TotalAcertos = acertos.Count,
        //        PorcentagemGeral = simulado.PorcentagemGeral,
        //        AcertosPorArea = simulado.AcertosPorArea,
        //        AreasAvaliadas = simulado.AreasInteresse
        //    };
        //}

        /// <summary>
        /// Verifica se simulado pode ser editado (não finalizado)
        /// </summary>


    }
}