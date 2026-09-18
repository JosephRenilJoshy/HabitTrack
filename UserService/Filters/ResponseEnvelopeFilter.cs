using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserService.Filters
{
    public class ResponseEnvelopeFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if(context.Result is ObjectResult result 
                && result.StatusCode is >= 200 && result.StatusCode < 300)
            {
                var envolope = new
                {
                    success = true,
                    data = result.Value,
                    timeStamp = DateTime.UtcNow
                };

                result.Value = envolope;
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            // No action needed after the result executes
        }
    }
}
