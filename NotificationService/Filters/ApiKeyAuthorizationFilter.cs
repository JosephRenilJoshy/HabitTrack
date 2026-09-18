using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NotificationService.Filters
{
    public class ApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        private const string API_KEY_HEADER_NAME = "X-Internal-Api-Key";

        public ApiKeyAuthorizationFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "API Key was not provided."
                });
            }

            var apiKey = _configuration["InternalApiKey"];

            if (string.IsNullOrEmpty(apiKey) || extractedApiKey != apiKey)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Invalid API Key."
                });
            }
        }
    }
}
