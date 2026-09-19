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
    public class ResponseEnvelopeFilterTests
    {
        [Fact]
        public void OnResultExecuting_ObjectResult_EnvelopesResponse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

            var original = new { Value = "payload" };
            var objectResult = new ObjectResult(original) { StatusCode = 200 };

            var context = new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), objectResult, controller: null!);

            var filter = new ResponseEnvelopeFilter();

            // Act
            filter.OnResultExecuting(context);

            // Assert
            Assert.IsType<ObjectResult>(context.Result);
            var result = (ObjectResult)context.Result!;
            Assert.NotSame(original, result.Value);

            // ensure the envelope has a 'data' property that references the original object
            var dataProp = result.Value!.GetType().GetProperty("data");
            Assert.NotNull(dataProp);
            var dataValue = dataProp!.GetValue(result.Value);
            Assert.Same(original, dataValue);
        }
    }
}
