using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace CRMWebApp.Services
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(IConfiguration config, ILogger<MailKitEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var s = _config.GetSection("SmtpSettings");

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(s["SenderName"], s["SenderEmail"]));
            msg.To.Add(MailboxAddress.Parse(email));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

            using var client = new SmtpClient();
            var port = int.TryParse(s["Port"], out var p) ? p : 587;
            var useStartTls = bool.TryParse(s["UseStartTls"], out var tls) && tls;

            await client.ConnectAsync(s["Server"], port,
                useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            if (!string.IsNullOrWhiteSpace(s["Username"]))
                await client.AuthenticateAsync(s["Username"], s["Password"]);

            await client.SendAsync(msg);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email} with subject {Subject}", email, subject);
        }
    }
}