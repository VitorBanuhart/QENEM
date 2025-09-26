namespace qenem.DTO
{
    public class Requests
    {
    }

    #region Simulado

    public class CriarSimuladoRequest
    {
        public string NomeSimulado { get; set; }
        public int NumeroQuestoes { get; set; }
        public List<string> AreasSelecionadas { get; set; }
        public List<int> AnosSelecionados { get; set; }
    }



    #endregion
}
