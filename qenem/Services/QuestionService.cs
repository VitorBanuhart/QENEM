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
                    question.UniqueId = file;
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
            var random = new Random();
            var result = new List<Question>();
            var usedQuestionUniqueIds = new HashSet<string>();
            var allCategories = disciplines.Concat(languages).ToList();

            if (!allCategories.Any() || !filteredByDiscipline.Any())
                return result; // Retorna vazio se não houver categorias ou questões disponíveis

            // Loop para adicionar uma questão por vez, de forma cíclica e balanceada
            for (int i = 0; i < totalQuestoes; i++)
            {
                // Seleciona a próxima categoria no ciclo (ex: Mat -> Fis -> Qui -> Mat ...)
                string category = allCategories[i % allCategories.Count];
                bool isDiscipline = disciplines.Contains(category, StringComparer.OrdinalIgnoreCase);

                // Busca todas as questões disponíveis e ainda não usadas para a categoria da vez
                var potentialQuestions = filteredByDiscipline
                    .Where(q => !usedQuestionUniqueIds.Contains(q.UniqueId) &&
                                (isDiscipline ? q.discipline != null && q.discipline.Equals(category, StringComparison.OrdinalIgnoreCase)
                                               : q.language != null && q.language.Equals(category, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (potentialQuestions.Any())
                {
                    // Dentre as disponíveis, seleciona uma de forma aleatória
                    var questionToAdd = potentialQuestions[random.Next(potentialQuestions.Count)];
                    result.Add(questionToAdd);
                    usedQuestionUniqueIds.Add(questionToAdd.UniqueId);
                }
            }

            // Se, mesmo após o ciclo, alguma categoria se esgotou e não atingimos as 10 questões,
            // preenchemos o restante com o que sobrou de QUALQUER outra categoria.
            // Isso garante que o resultado final sempre terá 10 questões, se houver disponibilidade.
            if (result.Count < totalQuestoes && filteredByDiscipline.Count > result.Count)
            {
                int needed = totalQuestoes - result.Count;
                var extraQuestions = filteredByDiscipline
                    .Where(q => !usedQuestionUniqueIds.Contains(q.UniqueId))
                    .OrderBy(_ => random.Next())
                    .Take(needed)
                    .ToList();

                result.AddRange(extraQuestions);
            }

            if (!_respostasPorDia.ContainsKey(userId))
                _respostasPorDia[userId] = 0;

            _respostasPorDia[userId] += result.Count;

            return result;
        }
    }
}
