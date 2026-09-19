using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using UserService.Exceptions;
using UserService.Filters;
using Xunit;

namespace UserService.Tests.Filters
{
    public class NotFoundExceptionFilterTests
    {
        [Fact]
        public void OnException_NotFoundException_SetsNotFoundResultAndHandles()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

            var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = new NotFoundException("item not found")
            };

            var logger = new NullLogger<NotFoundExceptionFilter>();
            var filter = new NotFoundExceptionFilter(logger);

            // Act
            filter.OnException(exceptionContext);

            // Assert
            Assert.True(exceptionContext.ExceptionHandled);
            Assert.IsType<NotFoundObjectResult>(exceptionContext.Result);
            var result = (NotFoundObjectResult)exceptionContext.Result!;
            Assert.NotNull(result.Value);
        }
    }
}
