using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using UserService.Filters;
using Xunit;

namespace UserService.Tests.Filters
{
    public class ValidationFilterTests
    {
        [Fact]
        public void OnActionExecuting_InvalidModelState_SetsBadRequestResult()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            actionContext.ModelState.AddModelError("Name", "Required");

            var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller: null!);

            var filter = new ValidationFilter();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.IsType<BadRequestObjectResult>(context.Result);
            var bad = (BadRequestObjectResult)context.Result!;
            Assert.NotNull(bad.Value);
        }
    }
}
