using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class Lista
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Nome { get; set; }
       
        [Required]
        public string UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser Usuario { get; set; }
        public virtual ICollection<ListaQuestao> ListaQuestoes { get; set; } = new List<ListaQuestao>();
    }
}
