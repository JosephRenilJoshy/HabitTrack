using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UserService.Exceptions;

namespace UserService.Filters
{
    public class NotFoundExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<NotFoundExceptionFilter> _logger;

        public NotFoundExceptionFilter(ILogger<NotFoundExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is NotFoundException notFoundEx)
            {
                _logger.LogWarning(notFoundEx, "NotFoundException occurred: {Message}", notFoundEx.Message);
                context.Result = new NotFoundObjectResult(new
                {
                    success = false,
                    message = notFoundEx.Message
                });

                context.ExceptionHandled = true; // tells the pipeline: don't propagate this further

                // If it's not a NotFoundException, we don't touch context.Result or
                // context.ExceptionHandled — the exception keeps propagating upward,
                // eventually reaching ExceptionHandlingMiddleware as the fallback

            }
        }

    }
}
