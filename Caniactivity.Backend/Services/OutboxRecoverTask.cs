using Caniactivity.Models;
using Microsoft.EntityFrameworkCore;

namespace Caniactivity.Backend.Services
{
    public class OutboxRecoverTask : BackgroundService
    {
        private readonly ILogger<OutboxRecoverTask> _logger;
        private readonly IServiceProvider _provider;
        private readonly IEmailService _emailService;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(10);

        public OutboxRecoverTask(
            ILogger<OutboxRecoverTask> logger,
            IServiceProvider provider,
            IEmailService emailService)
        {
            _logger = logger;
            _provider = provider;
            _emailService = emailService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(_period);
            while (
                !stoppingToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using(var scope = _provider.CreateScope())
                    {
                        var caniActivityContext = scope.ServiceProvider.GetService<CaniActivityContext>();
                        await caniActivityContext.Outbox
                            .Where(w => !w.IsProcessed)
                            .ForEachAsync(mailToSend =>
                            {
                                this._emailService.ReSendEmail(mailToSend, 1);
                                mailToSend.IsProcessed = true;
                                caniActivityContext.SaveChanges();
                            });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Failed to execute PeriodicHostedService with exception message {ex.Message}. Good luck next round!");
                }
            }
        }
    }
}
