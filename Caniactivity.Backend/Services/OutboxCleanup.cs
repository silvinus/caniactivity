using Caniactivity.Models;
using Microsoft.EntityFrameworkCore;

namespace Caniactivity.Backend.Services
{
    public class OutboxCleanup : BackgroundService
    {
        private readonly ILogger<OutboxCleanup> _logger;
        private readonly IServiceProvider _provider;
        private readonly IEmailService _emailService;
        private readonly TimeSpan _period = TimeSpan.FromHours(12);

        public OutboxCleanup(
            ILogger<OutboxCleanup> logger,
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
                            .Where(w => w.IsProcessed)
                            .ForEachAsync(mailToRemove =>
                            {
                                caniActivityContext.Outbox.Remove(mailToRemove);
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
