using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Middleware;
using Xunit;

namespace UserService.Tests.Middleware
{
    public class RequestLoggerTests
    {
        [Fact]
        public async Task InvokeAsync_LogsRequestStartAndCompleted()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var loggerMock = new Mock<ILogger<RequestLogger>>();

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = 201;
                return Task.CompletedTask;
            };

            var middleware = new RequestLogger(next, loggerMock.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            // Expect at least two LogInformation calls (start and completed)
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.AtLeast(2));
        }
    }
}
