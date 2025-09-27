using Microsoft.AspNetCore.Mvc;
using qenem.Interfaces;
using qenem.ViewModels;
using System.Net;
using Microsoft.Extensions.Logging; // 1. Adicione este using

namespace qenem.Controllers
{
    public class AjudaController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AjudaController> _logger; // 2. Adicione o campo para o logger

        // 3. Injete o ILogger no construtor
        public AjudaController(IEmailService emailService, IConfiguration configuration, ILogger<AjudaController> logger)
        {
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar([FromForm] EnviarEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, message = string.Join(" ", errors) });
            }

            try
            {
                var emailDestino = _configuration.GetValue<string>("MailerSend:SenderEmail");
                if (string.IsNullOrWhiteSpace(emailDestino))
                {
                    _logger.LogError("A chave 'MailerSend:SenderEmail' não foi encontrada ou está vazia no appsettings.json.");
                    return StatusCode((int)HttpStatusCode.InternalServerError, new
                    {
                        success = false,
                        message = "Erro de configuração do servidor. Contate o administrador."
                    });
                }

                var assunto = "Nova Mensagem de Ajuda do Site";
                var corpoHtml = $@"
                    <h3>Nova mensagem recebida:</h3>
                    <p><strong>Remetente:</strong> {model.Email}</p><hr>
                    <p><strong>Mensagem:</strong></p>
                    <div style='padding:15px; border:1px solid #ccc; background-color:#f9f9f9;'><p>{model.Mensagem}</p></div>";

                await _emailService.SendEmailAsync("vitorbanuhart123@gmail.com", assunto, corpoHtml);

                return Ok(new { success = true, message = "Sua mensagem foi enviada com sucesso!" });
            }
            catch (Exception ex)
            {
                // 4. LOG DO ERRO! Esta é a parte mais importante.
                // O erro detalhado aparecerá no console do seu servidor.
                _logger.LogError(ex, "Falha crítica ao tentar enviar e-mail pelo formulário de ajuda.");

                // 5. Retorna uma mensagem de erro mais clara
                return StatusCode((int)HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Ocorreu um erro ao conectar-se ao serviço de e-mail. Por favor, tente novamente mais tarde."
                });
            }
        }
    }
}