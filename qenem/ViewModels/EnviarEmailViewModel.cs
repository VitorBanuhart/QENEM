using System.ComponentModel.DataAnnotations;

namespace qenem.ViewModels
{
    public class EnviarEmailViewModel
    {
        [Required(ErrorMessage = "A mensagem é obrigatória.")]
        public string Mensagem { get; set; }
    }
}
