using qenem.Models;

namespace qenem.Interfaces
{
    public interface ISimuladoService
    {
        Task<Simulado?> CriarSimulado(string nome, List<string> areas, List<int> anos, int qtdQuestoes, string userId);
        Task<List<Simulado>> ListarSimulados();
        Task<Simulado?> ObterSimulado(int id);
        Task RegistrarResposta(int simuladoId, int questaoId, string resposta);
        Task FinalizarSimulado(int simuladoId);
    }

}
