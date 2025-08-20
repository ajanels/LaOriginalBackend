using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace LaOriginalBackend.Services
{
    public class EmailService
    {
        private readonly string _apiKey;

        public EmailService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("no-reply@laoriginal.com", "La Original");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Error enviando correo: {response.StatusCode}");
            }
        }
    }
}
