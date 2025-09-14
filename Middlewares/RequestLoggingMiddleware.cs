using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace UserAuthApi.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                // Request logging - ÖNEMLİ: Stream'i başa sar
                var request = await FormatRequest(context.Request);
                _logger.LogInformation("Request: {RequestMethod} {RequestPath} {RequestQuery} {RequestBody}",
                    context.Request.Method, context.Request.Path, context.Request.QueryString, request);

                // Response logging için memory stream kullan
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                stopwatch.Stop();

                // Response logging
                var response = await FormatResponse(context.Response, responseBody);
                _logger.LogInformation("Response: {StatusCode} in {ElapsedMilliseconds}ms {ResponseBody}",
                    context.Response.StatusCode, stopwatch.ElapsedMilliseconds, response);

                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            // Stream'i birden fazla kez okumak için buffering et
            request.EnableBuffering();

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true); // ← Stream'i kapatma

            var body = await reader.ReadToEndAsync();

            // STREAM'I MUTLAKA BAŞA SAR
            request.Body.Position = 0;

            return body;
        }

        private async Task<string> FormatResponse(HttpResponse response, MemoryStream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            return text;
        }
    }
}