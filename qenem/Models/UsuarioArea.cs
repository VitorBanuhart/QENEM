using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class UsuarioArea
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Usuario")]
        public string Id_Usuario { get; set; }
        public virtual ApplicationUser Usuario { get; set; }

        [ForeignKey("AreaInteresse")]
        public int Id_AreaInteresse { get; set; }
        public virtual AreaInteresse AreaInteresse { get; set; }
    }
}
