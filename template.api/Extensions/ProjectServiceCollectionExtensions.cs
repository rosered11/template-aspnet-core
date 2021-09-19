using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using template.infrastructure;
using template.infrastructure.Entities;
using template.infrastructure.Entities.Maps;
using template.infrastructure.Options;
using template.infrastructure.Repositories;
using template.infrastructure.Utilities;

namespace MTL.FragileCustomer.Api
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods add project services.
    /// </summary>
    /// <remarks>
    /// AddSingleton - Only one instance is ever created and returned.
    /// AddScoped - A new instance is created and returned for each request/response cycle.
    /// AddTransient - A new instance is created and returned each time.
    /// </remarks>
    public static class ProjectServiceCollectionExtensions
    {
        public static IServiceCollection RegisterEntity<TEntity, TMap>(this IServiceCollection services)
            where TEntity : BaseEntity
            where TMap : EntityMap<TEntity>, new()
        {
            return services
                        .AddScoped<Repository<TEntity>>()
                        .AddSingleton<IEntityTypeMap, TMap>();
        }

        public static IServiceCollection RegisterEntity(this IServiceCollection services)

        {
            foreach (var entityType in ModelTypes.Get("MTL.FragileCustomer.Infrastructure.Entities"))
            {
                var repository = typeof(Repository<>).MakeGenericType(entityType.AsType());
                var entityMap = typeof(EntityMap<>).MakeGenericType(entityType.AsType());
                services.AddScoped(repository)
                .AddSingleton(x => { return (IEntityTypeMap)Activator.CreateInstance(entityMap); });
            }

            return services;
        }

        public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration) =>
            services
                .AddOptions()
                .AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(configuration.GetValue<string>("ConnectionDatabase"))
                .Options)
                .AddSingleton<ApplicationDbContextOptions>()
                .AddDbContext<ApplicationDbContext>();

        public static IServiceCollection RegisterMapper(this IServiceCollection services) =>
            services;

        public static IServiceCollection RegisterService(this IServiceCollection services, IConfiguration configuration) =>
            services;
    }
}
