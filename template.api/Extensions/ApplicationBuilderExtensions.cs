using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using template.api;

namespace template.api
{
    internal static partial class ApplicationBuilderExtensions
    {
        public class CustomRequestLoggingMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly DiagnosticContext _diagnosticContext;
            private readonly MessageTemplate _messageTemplate;
            private readonly ILogger _logger;
            private static readonly LogEventProperty[] NoProperties = new LogEventProperty[0];
            private const string Template = "requested {Protocol} {Scheme} {Host} {QueryString} {RequestMethod} {RequestPath} responded {ContentType} {StatusCode}";

            public CustomRequestLoggingMiddleware(RequestDelegate next, DiagnosticContext diagnosticContext)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
                _diagnosticContext = diagnosticContext ?? throw new ArgumentNullException(nameof(diagnosticContext));
                _messageTemplate = new MessageTemplateParser().Parse(Template);
                _logger = Log.ForContext<CustomRequestLoggingMiddleware>();
            }

            public async Task Invoke(HttpContext httpContext)
            {
                var logger = _logger ?? Log.ForContext<CustomRequestLoggingMiddleware>();
                var level = GetLevel(httpContext, null);
                if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

                var request = httpContext.Request;
                var response = httpContext.Response;
                
                var collector = _diagnosticContext.BeginCollection();
                try
                {
                    if (!collector.TryComplete(out var collectedProperties))
                        collectedProperties = NoProperties;
                    List<LogEventProperty> properties = collectedProperties.ToList();

                    await SetPropertyRequestLog(httpContext, properties);
                    
                    await _next(httpContext);

                    SetPropertyResponseLog(httpContext, properties);

                    var evt = new LogEvent(DateTimeOffset.Now, level, null, _messageTemplate, properties);
                    logger.Write(evt);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    collector.Dispose();
                }
            }

            static async Task SetPropertyRequestLog(HttpContext httpContext,List<LogEventProperty> properties)
            {
                HttpRequest request = httpContext.Request;
                properties.AddRange(new List<LogEventProperty>
                    {
                        new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
                        new LogEventProperty("RequestPath", new ScalarValue(GetPath(httpContext))),
                        new LogEventProperty("Protocol", new ScalarValue(httpContext.Request.Protocol)),
                        new LogEventProperty("Scheme", new ScalarValue(httpContext.Request.Scheme)),
                        new LogEventProperty("Host", new ScalarValue(httpContext.Request.Host)),
                        new LogEventProperty("QueryString", new ScalarValue(httpContext.Request.QueryString.Value))
                    });

                if (request.Method == "POST")
                {
                    switch (request.Path)
                    {
                        case "/api/v1/mypost":
                            string requestBody = await ReadRequestBodyAsync(request);
                            // Implement something

                            break;
                    }
                }

                if (request.RouteValues.ContainsKey("proposalKey"))
                {
                    properties.Add(new LogEventProperty("Key", new ScalarValue(request.RouteValues["proposalKey"])));
                }

                if (request.RouteValues.ContainsKey("applicationKey"))
                {
                    properties.Add(new LogEventProperty("Key", new ScalarValue(request.RouteValues["applicationKey"])));
                }
            }

            static void SetPropertyResponseLog(HttpContext httpContext, List<LogEventProperty> properties)
            {
                HttpResponse response = httpContext.Response;
                var statusCode = httpContext.Response.StatusCode;
                properties.Add(new LogEventProperty("StatusCode", new ScalarValue(statusCode)));
                properties.Add(new LogEventProperty("ContentType", new ScalarValue(response.ContentType)));
            }

            static async Task<string> ReadRequestBodyAsync(HttpRequest request)
            {
                string result = "";
                if(request.ContentLength.HasValue && request.ContentLength > 0)
                {
                    HttpRequestRewindExtensions.EnableBuffering(request);
                    Stream body = request.Body;
                    byte[] buffer = new byte[Convert.ToInt32(request.ContentLength)];
                    await request.Body.ReadAsync(buffer, 0, buffer.Length);
                    result = Encoding.UTF8.GetString(buffer);
                    body.Seek(0, SeekOrigin.Begin);
                }
                
                return result;
            }

            static string GetPath(HttpContext httpContext)
            {
                var requestPath = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
                if (string.IsNullOrEmpty(requestPath))
                {
                    requestPath = httpContext.Request.Path.ToString();
                }

                return requestPath;
            }

            static LogEventLevel GetLevel(HttpContext httpContext, Exception exception)
            {
                if (exception == null && httpContext.Response.StatusCode <= 499)
                {
                    if (IsHealthCheckEndpoint(httpContext))
                    {
                        return LogEventLevel.Verbose;
                    }

                    return LogEventLevel.Information;
                }

                return LogEventLevel.Error;
            }

            static bool IsHealthCheckEndpoint(HttpContext httpContext)
            {
                var endpoint = httpContext.GetEndpoint();
                if (endpoint is object)
                {
                    return endpoint.DisplayName == "Health checks";
                }

                return false;
            }
        }

        public static IApplicationBuilder UseCustomSwaggerUI(this IApplicationBuilder application) =>
            application.UseSwaggerUI(
                options =>
                {
                    // Set the Swagger UI browser document title.
                    options.DocumentTitle = typeof(Startup)
                        .Assembly
                        .GetCustomAttribute<AssemblyProductAttribute>()
                        .Product;
                    // Set the Swagger UI to render at '/'.
                    options.RoutePrefix = string.Empty;

                    // options.RoutePrefix = "swagger";
                    options.DisplayOperationId();
                    options.DisplayRequestDuration();

                    var provider = application.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
                    foreach (var apiVersionDescription in provider
                        .ApiVersionDescriptions
                        .OrderByDescending(x => x.ApiVersion))
                    {
                        options.SwaggerEndpoint(
                            $"swagger/{apiVersionDescription.GroupName}/swagger.json",
                            $"Version {apiVersionDescription.ApiVersion}");
                    }
                });
    }
}
