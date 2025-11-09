using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class Anotacoes
    {
        [Key]
        public int Id { get; set; }

        // id do usuário (string para suportar GUIDs do Identity)
        [Required]
        public string UsuarioId { get; set; }

        // conteudo das anotações
        public string AnotacoesUsuario { get; set; }
    }
}
