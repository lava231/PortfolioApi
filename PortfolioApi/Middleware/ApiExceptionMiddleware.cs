using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PortfolioApi.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
                // Log the exception without exposing sensitive details to the client
                var correlationId = GetOrCreateCorrelationId(context);
                using (_logger.BeginScope(new System.Collections.Generic.Dictionary<string, object> { ["CorrelationId"] = correlationId }))
                {
                    _logger.LogError(ex, "Unhandled exception (CorrelationId={CorrelationId})", correlationId);
                }

                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            const string headerName = "X-Correlation-ID";
            if (context.Request.Headers.TryGetValue(headerName, out var vals))
            {
                var first = vals.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return first!;
                }
            }

            var id = System.Guid.NewGuid().ToString();
            context.Response.Headers[headerName] = id;
            return id;
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            var code = HttpStatusCode.InternalServerError;

            // Do not return exception.Message or stack trace to the client.
            // Return a non-null JSON payload describing the error and correlation id.
            var payload = new { error = "Server Error", correlationId };
            var result = JsonSerializer.Serialize(payload);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result ?? string.Empty);
        }
    }
}
