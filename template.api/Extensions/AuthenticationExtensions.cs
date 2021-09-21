using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace template.api
{
    public static class AuthenticationService
    {
        private const string JwtBearer = "JwtBearer";

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
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
                        IssuerSigningKey = new X509SecurityKey(LoadCertificate(configuration)),

                        ValidateIssuer = isValidate,
                        ValidIssuer = "http://localhost/auth",//configuration["Authentication:ValidIssuer"],
                        ValidateAudience = isValidate,
                        ValidAudience = "http://localhost/audian",//configuration["Authentication:ValidAudience"],
                        ValidateLifetime = isValidate,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };
                });
        }

        public static X509Certificate2 LoadCertificate(IConfiguration configuration)
        {
            string source = configuration["Certification:Source"] ?? "file";
            switch (source?.ToLower())
            {
                case "file":
                    try
                    {
                        var cert = new X509Certificate2("localhost.pfx", "1234");
                        return cert;
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
