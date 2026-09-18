namespace HabitService.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreateCorrelationId(context);

            // Make it available on the response, so a client (or another service)
            // calling this one can see/propagate the same ID
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // Push it into the logging scope — every log written during this
            // request, by any logger, anywhere in the pipeline, will automatically
            // include this correlation ID without needing to pass it around manually
            using (LogContext(context, correlationId))
            {
                await _next(context);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId)
                && !string.IsNullOrWhiteSpace(existingId))
            {
                return existingId!;
            }

            return Guid.NewGuid().ToString();
        }

        private static IDisposable LogContext(HttpContext context, string correlationId)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("CorrelationId");

            return logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            })!;
        }
    }
}
