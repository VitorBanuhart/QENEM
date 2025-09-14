namespace qenem.Models
{
    public class Simulado
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty; // Máx. 30 caracteres
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public List<string> AreasInteresse { get; set; } = new();
        public List<int> AnosSelecionados { get; set; } = new();
        public int NumeroQuestoes { get; set; }
        public List<Question> Questoes { get; set; } = new();
        public TimeSpan? TempoGasto { get; set; }
        public Dictionary<string, int> AcertosPorArea { get; set; } = new();
    }

}
