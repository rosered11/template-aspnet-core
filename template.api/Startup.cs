using Boxed.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MTL.FragileCustomer.Api;

namespace template.api
{
    // public class Startup
    // {
    //     public Startup(IConfiguration configuration)
    //     {
    //         Configuration = configuration;
    //     }

    //     public IConfiguration Configuration { get; }

    //     // This method gets called by the runtime. Use this method to add services to the container.
    //     public void ConfigureServices(IServiceCollection services)
    //     {

    //         services.AddControllers();
    //         services.AddSwaggerGen(c =>
    //         {
    //             c.SwaggerDoc("v1", new OpenApiInfo { Title = "workspace", Version = "v1" });
    //         });
    //     }

    //     // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    //     public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    //     {
    //         if (env.IsDevelopment())
    //         {
    //             app.UseDeveloperExceptionPage();
    //             app.UseSwagger();
    //             app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "workspace v1"));
    //         }

    //         app.UseHttpsRedirection();

    //         app.UseRouting();

    //         app.UseAuthorization();

    //         app.UseEndpoints(endpoints =>
    //         {
    //             endpoints.MapControllers();
    //         });
    //     }
    // }
    /* ================================= */
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration, where key value pair settings are stored. See
        /// http://docs.asp.net/en/latest/fundamentals/configuration.html</param>
        /// <param name="webHostEnvironment">The environment the application is running under. This can be Development,
        /// Staging or Production by default. See http://docs.asp.net/en/latest/fundamentals/environments.html</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        private IConfiguration _configuration { get; }
        private IWebHostEnvironment _webHostEnvironment { get; }

        /// <summary>
        /// Configures the services to add to the ASP.NET Core Injection of Control (IoC) container. This method gets
        /// called by the ASP.NET runtime. See
        /// http://blogs.msdn.com/b/webdev/archive/2014/06/17/dependency-injection-in-asp-net-vnext.aspx
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddCustomCaching()
                .AddCustomCors()
                .AddCustomOptions(_configuration)
                .AddCustomRouting()

                .AddCustomHealthChecks()
                .AddCustomSwagger()
                .AddHttpContextAccessor()

                .AddCustomApiVersioning()
                .AddServerTiming()

                .AddControllers()
                .AddCustomJsonOptions(_webHostEnvironment)
                .AddCustomMvcOptions(_configuration)
                .Services

                .RegisterEntity()
                .AddDbContext(_configuration)
                .RegisterMapper()
                .RegisterService(_configuration)
                
            ;

            services.AddAuthentication(_configuration);
        }

        /// <summary>
        /// Configures the application and HTTP request pipeline. Configure is called after ConfigureServices is
        /// called by the ASP.NET runtime.
        /// </summary>
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseCors(CorsPolicyName.AllowAny);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ApplicationBuilderExtensions.CustomRequestLoggingMiddleware>();

            app.UseSwagger();
            app.UseCustomSwaggerUI();

            app.UseEndpoints(builder =>
            {
                builder.MapControllers().RequireCors(CorsPolicyName.AllowAny);
                builder
                    .MapHealthChecks("/status")
                    .RequireCors(CorsPolicyName.AllowAny);
                builder
                    .MapHealthChecks("/status/self", new HealthCheckOptions() { Predicate = _ => false })
                    .RequireCors(CorsPolicyName.AllowAny);
            });
        }
    }
}
