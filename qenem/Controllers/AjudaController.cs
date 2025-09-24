using Microsoft.AspNetCore.Mvc;
using qenem.Interfaces;
using qenem.ViewModels;

namespace qenem.Controllers
{
    public class AjudaController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AjudaController(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Enviar([FromForm] EnviarEmailViewModel model)
        {
            // 1. Valida o modelo
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Por favor, preencha todos os campos obrigatórios."
                });
            }

            try
            {
                // 2. Pega o e-mail do administrador (destinatário) configurado no appsettings
                var emailDestino = _configuration.GetValue<string>("MailerSend:SenderEmail");
                if (string.IsNullOrWhiteSpace(emailDestino))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Configuração de e-mail do destinatário não encontrada. Contate o administrador."
                    });
                }

                var assunto = "Nova Mensagem do Site";

                // 3. Corpo HTML do e-mail
                var corpoHtml = $@"
                    <h3>Nova mensagem recebida:</h3>
                    <ul>
                    </ul>
                    <p><strong>Mensagem:</strong></p>
                    <div style='padding: 15px; border: 1px solid #ccc; background-color: #f9f9f9;'>
                        {model.Mensagem}
                    </div>";

                // 4. Envia o e-mail usando o serviço
                await _emailService.SendEmailAsync("vitorbanuhart123@gmail.com", assunto, corpoHtml);

                // 5. Retorno de sucesso
                return Ok(new
                {
                    success = true,
                    message = "Sua mensagem foi enviada com sucesso. Obrigado pelo contato!"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ocorreu um erro ao enviar a mensagem. Tente novamente mais tarde."
                });
            }
        }
    }
}
