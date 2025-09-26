using qenem.Models;

namespace qenem.ViewModels
{
    public class SimuladoViewModel
    {
        public int SimuladoId { get; set; }
        public string SimuladoNome { get; set; }
        public int TotalQuestoes { get; set; }
        public int QuestaoAtualIndex { get; set; }
        public Question QuestaoAtual { get; set; }
        public string RespostaUsuario { get; set; }
        public Dictionary<string, string> RespostasSalvas { get; set; }
    }

}
