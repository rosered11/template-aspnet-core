using Boxed.AspNetCore;
using Boxed.AspNetCore.Swagger;
using Boxed.AspNetCore.Swagger.OperationFilters;
using Boxed.AspNetCore.Swagger.SchemaFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace template.api
{
    internal static class CustomServiceCollectionExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with caching services added.</returns>
        public static IServiceCollection AddCustomCaching(this IServiceCollection services) =>
            services
                .AddMemoryCache()
                // Adds IDistributedCache which is a distributed cache shared between multiple servers. This adds a
                // default implementation of IDistributedCache which is not distributed. You probably want to use the
                // Redis cache provider by calling AddDistributedRedisCache.
                .AddDistributedMemoryCache();

        /// <summary>
        /// Add cross-origin resource sharing (CORS) services and configures named CORS policies. See
        /// https://docs.asp.net/en/latest/security/cors.html
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with CORS services added.</returns>
        public static IServiceCollection AddCustomCors(this IServiceCollection services) =>
            services.AddCors(
                options =>
                    // Create named CORS policies here which you can consume using application.UseCors("PolicyName")
                    // or a [EnableCors("PolicyName")] attribute on your controller or action.
                    options.AddPolicy(
                        CorsPolicyName.AllowAny,
                        x => x
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()));

        /// <summary>
        /// Configures the settings by binding the contents of the appsettings.json file to the specified Plain Old CLR
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The services with options services added.</returns>
        public static IServiceCollection AddCustomOptions(
            this IServiceCollection services,
            IConfiguration configuration) =>
            services
                // ConfigureAndValidateSingleton registers IOptions<T> and also T as a singleton to the services collection.
                .ConfigureAndValidateSingleton<ApplicationOptions>(configuration)
                //.ConfigureAndValidateSingleton<CompressionOptions>(configuration.GetSection(nameof(ApplicationOptions.Compression)))
                .ConfigureAndValidateSingleton<ForwardedHeadersOptions>(configuration.GetSection(nameof(ApplicationOptions.ForwardedHeaders)))
                .ConfigureAndValidateSingleton<CacheProfileOptions>(configuration.GetSection(nameof(ApplicationOptions.CacheProfiles)))
                .ConfigureAndValidateSingleton<KestrelServerOptions>(configuration.GetSection(nameof(ApplicationOptions.Kestrel)));

        /// <summary>
        /// Add custom routing settings which determines how URL's are generated.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with routing services added.</returns>
        public static IServiceCollection AddCustomRouting(this IServiceCollection services) =>
            services.AddRouting(options => options.LowercaseUrls = true);

        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services) =>
            services
                .AddHealthChecks()
                // Add health checks for external dependencies here. See https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks
                .Services;

        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services) =>
            services
                .AddApiVersioning(
                    options =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.ReportApiVersions = true;
                    })
                .AddVersionedApiExplorer(x => x.GroupNameFormat = "'v'VVV"); // Version format: 'v'major[.minor][-status]

        /// <summary>
        /// Adds Swagger services and configures the Swagger services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>The services with Swagger services added.</returns>
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(
                   options =>
                   {
                       var assembly = typeof(Startup).Assembly;
                       var assemblyProduct = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
                       var assemblyDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

                       options.DescribeAllParametersInCamelCase();
                       options.EnableAnnotations();

                       // Add the XML comment file for this assembly, so its contents can be displayed.
                       options.IncludeXmlCommentsIfExists(assembly);

                       options.OperationFilter<ApiVersionOperationFilter>();
                       options.OperationFilter<ClaimsOperationFilter>();
                       options.OperationFilter<ForbiddenResponseOperationFilter>();
                       options.OperationFilter<UnauthorizedResponseOperationFilter>();

                       // Show a default and example model for JsonPatchDocument<T>.
                       options.SchemaFilter<JsonPatchDocumentSchemaFilter>();

                       var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
                       foreach (var apiVersionDescription in provider.ApiVersionDescriptions)
                       {
                           var info = new OpenApiInfo()
                           {
                               Title = assemblyProduct,
                               Description = apiVersionDescription.IsDeprecated ?
                           $"{assemblyDescription} This API version has been deprecated." :
                           assemblyDescription,
                               Version = apiVersionDescription.ApiVersion.ToString(),
                           };
                           options.SwaggerDoc(apiVersionDescription.GroupName, info);
                       }

                       //Auth
                       options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                       {
                           In = ParameterLocation.Header,
                           Description = "Please insert JWT with Bearer into field",
                           Name = "Authorization",
                           Type = SecuritySchemeType.ApiKey
                       });

                       options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                       {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                    },
                                    Scheme = "oauth2",
                                    Name = "Bearer",
                                    In = ParameterLocation.Header,
                                },
                                new List<string>()
                            }
                       });
                   });

            return services;
        }
    }
}