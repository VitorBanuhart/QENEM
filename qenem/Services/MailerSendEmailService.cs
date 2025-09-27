using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using qenem.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace qenem.Services
{
    public class MailerSendEmailService : IEmailSender
    {
        private readonly MailerSendSetting _setting;
        private readonly HttpClient _httpClient;

        public MailerSendEmailService(IOptions<MailerSendSetting> setting)
        {
            _setting = setting.Value;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.mailersend.com/v1/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _setting.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            if (string.IsNullOrEmpty(_setting.ApiKey) || string.IsNullOrEmpty(_setting.SenderEmail))
            {
                throw new InvalidOperationException("MailerSend settings are not properly configured.");
            }

            var emailData = new
            {
                from = new
                {
                    email = _setting.SenderEmail,
                    name = _setting.SenderName
                },
                to = new[]
                {
                    new { email = toEmail, name = toEmail } // Nome pode ser vazio ou mesmo que email
                },
                subject = subject,
                html = htmlContent,
                text = "Versão em texto do e-mail"
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("email", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erro ao enviar e-mail. Status: {response.StatusCode}, Detalhes: {errorContent}");
            }
        }
    }
}
