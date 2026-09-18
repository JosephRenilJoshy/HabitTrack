namespace HabitService.Services
{
    public class NotificationServiceClient : INotificationServiceClient
    {
        private const string MilestoneEndpoint = "/api/notifications/milestone";
        private const string ApiKeyHeaderName = "X-Internal-Api-Key";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationServiceClient> _logger;

        public NotificationServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<NotificationServiceClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifyMilestoneAsync(int userId, int habitId, string habitName, int milestone)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, MilestoneEndpoint)
            {
                Content = JsonContent.Create(new
                {
                    userId,
                    habitId,
                    habitName,
                    milestone
                })
            };
            request.Headers.Add(ApiKeyHeaderName, _configuration["InternalApiKey"]);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to notify milestone. Status: {StatusCode}", response.StatusCode);
                }

            }
            catch (Exception ex)
            {
                // IMPORTANT: a failed notification should NEVER break the
                // check-in flow itself. The check-in already succeeded and
                // was saved — this is a best-effort side notification, not
                // a critical part of the core operation.
                _logger.LogError(ex, "Error notifying milestone for user {UserId}, habit {HabitId}, milestone {Milestone}", userId, habitId, milestone);
            }
        }
    }
}
