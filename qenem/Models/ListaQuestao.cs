using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class ListaQuestao
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ListaId { get; set; }

        [ForeignKey("ListaId")]
        public virtual Lista Lista { get; set; }

        [Required]
        public string QuestaoId { get; set; }
    }
}
