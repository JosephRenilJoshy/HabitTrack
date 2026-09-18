using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserService.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(kvp => new
                    {
                        Field = kvp.Key,
                        Messages = kvp.Value?.Errors.Select(e => e.ErrorMessage)
                    });

                var response = new
                {
                    success = false,
                    message = "Validation failed",
                    errors
                };

                context.Result = new BadRequestObjectResult(response);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after the action executes
        }
    }
}
