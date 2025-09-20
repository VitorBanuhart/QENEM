using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class ListaQuestaoSimulado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SimuladoId { get; set; }
        [ForeignKey("SimuladoId")]
        public virtual Simulado Simulado { get; set; }

        [Required]
        public int QuestaoId { get; set; }

        // Opcional: ordem da questão no simulado
        public int Ordem { get; set; }

        // Opcional: área da questão para facilitar relatórios
        [StringLength(50)]
        public string? AreaQuestao { get; set; }
    }
}
