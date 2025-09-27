using System.ComponentModel.DataAnnotations;

namespace qenem.ViewModels
{
    public class NovaListaViewModel
    {
        [Required(ErrorMessage = "O nome da lista é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
        public string Nome { get; set; }
    }
}
