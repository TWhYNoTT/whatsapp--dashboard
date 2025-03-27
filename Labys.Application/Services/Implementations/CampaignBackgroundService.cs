using Labys.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Labys.Application.Services.Implementations
{
    public class CampaignBackgroundService : BackgroundService
    {
        private readonly ILogger<CampaignBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

        public CampaignBackgroundService(
            ILogger<CampaignBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Background Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // IMPORTANT: Create a new scope for each check
                    // This ensures proper DbContext lifecycle management
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        _logger.LogInformation("Checking for scheduled campaigns");

                        // Get the campaign service from the DI container
                        var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

                        // Process any scheduled campaigns that are due
                        await campaignService.ProcessScheduledCampaignsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled campaigns");
                }

                // Wait for the next check interval
                _logger.LogInformation("Waiting {Interval} minutes until next check", _checkInterval.TotalMinutes);
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Campaign Background Service is stopping");
        }
    }
}