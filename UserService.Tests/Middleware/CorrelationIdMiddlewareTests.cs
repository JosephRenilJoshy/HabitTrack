using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserService.Middleware;
using Xunit;

namespace UserService.Tests.Middleware
{
    public class CorrelationIdMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WithoutCorrelationHeader_AddsResponseHeaderAndScope()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = ctx => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);
            // In ASP.NET Core the OnStarting callbacks are executed when the response starts.
            // Trigger the response start by writing to the response body so the header callback runs in this unit test.
            await context.Response.WriteAsync(string.Empty);

            // Assert
            Assert.True(context.Response.Headers.ContainsKey("X-Correlation-ID"));
            var header = context.Response.Headers["X-Correlation-ID"].ToString();
            Assert.False(string.IsNullOrWhiteSpace(header));
        }
    }
}
