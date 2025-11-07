using SendGrid;
using SendGrid.Helpers.Mail;

namespace LaOriginalBackend.Services
{
    public class EmailService
    {
        private readonly string _apiKey;
        private readonly string _from;
        private readonly string _fromName;

        public EmailService(IConfiguration cfg)
        {
            _apiKey = cfg["SendGrid:ApiKey"]
                        ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                        ?? throw new InvalidOperationException("Falta SendGrid:ApiKey");
            _from = cfg["SendGrid:FromAddress"] ?? "no-reply@laoriginal.com";
            _fromName = cfg["SendGrid:FromName"] ?? "La Original";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string html)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_from, _fromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: html);
            var res = await client.SendEmailAsync(msg);

            if ((int)res.StatusCode >= 300)
            {
                var body = await res.Body.ReadAsStringAsync();
                throw new Exception($"SendGrid error: {(int)res.StatusCode} {res.StatusCode} - {body}");
            }
        }
    }
}
