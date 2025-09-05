using qenem.Models;
using qenem.Services;

namespace qenem.Services
{
    public class EnemRepository
    {
        private readonly JsonDataService _jsonService;

        public EnemRepository(JsonDataService jsonService)
        {
            _jsonService = jsonService;
        }

        public List<Question> ObterQuestoesPorAno(int ano)
        {
            return _jsonService.LoadJson<Question>($"provas/{ano}.json");
        }

        //public Question? ObterQuestaoPorNumero(int ano, int numero)
        //{
        //    var questoes = ObterQuestoesPorAno(ano);
        //    return questoes.FirstOrDefault(q => q.Numero == numero);
        //}

        public List<Question> ObterQuestoesPorArea(int ano, string area)
        {
            var questoes = ObterQuestoesPorAno(ano);
            return questoes.Where(q => q.discipline.Equals(area, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
