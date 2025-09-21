namespace qenem.DTO
{
    public class SimuladoResultadoDto
    {
        public int SimuladoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public TimeSpan? TempoGasto { get; set; }
        public int TotalQuestoes { get; set; }
        public int QuestoesRespondidas { get; set; }
        public int TotalAcertos { get; set; }
        public double PorcentagemGeral { get; set; }
        public List<AreaResultadoDto> ResultadosPorArea { get; set; } = new();
    }

    public class AreaResultadoDto
    {
        public string Area { get; set; } = string.Empty;
        public int TotalQuestoes { get; set; }
        public int QuestoesRespondidas { get; set; }
        public int Acertos { get; set; }
        public double Porcentagem { get; set; }
    }
}
