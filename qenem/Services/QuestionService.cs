using qenem.Models;
using System.Text.Json;

namespace qenem.Services
{
    public class QuestionService
    {
        private readonly string _jsonDirectory;
        private static Dictionary<string, int> _respostasPorDia = new(); // controla limite diário por usuário (mock)
        private readonly SeuProjeto.Services.EnemRepository _repo;

        public QuestionService(SeuProjeto.Services.EnemRepository repo, string jsonDirectory)
        {
            _repo = repo;
            _jsonDirectory = jsonDirectory;
        }

        /// <summary>
        /// Carrega todas as questões do diretório de JSONs.
        /// </summary>
        private List<Question> LoadQuestions()
        {
            var questions = new List<Question>();

            var yearDirectories = Directory.GetDirectories(_jsonDirectory);

            foreach (var dir in yearDirectories) { 

                foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                var json = File.ReadAllText(file);
                Console.WriteLine($"Arquivo: {file}");
                Console.WriteLine($"Conteúdo JSON:\n{json}");
                
                var question = JsonSerializer.Deserialize<Question>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (question != null)
                {
                    questions.Add(question);
                }
            }
            }
            return questions;
        }

        /// <summary>
        /// Retorna 10 questões aleatórias de acordo com a disciplina escolhida.
        /// </summary>
        public List<Question> GetRandomQuestions(List<string> disciplines, List<string> languages, string userId)
        {
            // Checa limite diário
            if (_respostasPorDia.ContainsKey(userId) && _respostasPorDia[userId] >= 450)
                throw new InvalidOperationException("msg_maximo_questoes");

            var allQuestions = LoadQuestions();

            // Filtra apenas pelas disciplinas escolhidas
            var filteredByDiscipline = allQuestions
                .Where(q => disciplines.Contains(q.discipline, StringComparer.OrdinalIgnoreCase) || languages.Contains(q.language, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Calcula quantas questões por disciplina (divisão equilibrada)
            int totalQuestoes = 10;
            int baseCount = totalQuestoes / (disciplines.Count + languages.Count); // mínimo por disciplina
            int sobra = totalQuestoes % (disciplines.Count + languages.Count);     // se não divide certinho, sorteia as sobras

            var random = new Random();
            var result = new List<Question>();

            foreach (var disc in disciplines)
            {
                var questoesDisciplina = filteredByDiscipline
                    .Where(q => q.discipline.Equals(disc, StringComparison.OrdinalIgnoreCase) )
                    .OrderBy(_ => random.Next())
                    .Take(baseCount)
                    .ToList();

                result.AddRange(questoesDisciplina);
            }

            foreach (var lang in languages)
            {
                var questoesDisciplina = filteredByDiscipline
                    .Where(q => q.discipline.Equals("linguagens", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => random.Next())
                    .Take(baseCount)
                    .ToList();

                result.AddRange(questoesDisciplina);
            }

            // Distribui as sobras sorteando disciplinas

            var todasDisciplinas = new List<string> { };
            todasDisciplinas.AddRange(disciplines);
            todasDisciplinas.AddRange(languages);

            var disciplinasSorteadas = todasDisciplinas
                .OrderBy(_ => random.Next())
                .Take(sobra)
                .ToList();

            foreach (var disc in disciplinasSorteadas)
            {
                var questaoExtra = filteredByDiscipline
                    .Where(q => q.discipline.Equals(disc, StringComparison.OrdinalIgnoreCase) && !result.Contains(q))
                    .OrderBy(_ => random.Next())
                    .FirstOrDefault();

                if (questaoExtra != null)
                    result.Add(questaoExtra);
            }

            foreach (var lang in disciplinasSorteadas)
            {
                var questaoExtra = filteredByDiscipline
                    .Where(q => q.discipline.Equals("linguagens", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => random.Next())
                    .FirstOrDefault();

                if (questaoExtra != null)
                    result.Add(questaoExtra);
            }

            // Atualiza progresso diário
            if (!_respostasPorDia.ContainsKey(userId))
                _respostasPorDia[userId] = 0;

            _respostasPorDia[userId] += result.Count;

            return result;
        }
    }
}
