using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class Simulado
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser Usuario { get; set; }

        public string Nome { get; set; } = string.Empty; // Máx. 30 caracteres
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public List<string> AreasInteresse { get; set; } = new();
        public List<int> AnosSelecionados { get; set; } = new();
        public int NumeroQuestoes { get; set; }
        public List<RespostaUsuario> Respostas { get; set; } = new();
        public TimeSpan? TempoGasto { get; set; }
        public bool Finalizado { get; set; } = false;
        //public Dictionary<string, int> AcertosPorArea { get; set; } = new();
    }
    public class SimuladoRelatorio
    {
        public int SimuladoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public TimeSpan? TempoGasto { get; set; }
        public int TotalQuestoes { get; set; }
        public int QuestoesRespondidas { get; set; }
        public int TotalAcertos { get; set; }
        public double PorcentagemGeral { get; set; }
        public Dictionary<string, int> AcertosPorArea { get; set; } = new();
        public List<string> AreasAvaliadas { get; set; } = new();
    }

    public class SimuladoProgresso
    {
        public int SimuladoId { get; set; }
        public int QuestoesRespondidas { get; set; }
        public int TotalQuestoes { get; set; }
        public double PorcentagemCompleta { get; set; }
    }

}
