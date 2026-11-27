
using ApplyFlow.Server.Configuration;
using ApplyFlow.Server.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.Encodings.Web;

namespace ApplyFlow.Server.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendVerificationEmailAsync(
        string email,
        string verificationUrl)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _settings.SenderName,
            _settings.SenderEmail));

        message.To.Add(MailboxAddress.Parse(email));

        message.Subject = "Verify your ApplyFlow account";

        var safeUrl = HtmlEncoder.Default.Encode(verificationUrl);

        var body = new BodyBuilder
        {
            HtmlBody = $"""
                <html>
                <body style="font-family:Arial,sans-serif;
                             max-width:600px;margin:auto;
                             padding:30px;color:#333;">

                    <h2>Welcome to ApplyFlow!</h2>

                    <p>Thank you for creating an account.</p>

                    <p>Please verify your email address
                       by clicking the button below.</p>

                    <p style="margin:30px 0;">
                        <a href="{safeUrl}"
                           style="background:#2563eb;
                                  color:white;
                                  padding:14px 24px;
                                  text-decoration:none;
                                  border-radius:6px;">
                            Verify Email
                        </a>
                    </p>

                    <p>This verification link expires in 24 hours.</p>

                    <p>If you did not create this account,
                       you can ignore this email.</p>

                    <p>Regards,<br/>ApplyFlow Team</p>
                </body>
                </html>
                """,

            TextBody = $"""
                Welcome to ApplyFlow!

                Verify your email using this link:

                {verificationUrl}

                This link expires in 24 hours.
                """
        };

        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _settings.Username,
            _settings.Password);

        await client.SendAsync(message);

        await client.DisconnectAsync(true);
    }
}
