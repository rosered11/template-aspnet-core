using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using template.infrastructure.Entities;
using template.infrastructure.Options;

namespace template.infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ApplicationDbContextOptions options;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _accessor;

        public ApplicationDbContext(ApplicationDbContextOptions options, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
            : base(options.Options)
        {
            this.options = options;
            _configuration = configuration;
            _accessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var mapping in options.Mappings)
            {
                mapping.Map(builder);
            }
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            BeforeSaveShanges();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            BeforeSaveShanges();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            BeforeSaveShanges();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            BeforeSaveShanges();
            return base.SaveChanges();
        }

        protected virtual void BeforeSaveShanges()
        {
            var entities = ChangeTracker.Entries()
                .Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                var now = DateTime.Now;

                if (entity.State == EntityState.Added)
                {
                    ((BaseEntity)entity.Entity).CreatedAt = now;
                }
                ((BaseEntity)entity.Entity).UpdatedAt = now;
            }

            foreach (var mapping in options.Mappings)
            {
                mapping.BeforeSaveChanges(ChangeTracker, _accessor, _configuration);
            }
        }
    }
}
