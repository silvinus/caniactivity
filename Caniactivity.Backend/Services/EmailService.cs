using Caniactivity.Models;
using Duende.IdentityServer.Models;
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
        private readonly IHostEnvironment _hostEnvironment;

        public EmailService(EmailConfiguration configuration, IServiceProvider serviceProvider, IHostEnvironment hostEnvironment)
        {
            _emailConfig = configuration;
            _emailConfig.From = Environment.GetEnvironmentVariable(SMTP_FROM_ENV_VAR_NAME);
            _emailConfig.UserName = Environment.GetEnvironmentVariable(SMTP_USER_ENV_VAR_NAME);
            _emailConfig.Password = Environment.GetEnvironmentVariable(SMTP_PASSWORD_ENV_VAR_NAME);

            this.serviceProvider = serviceProvider;
            _hostEnvironment = hostEnvironment;
        }

        void IEmailService.SendEmail(Message message, int maxRetries)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var emailMessage = CreateEmailMessage(message);

                var context = scope.ServiceProvider.GetRequiredService<CaniActivityContext>();
                var outboxEntity = new EmailOutbox()
                {
                    To = string.Concat(message.To.Select(w => w.Address)),
                    Subject = message.Subject,
                    Body = emailMessage.HtmlBody,
                    IsProcessed = false
                };
                context.Outbox.Add(outboxEntity);
                context.SaveChanges();

                int attempts = 0;
                while (attempts < maxRetries)
                {
                    try
                    {
                        //Send(emailMessage);
                        outboxEntity.IsProcessed = false;
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
                var builder = new BodyBuilder();
                builder.HtmlBody = mailToSend.Body;
                var emailMessage = CreateRawMessage(
                    mailToSend.To.Split('|').Select(w => new MailboxAddress(w, w)),
                    mailToSend.Subject,
                    builder.ToMessageBody()
                );

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
            var pathToFile = _hostEnvironment.ContentRootPath
                            + Path.DirectorySeparatorChar.ToString()
                            + "Templates"
                            + Path.DirectorySeparatorChar.ToString()
                            + "Emails"
                            + Path.DirectorySeparatorChar.ToString()
                            + message.Template;
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();
            }
            builder.HtmlBody = string.Format(builder.HtmlBody, message.Args);

            return CreateRawMessage(message.To, message.Subject, builder.ToMessageBody());
        }

        private MimeMessage CreateRawMessage(IEnumerable<MailboxAddress> addresses, string subject, MimeEntity body)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailConfig.From, _emailConfig.From));
            emailMessage.To.AddRange(addresses);
            emailMessage.Subject = subject;
            emailMessage.Body = body;
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
        public string? From { get; set; } = "";
        public string SmtpServer { get; set; } = "";
        public int Port { get; set; }
        public string? UserName { get; set; } = "";
        public string? Password { get; set; } = "";
    }

    public record Message
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; }
        //public BodyBuilder Content { get; set; }
        public string[] Args { get; }
        public string Template { get; }

        public static Message AppointmentCreated(IEnumerable<string> to, string subject, params string[] args)
        {
            return new Message(to, subject, "AppointmentCreated.html", args);
        }

        public static Message AppointmentValidated(IEnumerable<string> to, string subject, params string[] args)
        {
            return new Message(to, subject, "AppointmentValidated.html", args);
        }

        public static Message AppointmentModified(IEnumerable<string> to, string subject, params string[] args)
        {
            return new Message(to, subject, "AppointmentModified.html", args);
        }

        public static Message AppointmentDeleted(IEnumerable<string> to, string subject, params string[] args)
        {
            return new Message(to, subject, "AppointmentDeleted.html", args);
        }

        internal Message(IEnumerable<string> to, string subject, string template, string[] args)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => new MailboxAddress(x, x)));
            Subject = subject;
            Args = args;
            Template = template;
        }
    }
}
