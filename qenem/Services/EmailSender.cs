using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace qenem.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Apenas loga no console (teste local)
            Console.WriteLine($"Envio de e-mail simulado para {email}: {subject}");
            return Task.CompletedTask;
        }
    }
}
