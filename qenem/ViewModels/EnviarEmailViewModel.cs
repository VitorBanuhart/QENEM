using System.ComponentModel.DataAnnotations;

namespace qenem.ViewModels
{
    public class EnviarEmailViewModel
    {
        [Required(ErrorMessage = "A mensagem é obrigatória.")]
        public string Mensagem { get; set; }

        [Required(ErrorMessage = "O e-mail do usuário é obrigatório.")]
        [EmailAddress(ErrorMessage = "Por favor, insira um endereço de e-mail válido.")]
        public string Email { get; set; }
    }
}
