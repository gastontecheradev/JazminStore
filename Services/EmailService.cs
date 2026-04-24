using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Jazmin.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null);
}

public class EmailOptions
{
    public bool Enabled { get; set; }
    public string From { get; set; } = "";
    public string FromName { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailService> _log;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> log)
    {
        _opts = config.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
        _log = log;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        if (!_opts.Enabled)
        {
            _log.LogInformation("[EMAIL DISABLED] To={To} Subject={Subject}", to, subject);
            return;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_opts.FromName, _opts.From));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody ?? StripHtml(htmlBody)
            };
            msg.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var secure = _opts.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_opts.SmtpHost, _opts.SmtpPort, secure);
            if (!string.IsNullOrEmpty(_opts.SmtpUser))
                await client.AuthenticateAsync(_opts.SmtpUser, _opts.SmtpPassword);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error enviando email a {To}", to);
        }
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}
