using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using UserService.Middleware;
using Xunit;

namespace UserService.Tests.Middleware
{
    public class ExceptionHandlerTests
    {
        [Fact]
        public async Task InvokeAsync_WhenNextThrows_WritesAppropriateStatusAndMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => Task.FromException(new InvalidOperationException("conflict happened"));
            var logger = new NullLogger<ExceptionHandler>();
            var middleware = new ExceptionHandler(next, logger);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(body);
            Assert.True(doc.TryGetProperty("message", out var msg));
            Assert.Equal("conflict happened", msg.GetString());
        }
    }
}
