using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class AreaInteresse
    {
        [Key]
        [Column("id_areaInteresse")]
        public int IdAreaInteresse { get; set; }

        [Column("nome_areaInteresse")]
        [StringLength(50)]
        public String NomeAreaInteresse { get; set; }

        public virtual ICollection<UsuarioArea> UsuarioAreas { get; set; } = new List<UsuarioArea>();
    }
}
