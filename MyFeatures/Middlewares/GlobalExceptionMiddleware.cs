using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace MyFeatures.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/problem+json";

            var problemDetails = exception switch
            {
                NotFoundException => CreateProblemDetails(
                    context,
                    (int)HttpStatusCode.NotFound,
                    "Resource not found.",
                    exception.Message,
                    "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
                _ => CreateProblemDetails(
                    context,
                    (int)HttpStatusCode.InternalServerError,
                    "An unexpected error occurred.",
                    "The server encountered an unexpected error.",
                    "https://tools.ietf.org/html/rfc9110#section-15.6.1")
            };

            response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

            _logger.LogError(
                exception,
                "Unhandled exception for {RequestMethod} {RequestPath}. Returning {StatusCode}.",
                context.Request.Method,
                context.Request.Path,
                response.StatusCode);

            var result = JsonSerializer.Serialize(problemDetails);

            return response.WriteAsync(result);
        }

        private static ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string detail, string type)
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = type,
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            return problemDetails;
        }
    }
}
