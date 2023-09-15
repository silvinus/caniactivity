using Caniactivity.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Caniactivity.Backend.Services
{
    public interface IEmailService
    {
        void SendEmail(Message message, int maxRetries);
        void ReSendEmail(EmailOutbox message, int maxRetries);
    }

    public class EmailService : IEmailService
    {
        public static readonly string SMTP_FROM_ENV_VAR_NAME = "SMTP_FROM";
        public static readonly string SMTP_USER_ENV_VAR_NAME = "SMTP_USER";
        public static readonly string SMTP_PASSWORD_ENV_VAR_NAME = "SMTP_PASSWORD";

        private readonly EmailConfiguration _emailConfig;
        private readonly IServiceProvider serviceProvider;

        public EmailService(EmailConfiguration configuration, IServiceProvider serviceProvider)
        {
            _emailConfig = configuration;
            _emailConfig.From = Environment.GetEnvironmentVariable(SMTP_FROM_ENV_VAR_NAME);
            _emailConfig.UserName = Environment.GetEnvironmentVariable(SMTP_USER_ENV_VAR_NAME);
            _emailConfig.Password = Environment.GetEnvironmentVariable(SMTP_PASSWORD_ENV_VAR_NAME);

            this.serviceProvider = serviceProvider;
        }

        void IEmailService.SendEmail(Message message, int maxRetries)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var emailMessage = CreateEmailMessage(message);

                var context = scope.ServiceProvider.GetService<CaniActivityContext>();
                var outboxEntity = new EmailOutbox()
                {
                    To = string.Concat(message.To.Select(w => w.Address)),
                    Subject = message.Subject,
                    Body = message.Content,
                    IsProcessed = false
                };
                context.Outbox.Add(outboxEntity);
                context.SaveChanges();

                int attempts = 0;
                while (attempts < maxRetries)
                {
                    try
                    {
                        Send(emailMessage);
                        outboxEntity.IsProcessed = true;
                        context.SaveChanges();
                        break;
                    }
                    catch (Exception)
                    {
                        attempts++;
                        if (attempts == maxRetries)
                        {
                            throw new InvalidOperationException("Failed to send email after multiple attempts.");
                        }
                    }
                }
            }
        }

        void IEmailService.ReSendEmail(EmailOutbox mailToSend, int maxRetries)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var emailMessage = CreateEmailMessage(new Message(
                                    mailToSend.To.Split('|'),
                                    mailToSend.Subject,
                                    mailToSend.Body));

                int attempts = 0;
                while (attempts < maxRetries)
                {
                    try
                    {
                        Send(emailMessage);
                        break;
                    }
                    catch (Exception)
                    {
                        attempts++;
                        if (attempts == maxRetries)
                        {
                            throw new InvalidOperationException("Failed to send email after multiple attempts.");
                        }
                    }
                }
            }
        }

        private MimeMessage CreateEmailMessage(Message message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("email", _emailConfig.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Text) { Text = message.Content };
            return emailMessage;
        }

        private void Send(MimeMessage mailMessage)
        {
            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(_emailConfig.SmtpServer, _emailConfig.Port, SecureSocketOptions.StartTls);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Authenticate(_emailConfig.UserName, _emailConfig.Password);
                    client.Send(mailMessage);
                }
                catch
                {
                    //log an error message or throw an exception or both.
                    throw;
                }
                finally
                {
                    client.Disconnect(true);
                    client.Dispose();
                }
            }
        }
    }

    public class EmailConfiguration
    {
        public string From { get; set; } = "";
        public string SmtpServer { get; set; } = "";
        public int Port { get; set; }
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public Message(IEnumerable<string> to, string subject, string content)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => new MailboxAddress(x, x)));
            Subject = subject;
            Content = content;
        }
    }
}
