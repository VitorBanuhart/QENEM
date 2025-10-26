using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class Pontos
    {
        public int Id { get; set; }
        [ForeignKey("UsuarioId")]
        public string Usuario { get; set; }
        public int TotalPontuacao { get; set; }
    }
}
