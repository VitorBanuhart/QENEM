using qenem.Models;
using System.Text.Json;

namespace qenem.Services
{
    public class QuestionService
    {
        private readonly string _jsonDirectory;
        private static Dictionary<string, int> _respostasPorDia = new(); // controla limite diário por usuário (mock)

        public QuestionService(SeuProjeto.Services.EnemRepository repo, string jsonDirectory)
        {
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
        public List<Question> GetRandomQuestions(List<string> disciplines, string userId)
        {
            // Checa limite diário
            if (_respostasPorDia.ContainsKey(userId) && _respostasPorDia[userId] >= 450)
                throw new InvalidOperationException("msg_maximo_questoes");

            var allQuestions = LoadQuestions();

            var filtered = allQuestions
                 .Where(q => disciplines.Any(d =>
                     q.discipline.Equals(d, StringComparison.OrdinalIgnoreCase)))
                 .OrderBy(_ => Guid.NewGuid()) // shuffle
                 .Take(10)
                 .ToList();

            // Atualiza progresso diário
            if (!_respostasPorDia.ContainsKey(userId))
                _respostasPorDia[userId] = 0;

            _respostasPorDia[userId] += filtered.Count;

            return filtered;
        }
    }
}
