using Boxed.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace template.api
{
    #region Default Main
    // public class Program
    // {
    //     public static void Main(string[] args)
    //     {
    //         CreateHostBuilder(args).Build().Run();
    //     }

    //     public static IHostBuilder CreateHostBuilder(string[] args) =>
    //         Host.CreateDefaultBuilder(args)
    //             .ConfigureWebHostDefaults(webBuilder =>
    //             {
    //                 webBuilder.UseStartup<Startup>();
    //             });
    // }
    #endregion Default Main

    #region Custom Main
    public class Program
    {
        public static void Main(string[] args)
        {
            LogAndRunAsync(CreateHostBuilder(args).Build());
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                    AddConfiguration(config, hostingContext.HostingEnvironment, args))
                .UseSerilog()
                .ConfigureWebHost(ConfigureWebHostBuilder);

        public static void LogAndRunAsync(IHost host)
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            // Use the W3C Trace Context format to propagate distributed trace identifiers.
            // See https://devblogs.microsoft.com/aspnet/improvements-in-net-core-3-0-for-troubleshooting-and-monitoring-distributed-apps/
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            host.Services.GetRequiredService<IHostEnvironment>().ApplicationName =
                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;

            Log.Logger = CreateLogger(host, host.Services.GetRequiredService<IConfiguration>());

            try
            {
                Log.Information("Started application");
                host.Run();
                Log.Information("Stopped application");
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder webHostBuilder) =>
            webHostBuilder
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    options.Limits.MaxRequestHeadersTotalSize = 1048576;
                    options.Limits.Http2.MaxRequestHeaderFieldSize = 1048576;
                    options.Limits.MaxRequestHeaderCount = 300;
                    options.Limits.MaxRequestBufferSize = 104857600;
                    // Configure the Url and ports to bind to
                    // This overrides calls to UseUrls and the ASPNETCORE_URLS environment variable, but will be 
                    // overridden if you call UseIisIntegration() and host behind IIS/IIS Express
                    options.Listen(IPAddress.Loopback, 5000);
                    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                    {
                        listenOptions.UseHttps("localhost.pfx", "1234");
                    });
                });
        private static Logger CreateLogger(IHost host, IConfiguration configuration)
        {
            var hostEnvironment = host.Services.GetRequiredService<IHostEnvironment>();
            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console()
                .WriteTo.Map("Table", "Internal", (table, wt) =>
                wt.MSSqlServer(
                    connectionString: configuration.GetValue<string>("ConnectionDatabase"),
                    sinkOptions: new MSSqlServerSinkOptions { TableName = configuration.GetValue<string>("Serilog:InternalTableName") },
                    columnOptions: new ColumnOptions
                    {
                        AdditionalColumns = new List<SqlColumn> {
                            new SqlColumn { ColumnName = "RequestId", DataType = SqlDbType.NVarChar, AllowNull = true, DataLength = 128 },
                            new SqlColumn { ColumnName = "Origin", DataType = SqlDbType.NVarChar, AllowNull = true, DataLength = 128 },
                            new SqlColumn { ColumnName = "Key", DataType = SqlDbType.NVarChar, AllowNull = true, DataLength = 128 },
                            new SqlColumn { ColumnName = "Channel", DataType = SqlDbType.NVarChar, AllowNull = true, DataLength = 128 }
                        }
                    }
                ))
                .CreateLogger();
        }

        private static IConfigurationBuilder AddConfiguration(
            IConfigurationBuilder configurationBuilder,
            IHostEnvironment hostEnvironment,
            string[] args) =>
            configurationBuilder
                // Add configuration specific to the Development, Staging or Production environments. This config can
                // be stored on the machine being deployed to or if you are using Azure, in the cloud. These settings
                // override the ones in all of the above config files. See
                // http://docs.asp.net/en/latest/security/app-secrets.html
                .AddEnvironmentVariables(prefix: configurationBuilder.Build().GetValue<string>("Prefix"))
                .AddIf(
                    args is object,
                    x => x.AddCommandLine(args));
    }
    #endregion Custom Main
}
