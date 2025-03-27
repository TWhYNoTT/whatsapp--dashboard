using Newtonsoft.Json;
using Twilio.TwiML;

namespace Labys.API.Middleware
{
    /// <summary>
    /// Middleware to handle WhatsApp webhook Content-Type header issues
    /// </summary>
    public class WhatsAppWebhookMiddleware
    {
        private readonly RequestDelegate _next;

        public WhatsAppWebhookMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only process WhatsApp webhook endpoints
            if ((context.Request.Path.StartsWithSegments("/api/WhatsApp/webhook") ||
                 context.Request.Path.StartsWithSegments("/api/WhatsApp/status")) &&
                 context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Enable buffering so we can read the request body multiple times
                context.Request.EnableBuffering();

                // Set a default content type for form processing if none exists
                if (string.IsNullOrEmpty(context.Request.ContentType))
                {
                    context.Request.ContentType = "application/x-www-form-urlencoded";
                }
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }

    /// <summary>
    /// Global exception handling middleware with specific handling for WhatsApp endpoints
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
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

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Special handling for Twilio webhook endpoints
            if (context.Request.Path.StartsWithSegments("/api/WhatsApp/webhook") ||
                context.Request.Path.StartsWithSegments("/api/WhatsApp/status"))
            {
                // For Twilio webhooks, return TwiML - we must return HTTP 200 even on error
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/xml";

                var response = new MessagingResponse();
                response.Message("Thank you for your message. We'll get back to you shortly.");

                await context.Response.WriteAsync(response.ToString());
            }
            else
            {
                // For regular API calls, return JSON error
                var error = new
                {
                    Error = "An error occurred while processing your request.",
                    Message = exception.Message,
                    Path = context.Request.Path,
                    StatusCode = context.Response.StatusCode
                };

                var json = JsonConvert.SerializeObject(error);
                await context.Response.WriteAsync(json);
            }
        }
    }

    /// <summary>
    /// Extension methods for middleware registration
    /// </summary>
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseWhatsAppWebhookMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WhatsAppWebhookMiddleware>();
        }

        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}