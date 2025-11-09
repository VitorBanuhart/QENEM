using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class AvaliaQuestao
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Usuario { get; set; }

        [Required]
        public string QuestaoId { get; set; }

        [Required]
        public int Avaliacao { get; set; }
    }
}
