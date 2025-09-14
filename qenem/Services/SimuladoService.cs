using qenem.Interfaces;
using qenem.Models;

namespace qenem.Services
{
    public class SimuladoService : ISimuladoService
    {
        private readonly QuestionService _questionService;
        private readonly List<Simulado> _simulados = new();

        public SimuladoService(QuestionService questionService)
        {
            _questionService = questionService;
        }

        public async Task<Simulado?> CriarSimulado(string nome, List<string> areas, List<int> anos, int qtdQuestoes, string userId)
        {
            if (_simulados.Count >= 10)
                throw new InvalidOperationException("msg_maximo_simulado");

            if (qtdQuestoes > 180)
                throw new InvalidOperationException("O simulado não pode ter mais que 180 questões.");

            // 🔹 Aqui chamamos o QuestionService
            var questoesSelecionadas = await Task.Run(() =>
                _questionService.GetRandomQuestions(areas, new List<string>(), userId) // línguas deixei vazio, pode adaptar
            );

            var simulado = new Simulado
            {
                Id = _simulados.Count + 1,
                Nome = nome.Length > 30 ? nome[..30] : nome,
                AreasInteresse = areas,
                AnosSelecionados = anos,
                NumeroQuestoes = qtdQuestoes,
                Questoes = questoesSelecionadas.Take(qtdQuestoes).ToList()
            };

            _simulados.Add(simulado);
            return simulado;
        }

        public Task<List<Simulado>> ListarSimulados()
            => Task.FromResult(_simulados);

        public Task<Simulado?> ObterSimulado(int id)
            => Task.FromResult(_simulados.FirstOrDefault(s => s.Id == id));

        public Task RegistrarResposta(int simuladoId, int questaoId, string resposta)
        {
            var simulado = _simulados.FirstOrDefault(s => s.Id == simuladoId);
            if (simulado != null)
            {
                var questao = simulado.Questoes.FirstOrDefault(q => q.Id == questaoId);
                if (questao != null)
                {
                    questao.RespostaUsuario = resposta;
                }
            }
            return Task.CompletedTask;
        }

        public Task FinalizarSimulado(int simuladoId)
        {
            var simulado = _simulados.FirstOrDefault(s => s.Id == simuladoId);
            if (simulado != null)
            {
