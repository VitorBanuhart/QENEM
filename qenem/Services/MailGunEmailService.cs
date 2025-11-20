using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using qenem.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace qenem.Services
{
    public class MailGunEmailService : IEmailSender, IEmailService
    {
        private readonly MailGunSetting _setting;
        private readonly HttpClient _httpClient;

        public MailGunEmailService(IOptions<MailGunSetting> setting)
        {
            _setting = setting.Value;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.mailgun.net/v3/")
            };
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{_setting.ApiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            if (string.IsNullOrEmpty(_setting.ApiKey) || string.IsNullOrEmpty(_setting.SenderEmail))
            {
                throw new InvalidOperationException("MailerSend settings are not properly configured.");
            }

            var emailData = new Dictionary<string,string>
            {
                { "from", _setting.SenderName},
                { "to", toEmail },
                { "subject", subject },
                { "html", htmlContent },
                { "text", "Versão em texto do e-mail" }
            };


            var content = new FormUrlEncodedContent(emailData);

            var response = await _httpClient.PostAsync($"{_setting.SenderEmail}/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erro ao enviar e-mail. Status: {response.StatusCode}, Detalhes: {errorContent}");
            }
        }
    }
}
