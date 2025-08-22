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
        public EnumAreaInteresse NomeAreaInteresse { get; set; }

    }
}