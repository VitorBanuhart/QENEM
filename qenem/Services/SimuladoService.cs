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

                // Obter questões do serviço
                var questoesSelecionadas = await Task.Run(() =>
                    _questionService.GetRandomQuestions(request.AreasSelecionadas, request.LinguagensSelecionadas, userId.ToString())
                );

                if (!questoesSelecionadas.Any())
                    throw new InvalidOperationException("Nenhuma questão encontrada para os critérios selecionados.");

                var simulado = new Simulado
                {
                    UsuarioId = userId,
                    Nome = request.NomeSimulado.Length > 30 ? request.NomeSimulado[..30] : request.NomeSimulado,
                    AreasInteresse = request.AreasSelecionadas
                        .Concat(request.LinguagensSelecionadas ?? new List<string>())
                        .ToList(),
                    AnosSelecionados = request.AnosSelecionados,
                    NumeroQuestoes = request.NumeroQuestoes,
                    DataCriacao = DateTime.UtcNow
                };

                _context.Simulados.Add(simulado);
                await _context.SaveChangesAsync(); // Para obter o Id do simulado

                // ✅ NOVO: Criar ListaSimulado para cada questão selecionada
                var questoesParaSimulado = questoesSelecionadas.Take(request.NumeroQuestoes).ToList();

                foreach (var questao in questoesParaSimulado)
                {
                    var listaSimulado = new ListaSimulado
                    {
                        SimuladoId = simulado.Id,
                        QuestaoId = questao.id,
                        AreaQuestao = questao.discipline // Para facilitar cálculos de AcertosPorArea
                    };

                    _context.ListaSimulados.Add(listaSimulado);
                }

                await _context.SaveChangesAsync();

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

        // ✅ CORRIGIDO: Usar banco de dados em vez de lista em memória
        public async Task<List<Simulado>> ListarSimulados(string userId)
        {
            return await _context.Simulados
                .Where(s => s.UsuarioId == userId)
                .Include(s => s.Questoes) // Para carregar questões
                .OrderByDescending(s => s.DataCriacao)
                .ToListAsync();
        }

        // ✅ CORRIGIDO: Buscar no banco com includes necessários
        public async Task<Simulado?> ObterSimulado(int id)
        {
            return await _context.Simulados
                .Include(s => s.Questoes) // ✅ CRÍTICO: Para AcertosPorArea funcionar
                .Include(s => s.Respostas)        // Para respostas se usar RespostaUsuario
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        // ✅ CORRIGIDO: Registrar resposta no banco de dados
        public async Task<bool> RegistrarResposta(int simuladoId, int questaoId, string resposta)
        {
            try
            {
                // Buscar a questão no simulado
                var questaoSimulado = await _context.ListaSimulados
                    .FirstOrDefaultAsync(ls =>
                        ls.SimuladoId == simuladoId &&
                        ls.QuestaoId == questaoId);

                if (questaoSimulado == null)
                {
                    _logger.LogWarning("Questão {QuestaoId} não encontrada no simulado {SimuladoId}", questaoId, simuladoId);
                    return false;
                }

                // ✅ Registrar a resposta
                questaoSimulado.RespostaUsuario = resposta;
                questaoSimulado.EstaCorreta = await VerificarSeRespostaEstaCorreta(questaoId, resposta);
                questaoSimulado.DataResposta = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogDebug("Resposta '{Resposta}' registrada para questão {QuestaoId} no simulado {SimuladoId}",
                    resposta, questaoId, simuladoId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar resposta para questão {QuestaoId} no simulado {SimuladoId}",
                    questaoId, simuladoId);
                return false;
            }
        }

        // ✅ CORRIGIDO:        //public async Task<bool> FinalizarSimulado(int simuladoId, TimeSpan? tempoGasto = null)
        //{
        //    try
        //    {
        //        var simulado = await _context.Simulados
        //            .Include(s => s.Questoes)
        //            .FirstOrDefaultAsync(s => s.Id == simuladoId);

        //        if (simulado == null)
        //        {
        //            _logger.LogWarning("Simulado {SimuladoId} não encontrado para finalização", simuladoId);
        //            return false;
        //        }

        //        // Definir tempo gasto
        //        if (tempoGasto.HasValue)
        //        {
        //            simulado.TempoGasto = tempoGasto;
        //        }

        //        await _context.SaveChangesAsync();

        //        // Log das estatísticas
        //        var totalRespondidas = simulado.Questoes.Count(q => q. != null);
        //        var totalAcertos = simulado.Questoes.Count(q => q.EstaCorreta == true);
        //        var acertosPorArea = simulado.AcertosPorArea;

        //        _logger.LogInformation("Simulado {SimuladoId} finalizado - {TotalAcertos}/{TotalRespondidas} questões corretas. Tempo: {Tempo}",
        //            simuladoId, totalAcertos, totalRespondidas, tempoGasto?.ToString(@"hh\:mm\:ss") ?? "N/A");

        //        _logger.LogDebug("Acertos por área: {AcertosPorArea}",
        //            string.Join(", ", acertosPorArea.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erro ao finalizar simulado {SimuladoId}", simuladoId);
        //        return false;
        //    }
        //} Finalizar simulado com cálculos reais


        // ✅ NOVO: Método para verificar se resposta está correta
        private async Task<bool> VerificarSeRespostaEstaCorreta(int questaoId, string resposta)
        {
            try
            {
                // IMPLEMENTAR baseado em como você armazena as questões
                // Opção 1: Se você tem DbSet<Question>
                // var questao = await _context.Questions.FindAsync(questaoId);
                // return questao?.CorrectAnswer?.Equals(resposta, StringComparison.OrdinalIgnoreCase) ?? false;

                // Opção 2: Se você usa o QuestionService
                var questoes = await Task.Run(() => _questionService.GetAllQuestions());
                var questao = questoes.FirstOrDefault(q => q.id == questaoId);

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

        // ✅ NOVO: Métodos utilitários adicionais

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
        public async Task<bool> SimuladoPodeSerEditado(int simuladoId)
        {
            var simulado = await _context.Simulados.FindAsync(simuladoId);
            return simulado?.TempoGasto == null; // Se não tem tempo gasto, ainda não foi finalizado
        }

        /// <summary>
        /// Obtém progresso do simulado (quantas questões foram respondidas)
        /// </summary>
        //public async Task<SimuladoProgresso?> ObterProgressoSimulado(int simuladoId)
        //{
        //    var simulado = await ObterSimulado(simuladoId);

        //    if (simulado == null)
        //        return null;

        //    var respondidas = simulado.Questoes.Count(q => q. != null);

        //    return new SimuladoProgresso
        //    {
        //        SimuladoId = simuladoId,
        //        QuestoesRespondidas = respondidas,
        //        TotalQuestoes = simulado.NumeroQuestoes,
        //        PorcentagemCompleta = (double)respondidas / simulado.NumeroQuestoes * 100
        //    };
        //}
    }
    public class SimuladoRelatorio
    {
        public int SimuladoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public TimeSpan? TempoGasto { get; set; }
        public int TotalQuestoes { get; set; }
        public int QuestoesRespondidas { get; set; }
        public int TotalAcertos { get; set; }
        public double PorcentagemGeral { get; set; }
        public Dictionary<string, int> AcertosPorArea { get; set; } = new();
        public List<string> AreasAvaliadas { get; set; } = new();
    }

    public class SimuladoProgresso
    {
        public int SimuladoId { get; set; }
        public int QuestoesRespondidas { get; set; }
        public int TotalQuestoes { get; set; }
        public double PorcentagemCompleta { get; set; }
    }
}