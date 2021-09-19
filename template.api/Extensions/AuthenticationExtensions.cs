using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace template.api
{
    public static class AuthenticationService
    {
        private const string JwtBearer = "JwtBearer";

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            bool isValidate = true;

            if (configuration["Authentication:Validate"] != null)
            {
                isValidate = bool.Parse(configuration["Authentication:Validate"]);
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearer;
                options.DefaultChallengeScheme = JwtBearer;
            })
                .AddJwtBearer("JwtBearer", jwtBearerOptions =>
                {
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new X509SecurityKey(LoadCertificate(configuration, logger)),

                        ValidateIssuer = isValidate,
                        ValidIssuer = configuration["Authentication:ValidIssuer"],
                        ValidateAudience = isValidate,
                        ValidAudience = configuration["Authentication:ValidAudience"],
                        ValidateLifetime = isValidate,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };
                });
        }

        public static X509Certificate2 LoadCertificate(IConfiguration configuration, ILogger logger)
        {
            string source = configuration["Certification:Source"] ?? "file";
            switch (source?.ToLower())
            {
                case "file":
                    string certPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "contoso.com.crt");
                    string keyPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "contoso.com.key");
                    logger.Error($"======================== Path cert ==> {certPath}");
                    string path =  System.IO.File.ReadAllText(certPath);

                    try
                    {
                        return new X509Certificate2(path, "p@ssw0rd2");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("No certificate found in the specified location.", ex);
                    }
                case "env":
                    try
                    {
                        var base64 = configuration["Certification:Base64"];
                        var cert = Convert.FromBase64String(base64);
                        return new X509Certificate2(cert, configuration["Certification:Secret"]);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("No certificate found in the specified location.", ex);
                    }
                default:
                    var thumbPrint = configuration["Certification:ThumbPrint"];

                    using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                    {
                        store.Open(OpenFlags.ReadOnly);
                        var count = store.Certificates.Count;
                        var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, Regex.Replace(thumbPrint, @"[^\da-fA-F]", string.Empty).ToUpper(), false);
                        if (certCollection.Count == 0)
                        {
                            throw new Exception("No certificate found containing the specified thumbprint.");
                        }
                        else
                        {
                            return certCollection[0];
                        }
                    }
            }
        }
    }
}
