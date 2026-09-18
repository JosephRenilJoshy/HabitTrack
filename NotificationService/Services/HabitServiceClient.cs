using NotificationService.Constants;
using NotificationService.DTOs;

namespace NotificationService.Services
{
    public class HabitServiceClient : IHabitServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HabitServiceClient> _logger;

        public HabitServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<HabitServiceClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<HabitDto>> GetAtRiskHabitsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, HabitServiceApiConstants.AtRiskEndPoint);
            request.Headers.Add(HabitServiceApiConstants.InternalApiKeyHeader, _configuration["InternalApiKey"]);

            var response = await _httpClient.SendAsync(request);

            if(!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch at-risk habits. Status Code: {StatusCode}", response.StatusCode);
                return new List<HabitDto>();
            }

            var habits = await response.Content.ReadFromJsonAsync<List<HabitDto>>();
            return habits ?? new List<HabitDto>();

        }
    }
}