namespace NotificationService.Services
{
    public class AtRiskCheckBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AtRiskCheckBackgroundService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public AtRiskCheckBackgroundService(IServiceProvider serviceProvider, ILogger<AtRiskCheckBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running at-risk habit check at: {time}", DateTimeOffset.Now);

                try
                {
                    // BackgroundService runs for the ENTIRE app lifetime, but
                    // scoped services (like our DbContext-dependent ones) can't
                    // just be injected into its constructor — we must create
                    // a new scope per iteration to safely resolve them
                    using var scope = _serviceProvider.CreateScope();

                    var habitClient = scope.ServiceProvider.GetRequiredService<IHabitServiceClient>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var atRiskHabits = await habitClient.GetAtRiskHabitsAsync();

                    foreach(var habit in atRiskHabits)
                    {
                        await notificationService.CreateAtRiskNotificationAsync(habit.UserId, habit.Id, habit.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking at-risk habits.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}
